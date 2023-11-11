namespace S3Lite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Threading;
    using RestWrapper;
    using S3Lite.ApiObjects;
    using System.Web;
    using System.Text;
    using System.Runtime.Serialization.Json;

    /// <summary>
    /// Amazon S3 bucket APIs.
    /// </summary>
    public class BucketApis
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[S3Bucket] ";
        private S3Client _Client = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public BucketApis(S3Client client)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// List the contents of a bucket.
        /// </summary>
        /// <param name="bucket">Bucket.</param>
        /// <param name="headers">Additional headers to add to the request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Boolean indicating existence.</returns>
        public async Task<bool> ExistsAsync(
            string bucket,
            NameValueCollection headers = null,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket));

            string header = _Header + "Exists ";

            string url = _Client.BuildUrl(bucket, null, null);

            using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Head, url))
            {
                _Client.Logger?.Invoke(_Header + "GET " + url);
                req.Headers = headers;

                using (RestResponse resp = await _Client.BuildRestResponse(req, null, token).ConfigureAwait(false))
                {
                    if (resp == null)
                    {
                        _Client.Logger?.Invoke(header + "no response from " + url);
                        WebException e = _Client.WebExceptionBuilder(null, url, null, null);
                        throw e;
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
        /// List the contents of a bucket.
        /// </summary>
        /// <param name="bucket">Bucket.</param>
        /// <param name="prefix">Prefix.</param>
        /// <param name="marker">Marker.</param>
        /// <param name="continuationToken">Continuation token.</param>
        /// <param name="maxKeys">Maximum key count.</param>
        /// <param name="headers">Additional headers to add to the request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List bucket result.</returns>
        public async Task<ListBucketResult> ListAsync(
            string bucket,
            string prefix = null,
            string marker = null,
            string continuationToken = null,
            int maxKeys = 1000,
            NameValueCollection headers = null,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket));
            if (maxKeys < 1) throw new ArgumentOutOfRangeException(nameof(maxKeys));

            string header = _Header + "List ";

            string url = _Client.BuildUrl(bucket, null, null);

            #region Query

            bool queryAdded = false;
            if (!String.IsNullOrEmpty(prefix))
            {
                if (!queryAdded) url += "?";
                else url += "&";
                url += "prefix=" + prefix;
                queryAdded = true;
            }

            if (!String.IsNullOrEmpty(marker))
            {
                if (!queryAdded) url += "?";
                else url += "&";
                url += "marker=" + marker;
                queryAdded = true;
            }

            if (!String.IsNullOrEmpty(continuationToken))
            {
                if (!queryAdded) url += "?";
                else url += "&";
                url += "continuation-token=" + continuationToken;
                queryAdded = true;
            }

            #endregion

            using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Get, url))
            {
                _Client.Logger?.Invoke(_Header + "GET " + url);
                req.Headers = headers;

                using (RestResponse resp = await _Client.BuildRestResponse(req, null, token).ConfigureAwait(false))
                {
                    if (resp == null)
                    {
                        _Client.Logger?.Invoke(header + "no response from " + url);
                        WebException e = _Client.WebExceptionBuilder(null, url, null, null);
                        throw e;
                    }
                    else if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                    {
                        _Client.Logger?.Invoke(header + resp.StatusCode + " response from " + url);
                        return _Client.Serializer.DeserializeXml<ListBucketResult>(resp.DataAsString);
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
        /// Create a bucket.
        /// </summary>
        /// <param name="bucket">Bucket.</param>
        /// <param name="region">Region.</param>
        /// <param name="headers">Additional headers to add to the request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task WriteAsync(
            string bucket,
            string region,
            NameValueCollection headers = null,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket));
            if (String.IsNullOrEmpty(region)) throw new ArgumentNullException(nameof(region));

            string header = _Header + "Write ";

            string url = _Client.BuildUrl(bucket, null, null);

            byte[] body = Encoding.UTF8.GetBytes(
                _Client.Serializer.SerializeXml(
                    new CreateBucketConfiguration { LocationConstraint = region }
                )
            );

            using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Put, url))
            {
                _Client.Logger?.Invoke(_Header + "PUT " + url);
                req.Headers = headers;
                req.ContentType = Constants.ContentTypeXml;

                using (RestResponse resp = await _Client.BuildRestResponse(req, body, token).ConfigureAwait(false))
                {
                    if (resp == null)
                    {
                        _Client.Logger?.Invoke(header + "no response from " + url);
                        WebException e = _Client.WebExceptionBuilder(null, url, null, null);
                        throw e;
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
        /// Delete a bucket.
        /// </summary>
        /// <param name="bucket">Bucket.</param>
        /// <param name="headers">Additional headers to add to the request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List bucket result.</returns>
        public async Task DeleteAsync(
            string bucket,
            NameValueCollection headers = null,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(bucket)) throw new ArgumentNullException(nameof(bucket));

            string header = _Header + "Delete ";

            string url = _Client.BuildUrl(bucket, null, null);

            using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Delete, url))
            {
                _Client.Logger?.Invoke(_Header + "DELETE " + url);
                req.Headers = headers;

                using (RestResponse resp = await _Client.BuildRestResponse(req, null, token).ConfigureAwait(false))
                {
                    if (resp == null)
                    {
                        _Client.Logger?.Invoke(header + "no response from " + url);
                        WebException e = _Client.WebExceptionBuilder(null, url, null, null);
                        throw e;
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