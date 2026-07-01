namespace S3Lite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AWSSignatureGenerator;
    using PrettyId;
    using RestWrapper;
    using S3Lite.ApiObjects;
    
    /// <summary>
    /// Amazon S3 storage client.
    /// </summary>
    public class S3Client
    {
        #region Public-Members

        /// <summary>
        /// Stream buffer size.
        /// </summary>
        public int StreamBufferSize
        {
            get
            {
                return _StreamBufferSize;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(StreamBufferSize));
                _StreamBufferSize = value;
            }
        }

        /// <summary>
        /// Method to invoke to send log messages.
        /// </summary>
        public Action<string> Logger { get; set; } = null;

        /// <summary>
        /// Access key, generally a base64-encoded string.
        /// When null or empty, anonymous access mode is used (no request signing).
        /// </summary>
        public string AccessKey
        {
            get
            {
                return _AccessKey;
            }
            set
            {
                _AccessKey = value;
            }
        }

        /// <summary>
        /// Secret access key, generally a base64-encoded string.
        /// When null or empty, anonymous access mode is used (no request signing).
        /// </summary>
        public string SecretKey
        {
            get
            {
                return _SecretKey;
            }
            set
            {
                _SecretKey = value;
            }
        }

        /// <summary>
        /// Boolean indicating whether credentials are configured.
        /// When false, anonymous access mode is used (no request signing).
        /// </summary>
        public bool HasCredentials
        {
            get
            {
                return !String.IsNullOrEmpty(_AccessKey) && !String.IsNullOrEmpty(_SecretKey);
            }
        }

        /// <summary>
        /// Signature version.  Default is version 4.
        /// </summary>
        public SignatureVersionEnum SignatureVersion
        {
            get
            {
                return _SignatureVersion;
            }
            set
            {
                _SignatureVersion = value;
            }
        }

        /// <summary>
        /// Request style.  Default is virtual hosted style.
        /// </summary>
        public RequestStyleEnum RequestStyle
        {
            get
            {
                return _RequestStyle;
            }
            set
            {
                _RequestStyle = value;
            }
        }

        /// <summary>
        /// Region.
        /// </summary>
        public string Region
        {
            get
            {
                return _Region;
            }
            set
            {
                _Region = value;
            }
        }

        /// <summary>
        /// Protocol, e.g. HTTP or HTTPS.
        /// </summary>
        public ProtocolEnum Protocol
        {
            get
            {
                return _Protocol;
            }
            set
            {
                _Protocol = value;
            }
        }

        /// <summary>
        /// Hostname.
        /// </summary>
        public string Hostname
        {
            get
            {
                return _Hostname;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Hostname));
                _Hostname = value;
            }
        }

        /// <summary>
        /// Port.
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
            set
            {
                if (value < 0 || value > 65535) throw new ArgumentOutOfRangeException(nameof(Port));
                _Port = value;
            }
        }

        /// <summary>
        /// Gateway settings used to route requests through an intermediate reverse proxy / gateway while the
        /// SigV4 signature stays bound to the upstream endpoint described by <see cref="Hostname" />. The gateway
        /// is expected to rewrite the Host header to the upstream request authority the signature was computed for
        /// (bucket-prefixed for virtual-hosted-style, the bare endpoint - including any non-standard port - for
        /// path-style) before forwarding upstream. When null, requests are sent directly to <see cref="Hostname" />.
        /// This is not a forward/HTTP (CONNECT) proxy; for that, supply an HttpClient configured with a
        /// WebProxy via <see cref="WithHttpClient" /> and leave this null.
        /// </summary>
        public GatewayConfig Gateway
        {
            get
            {
                return _Gateway;
            }
            set
            {
                _Gateway = value;
            }
        }

        /// <summary>
        /// Service APIs.
        /// </summary>
        public ServiceApis Service { get; private set; }

        /// <summary>
        /// Bucket APIs.
        /// </summary>
        public BucketApis Bucket { get; private set; }

        /// <summary>
        /// Object APIs.
        /// </summary>
        public ObjectApis Object { get; private set; }

        /// <summary>
        /// Debug settings.
        /// </summary>
        public DebugSettings Debug { get; private set; } = new DebugSettings();

        /// <summary>
        /// Serialization helper.
        /// </summary>
        public SerializationHelper Serializer
        {
            get
            {
                return _Serializer;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Serializer));
                _Serializer = value;
            }
        }

        /// <summary>
        /// Caller-supplied HTTP client instance.
        /// When null, RestWrapper creates and owns its own transport for each request.
        /// When non-null, the caller owns the lifetime and transport configuration.
        /// </summary>
        public HttpClient HttpClient
        {
            get
            {
                return _HttpClient;
            }
            set
            {
                _HttpClient = value;
            }
        }

        #endregion

        #region Private-Members

        private string _Header = "[S3Lite] ";
        private int _StreamBufferSize = 65536;
        private ProtocolEnum _Protocol = ProtocolEnum.Https;
        private string _Region = "us-west-1";
        private string _Hostname = "amazonaws.com";
        private int _Port = 443;
        private GatewayConfig _Gateway = null;

        private RequestStyleEnum _RequestStyle = RequestStyleEnum.VirtualHostedStyle;
        private SignatureVersionEnum _SignatureVersion = SignatureVersionEnum.Version4;
        private string _AccessKey = null;
        private string _SecretKey = null;
        private HttpClient _HttpClient = null;

        private SerializationHelper _Serializer = new SerializationHelper();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public S3Client()
        {
            Service = new ServiceApis(this);
            Bucket = new BucketApis(this);
            Object = new ObjectApis(this);
        }

        /// <summary>
        /// Instantiate using a caller-supplied HTTP client.
        /// The supplied client remains caller-owned and will not be disposed by S3Lite.
        /// </summary>
        /// <param name="httpClient">HTTP client instance to reuse for requests.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/> is null.</exception>
        public S3Client(HttpClient httpClient) : this()
        {
            if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));
            _HttpClient = httpClient;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Specify the logger method.
        /// </summary>
        /// <param name="logger">Logger method.</param>
        /// <returns>S3Client.</returns>
        public S3Client WithLogger(Action<string> logger)
        {
            Logger = logger;
            return this;
        }

        /// <summary>
        /// Specify a caller-supplied HTTP client.
        /// Pass null to return to RestWrapper-managed transport behavior.
        /// The supplied client remains caller-owned and will not be disposed by S3Lite.
        /// </summary>
        /// <param name="httpClient">HTTP client instance, or null to clear a previously assigned client.</param>
        /// <returns>S3Client.</returns>
        public S3Client WithHttpClient(HttpClient httpClient)
        {
            _HttpClient = httpClient;
            return this;
        }

        /// <summary>
        /// Specify the region.
        /// </summary>
        /// <param name="region">Region string.</param>
        /// <returns>S3Client.</returns>
        public S3Client WithRegion(string region)
        {
            _Region = region;
            return this;
        }

        /// <summary>
        /// Specify the access key.
        /// Pass null or empty string to use anonymous access mode.
        /// </summary>
        /// <param name="accessKey">Access key, or null for anonymous access.</param>
        /// <returns>S3Client.</returns>
        public S3Client WithAccessKey(string accessKey)
        {
            _AccessKey = accessKey;
            return this;
        }

        /// <summary>
        /// Specify the secret key.
        /// Pass null or empty string to use anonymous access mode.
        /// </summary>
        /// <param name="secretKey">Secret key, or null for anonymous access.</param>
        /// <returns>S3Client.</returns>
        public S3Client WithSecretKey(string secretKey)
        {
            _SecretKey = secretKey;
            return this;
        }

        /// <summary>
        /// Specify the signature version.
        /// </summary>
        /// <param name="version">Version.</param>
        /// <returns>S3Client.</returns>
        public S3Client WithSignatureVersion(SignatureVersionEnum version)
        {
            _SignatureVersion = version;
            return this;
        }

        /// <summary>
        /// Specify the request style.
        /// </summary>
        /// <param name="style">Request style.</param>
        /// <returns>S3Client.</returns>
        public S3Client WithRequestStyle(RequestStyleEnum style)
        {
            _RequestStyle = style;
            return this;
        }

        /// <summary>
        /// Specify the protocol.
        /// </summary>
        /// <param name="protocol">Protocol.</param>
        /// <returns>S3Client.</returns>
        public S3Client WithProtocol(ProtocolEnum protocol)
        {
            _Protocol = protocol;
            return this;
        }

        /// <summary>
        /// Specify the hostname.
        /// </summary>
        /// <param name="hostname">Hostname.</param>
        /// <returns>S3Client.</returns>
        public S3Client WithHostname(string hostname)
        {
            if (String.IsNullOrEmpty(hostname)) throw new ArgumentNullException(nameof(hostname));
            _Hostname = hostname;
            return this;
        }

        /// <summary>
        /// Specify the port.
        /// </summary>
        /// <param name="port">Port.</param>
        /// <returns>S3Client.</returns>
        public S3Client WithPort(int port)
        {
            _Port = port;
            return this;
        }

        /// <summary>
        /// Specify gateway settings used to route requests through an intermediate reverse proxy / gateway
        /// while the SigV4 signature stays bound to the upstream endpoint described by <see cref="Hostname" />.
        /// The gateway is expected to rewrite the Host header to the upstream request authority the signature
        /// was computed for (bucket-prefixed for virtual-hosted-style, the bare endpoint - including any
        /// non-standard port - for path-style) before forwarding upstream. Pass null to send requests directly
        /// to <see cref="Hostname" />.
        /// This is not a forward/HTTP (CONNECT) proxy; for that, supply an HttpClient configured with a
        /// WebProxy via <see cref="WithHttpClient" /> and leave this null.
        /// </summary>
        /// <param name="gateway">Gateway settings, or null to disable gateway routing.</param>
        /// <returns>S3Client.</returns>
        public S3Client WithGateway(GatewayConfig gateway)
        {
            _Gateway = gateway;
            return this;
        }

        /// <summary>
        /// Enable signature debugging.
        /// </summary>
        /// <returns></returns>
        public S3Client EnableSignatureDebug()
        {
            Debug.Signatures = true;
            return this;
        }

        #endregion

        #region Internal-Methods

        /// <summary>
        /// Build the URL given the request style, protocol, hostname, port, region, bucket, and key.
        /// </summary>
        /// <param name="bucket">Bucket.</param>
        /// <param name="key">Key.</param>
        /// <param name="versionId">Version ID.</param>
        /// <returns>String.</returns>
        internal string BuildUrl(
            string bucket = null, 
            string key = null,
            string versionId = null)
        {
            #region Base-URL

            string url = "";
            if (_RequestStyle == RequestStyleEnum.PathStyle) url = Constants.UrlPatternPathStyle;
            else if (_RequestStyle == RequestStyleEnum.VirtualHostedStyle) url = Constants.UrlPatternVirtualHosted;
            else throw new ArgumentException("Unknown request style '" + _RequestStyle.ToString() + "'.");

            if (_Protocol == ProtocolEnum.Http) url = url.Replace("{protocol}", "http");
            else if (_Protocol == ProtocolEnum.Https) url = url.Replace("{protocol}", "https");
            else throw new ArgumentException("Unknown protocol '" + _Protocol.ToString() + "'.");

            url = url.Replace("{service}", "s3");

            if (String.IsNullOrEmpty(_Region)) url = url.Replace("{region}.", "");
            else url = url.Replace("{region}.", _Region + ".");

            url = url
                .Replace("{hostname}", _Hostname)
                .Replace("{port}", _Port.ToString());

            if (String.IsNullOrEmpty(bucket))
            {
                url = url
                    .Replace("{bucket}.", "")
                    .Replace("{bucket}/", "")
                    .Replace("{key}", "");
            }
            else
            {
                url = url.Replace("{bucket}", bucket);

                if (String.IsNullOrEmpty(key))
                {
                    url = url.Replace("{key}", "");
                }
                else
                {
                    url = url.Replace("{key}", key);
                }
            }

            #endregion

            #region Query

            if (!String.IsNullOrEmpty(versionId))
            {
                url += "?versionId=" + versionId;
            }

            #endregion

            return url;
        }

        /// <summary>
        /// Create a RESTful request with standard headers.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="url">URL.</param>
        /// <param name="contentType">Content type.</param>
        /// <returns>RestRequest.</returns>
        internal RestRequest BuildRestRequest(HttpMethod method, string url, string contentType = null)
        {
            RestRequest req;

            if (_HttpClient != null)
                req = new RestRequest(url, method, contentType, _HttpClient);
            else
                req = new RestRequest(url, method, contentType);

            req.Headers.Add(Constants.HeaderUserAgent, Constants.HeaderUserAgentValue);

            if (!String.IsNullOrEmpty(contentType))
                req.Headers.Add(Constants.HeaderContentType, contentType);

            Logger?.Invoke(_Header + "returning REST request: " + method.ToString() + " " + url);
            return req;
        }

        /// <summary>
        /// Construct an authorization header (if credentials are configured) and submit RESTful request to retrieve a response.
        /// When no credentials are configured, the request is sent without signing (anonymous access mode).
        /// </summary>
        /// <param name="req">RestRequest.</param>
        /// <param name="data">Byte array containing data.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>RestResponse.</returns>
        internal async Task<RestResponse> BuildRestResponse(RestRequest req, byte[] data = null, CancellationToken token = default)
        {
            string header = _Header + req.Method.ToString() + " " + req.Url + ": ";
            string timestamp = DateTime.UtcNow.ToString(Constants.TimestampFormatCompact);
            string hash = BytesToHexString(Sha256(data)).ToLower();

            Uri uri = new Uri(req.Url);

            if (req.Headers == null)
                req.Headers = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

            if (req.Headers.AllKeys.Contains(Constants.HeaderHost))
                req.Headers.Remove(Constants.HeaderHost);

            // For standard ports (80 for HTTP, 443 for HTTPS), omit the port from the host header
            // AWS signature verification expects this format.
            bool isStandardPort = (uri.Scheme == "https" && uri.Port == 443) ||
                                  (uri.Scheme == "http" && uri.Port == 80);
            string hostHeader = isStandardPort ? uri.Host : uri.Host + ":" + uri.Port.ToString();

            if (!req.Headers.AllKeys.Contains(Constants.HeaderAmazonDate))
                req.Headers.Add(Constants.HeaderAmazonDate, timestamp);

            if (!req.Headers.AllKeys.Contains(Constants.HeaderAmazonContentSha256))
                req.Headers.Add(Constants.HeaderAmazonContentSha256, hash);

            NameValueCollection signingHeaders = new NameValueCollection(req.Headers);
            signingHeaders.Add(Constants.HeaderHost, hostHeader);
            signingHeaders = SortNameValueCollection(signingHeaders);
            req.Headers = SortNameValueCollection(req.Headers);

            if (HasCredentials)
            {
                V4SignatureResult signature = new V4SignatureResult(
                    timestamp,
                    req.Method.ToString().ToUpper(),
                    req.Url,
                    AccessKey,
                    SecretKey,
                    Region,
                    "s3",
                    signingHeaders,
                    data
                    );

                if (Debug.Signatures)
                    Logger?.Invoke(_Header + Environment.NewLine + signature);

                req.Authorization.Raw = signature.AuthorizationHeader;
            }
            else
            {
                Logger?.Invoke(_Header + "anonymous access mode, skipping request signing");
            }

            if (_Gateway != null && !String.IsNullOrEmpty(_Gateway.Hostname))
            {
                // Redirect the connection (scheme/host/port) to the gateway while the signature stays bound to
                // the upstream request authority. The signature was already computed above from req.Url (the
                // upstream URL), so its signed Host is the upstream authority - bucket.s3.region.hostname for
                // virtual-hosted-style, or s3.region.hostname[:port] for path-style - not necessarily Hostname
                // verbatim. The transport derives the Host header from this rewritten URL, so the gateway
                // receives Host = the gateway host and is expected to rewrite it back to that upstream authority
                // before forwarding upstream (the corporate reverse-proxy case). Only scheme/host/port change
                // here; the path, query, body, and signature are untouched.
                //
                // This is not a forward/CONNECT proxy. For a forward proxy that must preserve the upstream Host
                // end-to-end, leave Gateway null and supply an HttpClient configured with a WebProxy
                // (see WithHttpClient).
                int gatewayPort = _Gateway.Port > 0 ? _Gateway.Port : _Port;
                UriBuilder ub = new UriBuilder(req.Url);
                if (_Gateway.Protocol.HasValue)
                    ub.Scheme = _Gateway.Protocol.Value == ProtocolEnum.Https ? "https" : "http";
                ub.Host = _Gateway.Hostname;
                ub.Port = gatewayPort;
                req.Url = ub.ToString();
            }

            RestResponse resp;

            if (data == null || data.Length < 1)
                resp = await req.SendAsync(token).ConfigureAwait(false);
            else
                resp = await req.SendAsync(data, token).ConfigureAwait(false);

            if (resp != null)
            {
                resp.Time.End = DateTime.UtcNow;

                if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                {
                    Logger?.Invoke(header + resp.StatusCode + " (" + resp.Time.TotalMs + "ms)");
                }
                else
                {
                    Logger?.Invoke(header + resp.StatusCode + " (" + resp.Time.TotalMs + "ms)" + Environment.NewLine + resp.DataAsString);
                }
            }
            else
            {
                Logger?.Invoke(header + "(null)");
            }

            return resp;
        }

        internal WebException WebExceptionBuilder(int? statusCode, string url, string reqBody, string respBody)
        {
            string statusStr = "(null)";
            if (statusCode != null) statusStr = statusCode.Value.ToString();

            Logger?.Invoke(_Header + "creating web exception for status " + statusStr + " URL " + url);

            WebException ret;
            
            if (statusCode == null)
                throw new WebException(MessageFromStatusCode(null, url));

            string message = MessageFromStatusCode(statusCode, url);
            string key = null;
            string versionId = null;
            string requestId = null;
            string resource = null;
            string errorCode = null;

            if (!String.IsNullOrEmpty(respBody))
            {
                try
                {
                    Error error = Serializer.DeserializeXml<Error>(respBody);
                    message = error.Message;
                    key = error.Key;
                    versionId = error.VersionId;
                    requestId = error.RequestId;
                    resource = error.Resource;
                    errorCode = error.Code.ToString();
                }
                catch (Exception e)
                {
                    Logger?.Invoke(_Header + "unable to deserialize error response body" + Environment.NewLine + e.ToString());
                }
            }

            ret = new WebException(message);
            ret.Data.Add("StatusCode", statusCode);
            ret.Data.Add("URL", url);
            ret.Data.Add("RequestBody", reqBody);
            ret.Data.Add("ResponseBody", respBody);

            if (!String.IsNullOrEmpty(key)) ret.Data.Add("Key", key);
            if (!String.IsNullOrEmpty(versionId)) ret.Data.Add("VersionId", versionId);
            if (!String.IsNullOrEmpty(requestId)) ret.Data.Add("RequestId", requestId);
            if (!String.IsNullOrEmpty(resource)) ret.Data.Add("Resource", resource);
            if (!String.IsNullOrEmpty(errorCode)) ret.Data.Add("ErrorCode", errorCode);

            return ret;
        }

        /// <summary>
        /// Generate a SHA256 hash for a stream.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <returns>Hash.</returns>
        internal byte[] Sha256(Stream stream)
        {
            if (stream == null) return BytesFromHexString(Constants.EmptySha256Hash);

            using (SHA256 sha256 = SHA256.Create())
            {
                if (stream.CanSeek)
                    stream.Seek(0, SeekOrigin.Begin);

                byte[] hash = sha256.ComputeHash(stream);

                if (stream.CanSeek)
                    stream.Seek(0, SeekOrigin.Begin);

                return hash;
            }
        }

        /// <summary>
        /// Generate a SHA256 hash for a byte array.
        /// </summary>
        /// <param name="data">Byte array.</param>
        /// <returns>Hash.</returns>
        internal byte[] Sha256(byte[] data)
        {
            if (data == null) data = Array.Empty<byte>();

            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }

        #endregion

        #region Private-Methods

        private NameValueCollection SortNameValueCollection(NameValueCollection nvc)
        {
            SortedDictionary<string, string> sorted = new SortedDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (string key in nvc.AllKeys)
            {
                sorted.Add(key, nvc.Get(key));
            }

            sorted.OrderBy(k => k.Key);

            NameValueCollection ret = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

            foreach (KeyValuePair<string, string> kvp in sorted)
            {
                ret.Add(kvp.Key, kvp.Value);
            }

            return ret;
        }

        private string MessageFromStatusCode(int? statusCode, string url)
        {
            // https://docs.aws.amazon.com/AmazonS3/latest/API/ErrorResponses.html#ErrorCodeList

            if (statusCode == null) return "Unable to connect to the specified URL: " + url + ".";

            switch (statusCode.Value)
            {
                case 301: // moved permanently
                    return "The resource at the following URL has permanently moved: " + url;

                case 304: // not modified
                    return "The resource at the following URL has not been modified: " + url;

                case 307: // moved temporarily
                    return "The resource at the following URL has moved temporarily: " + url;

                case 400:
                    return "The request to the following URL was invalid: " + url;

                case 401:
                    return "The request to the following URL was not authorized: " + url;

                case 403:
                    return "The request to the following URL was forbidden: " + url;

                case 404:
                    return "The resource at the following URL was not found: " + url;

                case 405:
                    return "The specified method was not allowed for the following URL: " + url;

                case 409:
                    return "The requested operation at the following URL could not be completed due to conflict: " + url;

                case 411: // length required
                    return "The request operation at the following URL failed due to a lack of a supplied length: " + url;

                case 412: // precondition failed
                    return "A precondition failed for the request direct at the following URL: " + url;

                case 416: // range not satisfiable
                    return "The requested range could not be satisfied for the following URL: " + url;

                case 500:
                    return "An internal server error was encountered while handling the request for URL: " + url;

                case 501: // not implemented
                    return "A supplied header implies functionality that is not implemented for URL: " + url;

                case 503: // service unavailable or slow down
                    return "Either the service is unavailable or your request rate must decrease for URL: " + url;

                case 507: // insufficient storage
                    return "Insufficient storage is available to satisfy the request to URL: " + url;

                default:
                    return "An unknown HTTP status code of " + statusCode.Value + " was returned for URL: " + url;
            }
        }

        private string BytesToHexString(byte[] bytes)
        {
            // NOT supported in netstandard2.1!
            // return Convert.ToHexString(bytes);  

            return BitConverter.ToString(bytes).Replace("-", "");
        }

        private byte[] BytesFromHexString(string hex)
        {
            // NOT supported in netstandard2.1!
            // return Convert.FromHexString(hex);

            int chars = hex.Length;
            byte[] bytes = new byte[chars / 2];
            for (int i = 0; i < chars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        #endregion
    }
}
