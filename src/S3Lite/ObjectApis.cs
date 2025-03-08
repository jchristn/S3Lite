namespace S3Lite
{
    using System;
    using RestWrapper;
    using System.Net.Http;
    using System.Net;
    using System.Threading.Tasks;
    using System.Threading;
    using S3Lite.ApiObjects;
    using System.IO;
    using System.Linq;
    using System.Collections.Specialized;

    /// <summary>
    /// Amazon S3 object APIs.
    /// </summary>
    public class ObjectApis
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[S3Object] ";
        private S3Client _Client = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ObjectApis(S3Client client)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Check object existence.
        /// </summary>
        /// <param name="bucket">Bucket.</param>
        /// <param name="key">Key.</param>
        /// <param name="versionId">Version ID.</param>
        /// <param name="headers">Additional headers to add to the request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Boolean indicating existence.</returns>
        public async Task<bool> ExistsAsync(
            string bucket, 
            string key, 
            string versionId = null, 
            NameValueCollection headers = null,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            string header = _Header + "Exists ";

            string url = _Client.BuildUrl(bucket, key, versionId);

            using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Get, url))
            {
                _Client.Logger?.Invoke(_Header + "HEAD " + url);
                req.Headers = headers;

                using (RestResponse resp = await _Client.BuildRestResponse(req, null, token).ConfigureAwait(false))
                {
                    if (resp == null)
                    {
                        _Client.Logger?.Invoke(header + "no response from " + url);
                        throw new WebException(Constants.NoRestResponseExceptionMessage + url);
                    }
                    else if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                    {
                        _Client.Logger?.Invoke(header + resp.StatusCode + " response from " + url);
                        return true;
                    }
                    else if (resp.StatusCode == 404)
                    {
                        _Client.Logger?.Invoke(header + resp.StatusCode + " response from " + url);
                        return false;
                    }
                    else
                    {
                        _Client.Logger?.Invoke(header + "request failed" + Environment.NewLine + resp.DataAsString);
                        WebException e = _Client.WebExceptionBuilder(resp.StatusCode, url, null, resp.DataAsString);
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        /// Get object metadata.
        /// </summary>
        /// <param name="bucket">Bucket.</param>
        /// <param name="key">Key.</param>
        /// <param name="versionId">Version ID.</param>
        /// <param name="headers">Additional headers to add to the request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Object metadata.</returns>
        public async Task<ObjectMetadata> GetMetadataAsync(
            string bucket, 
            string key, 
            string versionId = null,
            NameValueCollection headers = null,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            string header = _Header + "GetMetadata ";

            string url = _Client.BuildUrl(bucket, key, versionId);

            using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Get, url))
            {
                _Client.Logger?.Invoke(_Header + "HEAD " + url);
                req.Headers = headers;

                using (RestResponse resp = await _Client.BuildRestResponse(req, null, token).ConfigureAwait(false))
                {
                    if (resp == null)
                    {
                        _Client.Logger?.Invoke(header + "no response from " + url);
                        throw new WebException(Constants.NoRestResponseExceptionMessage + url);
                    }
                    else if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                    {
                        _Client.Logger?.Invoke(header + resp.StatusCode + " response from " + url);

                        ObjectMetadata ret = new ObjectMetadata();
                        ret.Key = key;
                        ret.Size = resp.ContentLength != null ? resp.ContentLength.Value : 0;
                        ret.ContentType = resp.ContentType;

                        if (resp.Headers.AllKeys.Contains("etag"))
                            ret.ETag = resp.Headers.Get("etag");

                        if (resp.Headers.AllKeys.Contains("x-amz-meta-s3b-last-modified"))
                            ret.LastModified = DateTime.Parse(resp.Headers.Get("x-amz-meta-s3b-last-modified"));

                        return ret;
                    }
                    else
                    {
                        _Client.Logger?.Invoke(header + "request failed" + Environment.NewLine + resp.DataAsString);
                        WebException e = _Client.WebExceptionBuilder(resp.StatusCode, url, null, resp.DataAsString);
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        /// Get object.
        /// </summary>
        /// <param name="bucket">Bucket.</param>
        /// <param name="key">Key.</param>
        /// <param name="versionId">Version ID.</param>
        /// <param name="headers">Additional headers to add to the request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Object data.</returns>
        public async Task<byte[]> GetAsync(
            string bucket, 
            string key, 
            string versionId = null,
            NameValueCollection headers = null,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            string header = _Header + "Get ";

            string url = _Client.BuildUrl(bucket, key, versionId);

            using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Get, url))
            {
                _Client.Logger?.Invoke(_Header + "GET " + url);
                req.Headers = null;

                using (RestResponse resp = await _Client.BuildRestResponse(req, null, token).ConfigureAwait(false))
                {
                    if (resp == null)
                    {
                        _Client.Logger?.Invoke(header + "no response from " + url);
                        throw new WebException(Constants.NoRestResponseExceptionMessage + url);
                    }
                    else if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                    {
                        _Client.Logger?.Invoke(header + resp.StatusCode + " response from " + url);
                        return resp.DataAsBytes;
                    }
                    else
                    {
                        _Client.Logger?.Invoke(header + "request failed" + Environment.NewLine + resp.DataAsString);
                        WebException e = _Client.WebExceptionBuilder(resp.StatusCode, url, null, resp.DataAsString);
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        /// Write object.
        /// </summary>
        /// <param name="bucket">Bucket.</param>
        /// <param name="key">Key.</param>
        /// <param name="data">Data.</param>
        /// <param name="contentType">Content type.</param>
        /// <param name="versionId">Version ID.</param>
        /// <param name="headers">Additional headers to add to the request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task WriteAsync(
            string bucket, 
            string key, 
            byte[] data, 
            string contentType = "application/octet-stream",
            string versionId = null,
            NameValueCollection headers = null,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (data == null) data = Array.Empty<byte>();

            string header = _Header + "Write ";

            string url = _Client.BuildUrl(bucket, key, versionId);

            using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Put, url))
            {
                _Client.Logger?.Invoke(_Header + "PUT " + url);
                req.Headers = headers;
                req.ContentType = contentType;

                using (RestResponse resp = await _Client.BuildRestResponse(req, data, token).ConfigureAwait(false))
                {
                    if (resp == null)
                    {
                        _Client.Logger?.Invoke(header + "no response from " + url);
                        throw new WebException(Constants.NoRestResponseExceptionMessage + url);
                    }
                    else if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                    {
                        _Client.Logger?.Invoke(header + resp.StatusCode + " response from " + url);
                        return;
                    }
                    else
                    {
                        _Client.Logger?.Invoke(header + "request failed" + Environment.NewLine + resp.DataAsString);
                        WebException e = _Client.WebExceptionBuilder(resp.StatusCode, url, null, resp.DataAsString);
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        /// Delete object.
        /// </summary>
        /// <param name="bucket">Bucket.</param>
        /// <param name="key">Key.</param>
        /// <param name="versionId">Version ID.</param>
        /// <param name="headers">Additional headers to add to the request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task DeleteAsync(
            string bucket, 
            string key, 
            string versionId = null,
            NameValueCollection headers = null,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket));
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            string header = _Header + "Delete ";

            string url = _Client.BuildUrl(bucket, key, versionId);

            using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Delete, url))
            {
                _Client.Logger?.Invoke(_Header + "DELETE " + url);
                req.Headers = headers;

                using (RestResponse resp = await _Client.BuildRestResponse(req, null, token).ConfigureAwait(false))
                {
                    if (resp == null)
                    {
                        _Client.Logger?.Invoke(header + "no response from " + url);
                        throw new WebException(Constants.NoRestResponseExceptionMessage + url);
                    }
                    else if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                    {
                        _Client.Logger?.Invoke(header + resp.StatusCode + " response from " + url);
                        return;
                    }
                    else
                    {
                        _Client.Logger?.Invoke(header + "request failed" + Environment.NewLine + resp.DataAsString);
                        WebException e = _Client.WebExceptionBuilder(resp.StatusCode, url, null, resp.DataAsString);
                        throw e;
                    }
                }
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}