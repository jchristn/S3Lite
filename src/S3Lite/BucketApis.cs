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

            using (OperationScope op = OperationScope.Start(_Client, S3LiteTelemetryNames.OperationHeadBucket, "HEAD", bucket, null))
            {
                try
                {
                    using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Head, url))
                    {
                        _Client.Logger?.Invoke(_Header + "GET " + url);
                        req.Headers = headers;

                        using (RestResponse resp = await _Client.BuildRestResponse(req, null, token, op).ConfigureAwait(false))
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
                                op?.Succeed();
                                return true;
                            }
                            else if (resp.StatusCode == 404)
                            {
                                _Client.Logger?.Invoke(header + resp.StatusCode + " response from " + url);
                                op?.NotFound();
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
                catch (Exception e)
                {
                    op?.Fail(e);
                    throw;
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
        /// <param name="maxKeys">Maximum key count.  Must be greater than zero and less than or equal to 1000.</param>
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
            if (maxKeys < 1 || maxKeys > 1000) throw new ArgumentOutOfRangeException(nameof(maxKeys));

            string header = _Header + "List ";

            string url = _Client.BuildUrl(bucket, null, null) + "?list-type=2&max-keys=" + maxKeys;

            if (!String.IsNullOrEmpty(prefix)) url += "&prefix=" + prefix;
            if (!String.IsNullOrEmpty(marker)) url += "&marker=" + marker;
            if (!String.IsNullOrEmpty(continuationToken)) url += "&continuation-token=" + Helpers.UriEncode(continuationToken);

            using (OperationScope op = OperationScope.Start(_Client, S3LiteTelemetryNames.OperationListObjects, "GET", bucket, null))
            {
                try
                {
                    using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Get, url))
                    {
                        _Client.Logger?.Invoke(_Header + "GET " + url);
                        req.Headers = headers;

                        using (RestResponse resp = await _Client.BuildRestResponse(req, null, token, op).ConfigureAwait(false))
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
                                ListBucketResult result;
                                using (StageScope stage = op?.StartStage(S3LiteTelemetryNames.StageResponse))
                                {
                                    try
                                    {
                                        result = _Client.Serializer.DeserializeXml<ListBucketResult>(resp.DataAsString);
                                    }
                                    catch (Exception e)
                                    {
                                        stage?.Fail(e);
                                        throw;
                                    }
                                }
                                S3LiteTelemetry.RecordListed(op, result?.Contents != null ? result.Contents.Count : 0, result != null && result.IsTruncated);
                                op?.Succeed();
                                return result;
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
                catch (Exception e)
                {
                    op?.Fail(e);
                    throw;
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

            using (OperationScope op = OperationScope.Start(_Client, S3LiteTelemetryNames.OperationCreateBucket, "PUT", bucket, null))
            {
                try
                {
                    using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Put, url))
                    {
                        _Client.Logger?.Invoke(_Header + "PUT " + url);
                        req.Headers = headers;
                        req.ContentType = Constants.ContentTypeXml;

                        using (RestResponse resp = await _Client.BuildRestResponse(req, body, token, op).ConfigureAwait(false))
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
                                op?.Succeed();
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
                catch (Exception e)
                {
                    op?.Fail(e);
                    throw;
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

            using (OperationScope op = OperationScope.Start(_Client, S3LiteTelemetryNames.OperationDeleteBucket, "DELETE", bucket, null))
            {
                try
                {
                    using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Delete, url))
                    {
                        _Client.Logger?.Invoke(_Header + "DELETE " + url);
                        req.Headers = headers;

                        using (RestResponse resp = await _Client.BuildRestResponse(req, null, token, op).ConfigureAwait(false))
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
                                op?.Succeed();
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
                catch (Exception e)
                {
                    op?.Fail(e);
                    throw;
                }
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}