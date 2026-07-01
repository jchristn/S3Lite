namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using S3Lite;
    using S3Lite.ApiObjects;

    internal sealed class S3LiteSyntheticServer : IDisposable, IAsyncDisposable
    {
        private readonly Dictionary<string, S3LiteSyntheticBucket> _Buckets = new Dictionary<string, S3LiteSyntheticBucket>(StringComparer.Ordinal);
        private readonly ReaderWriterLockSlim _BucketsLock = new ReaderWriterLockSlim();
        private readonly CancellationTokenSource _CancellationTokenSource = new CancellationTokenSource();
        private readonly string _Hostname = "127.0.0.1";
        private readonly HttpListener _Listener;
        private readonly Owner _Owner = new Owner("synthetic-owner-id", "synthetic-owner");
        private readonly int _Port;
        private readonly string _Region = "us-test-1";
        private readonly SerializationHelper _Serializer = new SerializationHelper();
        private int _Disposed = 0;
        private Task _ListenerTask = Task.CompletedTask;
        private string? _LastHostHeader = null;
        private string? _LastHttpMethod = null;
        private string? _LastRawPathAndQuery = null;
        private NameValueCollection? _LastRequestHeaders = null;

        private S3LiteSyntheticServer(int port)
        {
            _Port = port;
            _Listener = new HttpListener();
            _Listener.Prefixes.Add("http://" + _Hostname + ":" + _Port.ToString() + "/");
        }

        internal string BaseUrl
        {
            get
            {
                return "http://" + _Hostname + ":" + _Port.ToString();
            }
        }

        internal string Hostname
        {
            get
            {
                return _Hostname;
            }
        }

        internal int Port
        {
            get
            {
                return _Port;
            }
        }

        internal string Region
        {
            get
            {
                return _Region;
            }
        }

        /// <summary>
        /// Host header value presented on the most recently handled request.
        /// Used to verify the Host header a gateway-routed request carries.
        /// </summary>
        internal string? LastHostHeader
        {
            get
            {
                return _LastHostHeader;
            }
        }

        /// <summary>
        /// HTTP method of the most recently handled request.
        /// </summary>
        internal string? LastHttpMethod
        {
            get
            {
                return _LastHttpMethod;
            }
        }

        /// <summary>
        /// Raw (still percent-encoded) path and query of the most recently handled request.
        /// Used to reconstruct the upstream URL a gateway-routed request was signed against.
        /// </summary>
        internal string? LastRawPathAndQuery
        {
            get
            {
                return _LastRawPathAndQuery;
            }
        }

        /// <summary>
        /// Headers presented on the most recently handled request.
        /// Used to recompute the SigV4 signature a gateway-routed request carries.
        /// </summary>
        internal NameValueCollection? LastRequestHeaders
        {
            get
            {
                return _LastRequestHeaders;
            }
        }

        internal static async Task<S3LiteSyntheticServer> StartAsync(CancellationToken cancellationToken = default)
        {
            int port = GetAvailablePort();
            S3LiteSyntheticServer server = new S3LiteSyntheticServer(port);
            server._Listener.Start();
            server._ListenerTask = server.ListenAsync(server._CancellationTokenSource.Token);
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            return server;
        }

        internal bool BucketExists(string bucketName)
        {
            if (String.IsNullOrWhiteSpace(bucketName)) throw new ArgumentNullException(nameof(bucketName));

            _BucketsLock.EnterReadLock();
            try
            {
                return _Buckets.ContainsKey(bucketName);
            }
            finally
            {
                _BucketsLock.ExitReadLock();
            }
        }

        internal HttpClient CreateHttpClient(TimeSpan? timeout = null)
        {
            HttpClient client = new HttpClient();
            if (timeout != null) client.Timeout = timeout.Value;
            return client;
        }

        internal byte[] GetObjectData(string bucketName, string key)
        {
            if (String.IsNullOrWhiteSpace(bucketName)) throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            _BucketsLock.EnterReadLock();
            try
            {
                if (!_Buckets.TryGetValue(bucketName, out S3LiteSyntheticBucket? bucket) || bucket == null)
                    throw new InvalidOperationException("Bucket '" + bucketName + "' does not exist.");

                if (!bucket.Objects.TryGetValue(key, out S3LiteSyntheticObject? obj) || obj == null)
                    throw new InvalidOperationException("Object '" + key + "' does not exist in bucket '" + bucketName + "'.");

                return (byte[])obj.Data.Clone();
            }
            finally
            {
                _BucketsLock.ExitReadLock();
            }
        }

        internal string GetObjectContentType(string bucketName, string key)
        {
            if (String.IsNullOrWhiteSpace(bucketName)) throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            _BucketsLock.EnterReadLock();
            try
            {
                if (!_Buckets.TryGetValue(bucketName, out S3LiteSyntheticBucket? bucket) || bucket == null)
                    throw new InvalidOperationException("Bucket '" + bucketName + "' does not exist.");

                if (!bucket.Objects.TryGetValue(key, out S3LiteSyntheticObject? obj) || obj == null)
                    throw new InvalidOperationException("Object '" + key + "' does not exist in bucket '" + bucketName + "'.");

                return obj.ContentType;
            }
            finally
            {
                _BucketsLock.ExitReadLock();
            }
        }

        internal string GetBucketRegion(string bucketName)
        {
            if (String.IsNullOrWhiteSpace(bucketName)) throw new ArgumentNullException(nameof(bucketName));

            _BucketsLock.EnterReadLock();
            try
            {
                if (!_Buckets.TryGetValue(bucketName, out S3LiteSyntheticBucket? bucket) || bucket == null)
                    throw new InvalidOperationException("Bucket '" + bucketName + "' does not exist.");

                return bucket.Region;
            }
            finally
            {
                _BucketsLock.ExitReadLock();
            }
        }

        internal bool ObjectExists(string bucketName, string key)
        {
            if (String.IsNullOrWhiteSpace(bucketName)) throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            _BucketsLock.EnterReadLock();
            try
            {
                if (!_Buckets.TryGetValue(bucketName, out S3LiteSyntheticBucket? bucket) || bucket == null) return false;
                return bucket.Objects.ContainsKey(key);
            }
            finally
            {
                _BucketsLock.ExitReadLock();
            }
        }

        internal void SeedBucket(string bucketName, string? region = null)
        {
            if (String.IsNullOrWhiteSpace(bucketName)) throw new ArgumentNullException(nameof(bucketName));
            string targetRegion = String.IsNullOrWhiteSpace(region) ? _Region : region;

            _BucketsLock.EnterWriteLock();
            try
            {
                if (_Buckets.ContainsKey(bucketName))
                    throw new InvalidOperationException("Bucket '" + bucketName + "' is already seeded.");

                _Buckets.Add(bucketName, new S3LiteSyntheticBucket(bucketName, targetRegion));
            }
            finally
            {
                _BucketsLock.ExitWriteLock();
            }
        }

        internal void SeedObject(string bucketName, string key, byte[]? data, string? contentType = "application/octet-stream")
        {
            if (String.IsNullOrWhiteSpace(bucketName)) throw new ArgumentNullException(nameof(bucketName));
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            _BucketsLock.EnterWriteLock();
            try
            {
                if (!_Buckets.TryGetValue(bucketName, out S3LiteSyntheticBucket? bucket) || bucket == null)
                    throw new InvalidOperationException("Bucket '" + bucketName + "' does not exist.");

                bucket.Objects[key] = new S3LiteSyntheticObject(key, data, contentType);
            }
            finally
            {
                _BucketsLock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (Interlocked.Exchange(ref _Disposed, 1) != 0) return;

            _CancellationTokenSource.Cancel();

            try
            {
                _Listener.Stop();
            }
            catch (HttpListenerException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            _Listener.Close();

            try
            {
                _ListenerTask.GetAwaiter().GetResult();
            }
            catch (HttpListenerException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            _CancellationTokenSource.Dispose();
            _BucketsLock.Dispose();
        }

        private static int GetAvailablePort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static Dictionary<string, string> ParseQueryString(string query)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (String.IsNullOrEmpty(query)) return values;

            string trimmed = query.StartsWith("?", StringComparison.Ordinal) ? query.Substring(1) : query;
            if (String.IsNullOrEmpty(trimmed)) return values;

            string[] pairs = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);

            foreach (string pair in pairs)
            {
                int separatorIndex = pair.IndexOf('=');
                if (separatorIndex >= 0)
                {
                    string key = Uri.UnescapeDataString(pair.Substring(0, separatorIndex));
                    string value = Uri.UnescapeDataString(pair.Substring(separatorIndex + 1));
                    values[key] = value;
                }
                else
                {
                    string key = Uri.UnescapeDataString(pair);
                    values[key] = String.Empty;
                }
            }

            return values;
        }

        private async ValueTask DisposeAsyncCore()
        {
            if (Interlocked.Exchange(ref _Disposed, 1) != 0) return;

            _CancellationTokenSource.Cancel();

            try
            {
                _Listener.Stop();
            }
            catch (HttpListenerException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            _Listener.Close();

            try
            {
                await _ListenerTask.ConfigureAwait(false);
            }
            catch (HttpListenerException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            _CancellationTokenSource.Dispose();
            _BucketsLock.Dispose();
        }

        private async Task HandleBucketDeleteAsync(HttpListenerContext context, string bucketName)
        {
            if (!RequestHasAuthorization(context.Request))
            {
                await SendErrorAsync(context.Response, ErrorCode.AccessDenied, "/" + bucketName).ConfigureAwait(false);
                return;
            }

            bool bucketMissing = false;
            bool bucketNotEmpty = false;

            _BucketsLock.EnterWriteLock();
            try
            {
                if (!_Buckets.TryGetValue(bucketName, out S3LiteSyntheticBucket? bucket) || bucket == null)
                {
                    bucketMissing = true;
                }
                else if (bucket.Objects.Count > 0)
                {
                    bucketNotEmpty = true;
                }
                else
                {
                    _Buckets.Remove(bucketName);
                }
            }
            finally
            {
                _BucketsLock.ExitWriteLock();
            }

            if (bucketMissing)
            {
                await SendErrorAsync(context.Response, ErrorCode.NoSuchBucket, "/" + bucketName).ConfigureAwait(false);
                return;
            }

            if (bucketNotEmpty)
            {
                await SendErrorAsync(context.Response, ErrorCode.BucketNotEmpty, "/" + bucketName).ConfigureAwait(false);
                return;
            }

            await WriteResponseAsync(context.Response, 204, "application/xml", Array.Empty<byte>(), false, null).ConfigureAwait(false);
        }

        private async Task HandleBucketExistsAsync(HttpListenerContext context, string bucketName)
        {
            bool exists = BucketExists(bucketName);

            if (exists)
            {
                await WriteResponseAsync(context.Response, 200, "application/xml", Array.Empty<byte>(), false, null).ConfigureAwait(false);
            }
            else
            {
                await SendErrorAsync(context.Response, ErrorCode.NoSuchBucket, "/" + bucketName, sendBody: false).ConfigureAwait(false);
            }
        }

        private async Task HandleBucketListAsync(HttpListenerContext context, string bucketName, Dictionary<string, string> query)
        {
            bool bucketMissing = false;
            ListBucketResult? result = null;
            Dictionary<string, string>? headers = null;

            _BucketsLock.EnterReadLock();
            try
            {
                if (!_Buckets.TryGetValue(bucketName, out S3LiteSyntheticBucket? bucket) || bucket == null)
                {
                    bucketMissing = true;
                }
                else
                {
                    string? prefix = query.ContainsKey("prefix") ? query["prefix"] : null;
                    string? marker = query.ContainsKey("marker") ? query["marker"] : null;
                    int maxKeys = query.ContainsKey("max-keys") && Int32.TryParse(query["max-keys"], out int parsedMaxKeys) ? parsedMaxKeys : 1000;
                    int startIndex = 0;

                    if (query.ContainsKey("continuation-token"))
                    {
                        Int32.TryParse(query["continuation-token"], out startIndex);
                    }
                    else if (!String.IsNullOrEmpty(marker))
                    {
                        List<string> markerOrderedKeys = bucket.Objects.Keys.OrderBy(key => key, StringComparer.Ordinal).ToList();
                        int markerIndex = markerOrderedKeys.FindIndex(key => String.Equals(key, marker, StringComparison.Ordinal));
                        if (markerIndex >= 0) startIndex = markerIndex + 1;
                    }

                    List<S3LiteSyntheticObject> filteredObjects = bucket.Objects.Values
                        .Where(obj => String.IsNullOrEmpty(prefix) || obj.Key.StartsWith(prefix, StringComparison.Ordinal))
                        .OrderBy(obj => obj.Key, StringComparer.Ordinal)
                        .ToList();

                    List<S3LiteSyntheticObject> page = filteredObjects
                        .Skip(startIndex)
                        .Take(maxKeys)
                        .ToList();

                    bool isTruncated = (startIndex + page.Count) < filteredObjects.Count;
                    string? nextToken = isTruncated ? (startIndex + page.Count).ToString() : null;

                    List<ObjectMetadata> metadata = new List<ObjectMetadata>();

                    foreach (S3LiteSyntheticObject obj in page)
                    {
                        metadata.Add(new ObjectMetadata(obj.Key, obj.LastModifiedUtc, obj.ETag, obj.Data.LongLength, _Owner));
                    }

                    result = new ListBucketResult(
                        bucket.Name,
                        metadata,
                        metadata.Count,
                        maxKeys,
                        prefix,
                        marker,
                        null,
                        isTruncated,
                        nextToken,
                        null,
                        bucket.Region);

                    headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    headers["x-amz-bucket-region"] = bucket.Region;
                }
            }
            finally
            {
                _BucketsLock.ExitReadLock();
            }

            if (bucketMissing)
            {
                await SendErrorAsync(context.Response, ErrorCode.NoSuchBucket, "/" + bucketName).ConfigureAwait(false);
                return;
            }

            await SendXmlAsync(
                context.Response,
                200,
                result ?? throw new InvalidOperationException("Bucket listing payload was not populated."),
                headers).ConfigureAwait(false);
        }

        private async Task HandleBucketWriteAsync(HttpListenerContext context, string bucketName, CancellationToken cancellationToken)
        {
            if (!RequestHasAuthorization(context.Request))
            {
                await SendErrorAsync(context.Response, ErrorCode.AccessDenied, "/" + bucketName).ConfigureAwait(false);
                return;
            }

            string region = _Region;
            bool bucketAlreadyExists = false;
            byte[] requestBody = await ReadRequestBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);

            if (requestBody.Length > 0)
            {
                try
                {
                    CreateBucketConfiguration configuration = _Serializer.DeserializeXml<CreateBucketConfiguration>(requestBody);
                    if (configuration != null && !String.IsNullOrWhiteSpace(configuration.LocationConstraint))
                        region = configuration.LocationConstraint;
                }
                catch (Exception)
                {
                    await SendErrorAsync(context.Response, ErrorCode.MalformedXML, "/" + bucketName).ConfigureAwait(false);
                    return;
                }
            }

            _BucketsLock.EnterWriteLock();
            try
            {
                if (_Buckets.ContainsKey(bucketName))
                {
                    bucketAlreadyExists = true;
                }
                else
                {
                    _Buckets.Add(bucketName, new S3LiteSyntheticBucket(bucketName, region));
                }
            }
            finally
            {
                _BucketsLock.ExitWriteLock();
            }

            if (bucketAlreadyExists)
            {
                await SendErrorAsync(context.Response, ErrorCode.BucketAlreadyOwnedByYou, "/" + bucketName).ConfigureAwait(false);
                return;
            }

            await WriteResponseAsync(context.Response, 200, "application/xml", Array.Empty<byte>(), true, null).ConfigureAwait(false);
        }

        private async Task HandleContextAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            try
            {
                _LastHostHeader = context.Request.UserHostName;
                _LastHttpMethod = context.Request.HttpMethod;
                _LastRequestHeaders = new NameValueCollection(context.Request.Headers);
                Uri requestUrl = context.Request.Url ?? throw new InvalidOperationException("Request URL is null.");
                _LastRawPathAndQuery = requestUrl.PathAndQuery;
                string absolutePath = Uri.UnescapeDataString(requestUrl.AbsolutePath ?? "/");
                Dictionary<string, string> query = ParseQueryString(requestUrl.Query);

                if (String.Equals(absolutePath, "/", StringComparison.Ordinal))
                {
                    await HandleServiceRouteAsync(context).ConfigureAwait(false);
                    return;
                }

                string trimmedPath = absolutePath.Trim('/');
                string[] segments = String.IsNullOrEmpty(trimmedPath)
                    ? Array.Empty<string>()
                    : trimmedPath.Split('/');

                if (segments.Length < 1)
                {
                    await SendErrorAsync(context.Response, ErrorCode.InvalidURI, absolutePath).ConfigureAwait(false);
                    return;
                }

                string bucketName = segments[0];
                string? key = segments.Length > 1 ? String.Join("/", segments.Skip(1)) : null;

                if (String.IsNullOrEmpty(key))
                {
                    if (String.Equals(context.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleBucketExistsAsync(context, bucketName).ConfigureAwait(false);
                    }
                    else if (String.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)
                        && query.ContainsKey("list-type"))
                    {
                        await HandleBucketListAsync(context, bucketName, query).ConfigureAwait(false);
                    }
                    else if (String.Equals(context.Request.HttpMethod, "PUT", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleBucketWriteAsync(context, bucketName, cancellationToken).ConfigureAwait(false);
                    }
                    else if (String.Equals(context.Request.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandleBucketDeleteAsync(context, bucketName).ConfigureAwait(false);
                    }
                    else
                    {
                        await SendErrorAsync(context.Response, ErrorCode.MethodNotAllowed, absolutePath).ConfigureAwait(false);
                    }

                    return;
                }

                if (String.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(context.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleObjectGetAsync(
                        context,
                        bucketName,
                        key,
                        String.Equals(context.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase)).ConfigureAwait(false);
                }
                else if (String.Equals(context.Request.HttpMethod, "PUT", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleObjectWriteAsync(context, bucketName, key, cancellationToken).ConfigureAwait(false);
                }
                else if (String.Equals(context.Request.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleObjectDeleteAsync(context, bucketName, key).ConfigureAwait(false);
                }
                else
                {
                    await SendErrorAsync(context.Response, ErrorCode.MethodNotAllowed, absolutePath, key).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                string payload = "Synthetic server error: " + exception.Message;
                await WriteResponseAsync(
                    context.Response,
                    500,
                    "text/plain",
                    Encoding.UTF8.GetBytes(payload),
                    true,
                    null).ConfigureAwait(false);
            }
            finally
            {
                try
                {
                    context.Response.Close();
                }
                catch (HttpListenerException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        private async Task HandleObjectDeleteAsync(HttpListenerContext context, string bucketName, string key)
        {
            if (!RequestHasAuthorization(context.Request))
            {
                await SendErrorAsync(context.Response, ErrorCode.AccessDenied, "/" + bucketName + "/" + key, key).ConfigureAwait(false);
                return;
            }

            bool bucketMissing = false;

            _BucketsLock.EnterWriteLock();
            try
            {
                if (!_Buckets.TryGetValue(bucketName, out S3LiteSyntheticBucket? bucket) || bucket == null)
                {
                    bucketMissing = true;
                }
                else
                {
                    bucket.Objects.Remove(key);
                }
            }
            finally
            {
                _BucketsLock.ExitWriteLock();
            }

            if (bucketMissing)
            {
                await SendErrorAsync(context.Response, ErrorCode.NoSuchBucket, "/" + bucketName + "/" + key, key).ConfigureAwait(false);
                return;
            }

            await WriteResponseAsync(context.Response, 204, "application/xml", Array.Empty<byte>(), false, null).ConfigureAwait(false);
        }

        private async Task HandleObjectGetAsync(HttpListenerContext context, string bucketName, string key, bool headOnly)
        {
            bool bucketMissing = false;
            bool objectMissing = false;
            byte[]? data = null;
            string? contentType = null;
            Dictionary<string, string>? headers = null;

            _BucketsLock.EnterReadLock();
            try
            {
                if (!_Buckets.TryGetValue(bucketName, out S3LiteSyntheticBucket? bucket) || bucket == null)
                {
                    bucketMissing = true;
                }
                else if (!bucket.Objects.TryGetValue(key, out S3LiteSyntheticObject? obj) || obj == null)
                {
                    objectMissing = true;
                }
                else
                {
                    headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    headers["ETag"] = "\"" + obj.ETag + "\"";
                    headers["x-amz-meta-s3b-last-modified"] = obj.LastModifiedUtc.ToString("o");
                    headers["x-amz-bucket-region"] = bucket.Region;
                    contentType = obj.ContentType;
                    data = (byte[])obj.Data.Clone();
                }
            }
            finally
            {
                _BucketsLock.ExitReadLock();
            }

            if (bucketMissing)
            {
                await SendErrorAsync(context.Response, ErrorCode.NoSuchBucket, "/" + bucketName + "/" + key, key, sendBody: !headOnly).ConfigureAwait(false);
                return;
            }

            if (objectMissing)
            {
                await SendErrorAsync(context.Response, ErrorCode.NoSuchKey, "/" + bucketName + "/" + key, key, sendBody: !headOnly).ConfigureAwait(false);
                return;
            }

            await WriteResponseAsync(context.Response, 200, contentType, data, !headOnly, headers).ConfigureAwait(false);
        }

        private async Task HandleObjectWriteAsync(HttpListenerContext context, string bucketName, string key, CancellationToken cancellationToken)
        {
            if (!RequestHasAuthorization(context.Request))
            {
                await SendErrorAsync(context.Response, ErrorCode.AccessDenied, "/" + bucketName + "/" + key, key).ConfigureAwait(false);
                return;
            }

            byte[] requestBody = await ReadRequestBodyAsync(context.Request, cancellationToken).ConfigureAwait(false);
            string contentType = context.Request.ContentType ?? "application/octet-stream";
            bool bucketMissing = false;

            _BucketsLock.EnterWriteLock();
            try
            {
                if (!_Buckets.TryGetValue(bucketName, out S3LiteSyntheticBucket? bucket) || bucket == null)
                {
                    bucketMissing = true;
                }
                else if (bucket.Objects.TryGetValue(key, out S3LiteSyntheticObject? existing) && existing != null)
                {
                    existing.Update(requestBody, contentType);
                }
                else
                {
                    bucket.Objects.Add(key, new S3LiteSyntheticObject(key, requestBody, contentType));
                }
            }
            finally
            {
                _BucketsLock.ExitWriteLock();
            }

            if (bucketMissing)
            {
                await SendErrorAsync(context.Response, ErrorCode.NoSuchBucket, "/" + bucketName + "/" + key, key).ConfigureAwait(false);
                return;
            }

            await WriteResponseAsync(context.Response, 200, "application/xml", Array.Empty<byte>(), true, null).ConfigureAwait(false);
        }

        private async Task HandleServiceRouteAsync(HttpListenerContext context)
        {
            if (!String.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                await SendErrorAsync(context.Response, ErrorCode.MethodNotAllowed, "/").ConfigureAwait(false);
                return;
            }

            if (!RequestHasAuthorization(context.Request))
            {
                await SendErrorAsync(context.Response, ErrorCode.AccessDenied, "/").ConfigureAwait(false);
                return;
            }

            ListAllMyBucketsResult result;

            _BucketsLock.EnterReadLock();
            try
            {
                List<Bucket> buckets = _Buckets.Values
                    .OrderBy(bucket => bucket.Name, StringComparer.Ordinal)
                    .Select(bucket => new Bucket(bucket.Name, bucket.CreatedUtc))
                    .ToList();

                result = new ListAllMyBucketsResult(_Owner, new Buckets(buckets));
            }
            finally
            {
                _BucketsLock.ExitReadLock();
            }

            await SendXmlAsync(context.Response, 200, result, null).ConfigureAwait(false);
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context;

                try
                {
                    context = await _Listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                await HandleContextAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<byte[]> ReadRequestBodyAsync(HttpListenerRequest request, CancellationToken cancellationToken)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                await request.InputStream.CopyToAsync(stream, 81920, cancellationToken).ConfigureAwait(false);
                return stream.ToArray();
            }
        }

        private static bool RequestHasAuthorization(HttpListenerRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return !String.IsNullOrWhiteSpace(request.Headers["Authorization"]);
        }

        private async Task SendErrorAsync(
            HttpListenerResponse response,
            ErrorCode errorCode,
            string resource,
            string? key = null,
            bool sendBody = true)
        {
            Error error = new Error(errorCode, key, null, Guid.NewGuid().ToString("N"), resource);
            string xml = _Serializer.SerializeXml(error);
            byte[] data = Encoding.UTF8.GetBytes(xml);

            await WriteResponseAsync(
                response,
                error.HttpStatusCode,
                "application/xml",
                data,
                sendBody,
                null).ConfigureAwait(false);
        }

        private async Task SendXmlAsync(HttpListenerResponse response, int statusCode, object payload, Dictionary<string, string>? headers)
        {
            string xml = _Serializer.SerializeXml(payload);
            byte[] data = Encoding.UTF8.GetBytes(xml);
            await WriteResponseAsync(response, statusCode, "application/xml", data, true, headers).ConfigureAwait(false);
        }

        private static async Task WriteResponseAsync(
            HttpListenerResponse response,
            int statusCode,
            string? contentType,
            byte[]? data,
            bool sendBody,
            Dictionary<string, string>? headers)
        {
            response.StatusCode = statusCode;
            response.ContentType = contentType ?? "application/octet-stream";

            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    response.Headers[header.Key] = header.Value;
                }
            }

            byte[] payload = data ?? Array.Empty<byte>();
            response.ContentLength64 = payload.LongLength;

            if (sendBody && payload.Length > 0)
            {
                await response.OutputStream.WriteAsync(payload, 0, payload.Length).ConfigureAwait(false);
            }
        }
    }
}
