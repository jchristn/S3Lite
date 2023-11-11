using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using RestWrapper;
using S3Lite.ApiObjects;

namespace S3Lite
{
    /// <summary>
    /// Amazon S3 service APIs.
    /// </summary>
    public class ServiceApis
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[S3Service] ";
        private S3Client _Client = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ServiceApis(S3Client client)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// List buckets.
        /// </summary>
        /// <param name="headers">Additional headers to add to the request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List my buckets result.</returns>
        public async Task<ListAllMyBucketsResult> ListBucketsAsync(
            NameValueCollection headers = null,
            CancellationToken token = default)
        {
            string header = _Header + "ListBucketsAsync ";

            (bool, string) ret = new(false, null);
            string url = _Client.BuildUrl();

            using (RestRequest req = _Client.BuildRestRequest(HttpMethod.Get, url))
            {
                _Client.Logger?.Invoke(_Header + "GET " + url);
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
                        return _Client.Serializer.DeserializeXml<ListAllMyBucketsResult>(resp.DataAsString);
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