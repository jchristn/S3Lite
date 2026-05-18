namespace Test.Shared
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using RestWrapper;
    using S3Lite;
    using S3Lite.ApiObjects;
    using Touchstone.Core;

    public static partial class S3LiteTestSuites
    {
        private static TestSuiteDescriptor BuildGuardSuite()
        {
            string suiteId = "Guards";

            return new TestSuiteDescriptor(
                suiteId,
                "Client defaults and public guard clauses",
                new[]
                {
                    new TestCaseDescriptor(suiteId, "ClientDefaults", "S3Client exposes the expected defaults", ct => TestClientDefaultsAsync()),
                    new TestCaseDescriptor(suiteId, "ClientNullHttpClientConstructor", "S3Client rejects a null caller-supplied HttpClient", ct => TestClientNullHttpClientConstructorAsync()),
                    new TestCaseDescriptor(suiteId, "ClientPropertyGuards", "S3Client property guards reject invalid values", ct => TestClientPropertyGuardsAsync()),
                    new TestCaseDescriptor(suiteId, "ClientHttpClientRoundTrip", "S3Client can attach and clear a caller-supplied HttpClient", ct => TestClientHttpClientRoundTripAsync()),
                    new TestCaseDescriptor(suiteId, "ClientCredentialState", "S3Client credential state tracks configured keys", ct => TestClientCredentialStateAsync()),
                    new TestCaseDescriptor(suiteId, "ApiConstructorsRejectNull", "API helper constructors reject null S3Client instances", ct => TestApiConstructorsRejectNullAsync()),
                    new TestCaseDescriptor(suiteId, "BucketGuardClauses", "Bucket APIs reject invalid arguments before issuing requests", ct => TestBucketGuardClausesAsync()),
                    new TestCaseDescriptor(suiteId, "ObjectGuardClauses", "Object APIs reject invalid arguments before issuing requests", ct => TestObjectGuardClausesAsync())
                });
        }

        private static TestSuiteDescriptor BuildInternalSurfaceSuite()
        {
            string suiteId = "InternalSurface";

            return new TestSuiteDescriptor(
                suiteId,
                "Internal request-building and hashing behavior",
                new[]
                {
                    new TestCaseDescriptor(suiteId, "BuildUrlVirtualHosted", "BuildUrl emits the default virtual-hosted URL", ct => TestBuildUrlVirtualHostedAsync()),
                    new TestCaseDescriptor(suiteId, "BuildUrlPathStyleVersionId", "BuildUrl emits path-style URLs with versionId", ct => TestBuildUrlPathStyleVersionIdAsync()),
                    new TestCaseDescriptor(suiteId, "BuildUrlServiceRoot", "BuildUrl emits the service root when no bucket is supplied", ct => TestBuildUrlServiceRootAsync()),
                    new TestCaseDescriptor(suiteId, "BuildRestRequestHeaders", "BuildRestRequest adds standard headers", ct => TestBuildRestRequestHeadersAsync()),
                    new TestCaseDescriptor(suiteId, "Sha256NullBytes", "Sha256(null) matches the documented empty payload hash", ct => TestSha256NullBytesAsync()),
                    new TestCaseDescriptor(suiteId, "Sha256StreamMatchesByteArray", "Sha256(Stream) matches Sha256(byte[])", ct => TestSha256StreamMatchesByteArrayAsync()),
                    new TestCaseDescriptor(suiteId, "WebExceptionBuilderParsesError", "WebExceptionBuilder copies S3 XML error metadata", ct => TestWebExceptionBuilderParsesErrorAsync()),
                    new TestCaseDescriptor(suiteId, "WebExceptionBuilderWithoutStatus", "WebExceptionBuilder throws when status code is unavailable", ct => TestWebExceptionBuilderWithoutStatusAsync())
                });
        }

        private static Task TestApiConstructorsRejectNullAsync()
        {
            AssertThrows<ArgumentNullException>(() => new BucketApis(null));
            AssertThrows<ArgumentNullException>(() => new ObjectApis(null));
            AssertThrows<ArgumentNullException>(() => new ServiceApis(null));
            return Task.CompletedTask;
        }

        private static Task TestBuildRestRequestHeadersAsync()
        {
            S3Client client = new S3Client();

            using (RestRequest request = client.BuildRestRequest(HttpMethod.Put, "http://127.0.0.1:9000/test", "text/plain"))
            {
                AssertEqual(HttpMethod.Put.ToString(), request.Method.ToString(), "request method");
                AssertEqual(Constants.HeaderUserAgentValue, request.Headers[Constants.HeaderUserAgent], "user agent header");
                AssertEqual("text/plain", request.Headers[Constants.HeaderContentType], "content type header");
            }

            return Task.CompletedTask;
        }

        private static Task TestBuildUrlPathStyleVersionIdAsync()
        {
            S3Client client = new S3Client();
            client.RequestStyle = RequestStyleEnum.PathStyle;
            client.Protocol = ProtocolEnum.Http;
            client.Hostname = "127.0.0.1";
            client.Port = 9000;
            client.Region = String.Empty;

            string url = client.BuildUrl("demo-bucket", "folder/file.txt", "v1");

            AssertEqual("http://127.0.0.1:9000/demo-bucket/folder/file.txt?versionId=v1", url, "path-style URL");
            return Task.CompletedTask;
        }

        private static Task TestBuildUrlServiceRootAsync()
        {
            S3Client client = new S3Client();
            string url = client.BuildUrl();

            AssertEqual("https://s3.us-west-1.amazonaws.com:443/", url, "service root URL");
            return Task.CompletedTask;
        }

        private static Task TestBuildUrlVirtualHostedAsync()
        {
            S3Client client = new S3Client();
            string url = client.BuildUrl("demo-bucket", "hello.txt");

            AssertEqual("https://demo-bucket.s3.us-west-1.amazonaws.com:443/hello.txt", url, "virtual-hosted URL");
            return Task.CompletedTask;
        }

        private static async Task TestBucketGuardClausesAsync()
        {
            S3Client client = new S3Client();

            await AssertThrowsAsync<ArgumentNullException>(() => client.Bucket.ExistsAsync(null)).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Bucket.ExistsAsync(String.Empty)).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Bucket.ListAsync(null)).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentOutOfRangeException>(() => client.Bucket.ListAsync("bucket", maxKeys: 0)).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentOutOfRangeException>(() => client.Bucket.ListAsync("bucket", maxKeys: 1001)).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Bucket.WriteAsync(null, "us-west-1")).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Bucket.WriteAsync("bucket", null)).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Bucket.DeleteAsync(null)).ConfigureAwait(false);
        }

        private static Task TestClientCredentialStateAsync()
        {
            S3Client client = new S3Client();

            AssertFalse(client.HasCredentials, "S3Client should start without credentials.");

            client.WithAccessKey("access-key");
            AssertFalse(client.HasCredentials, "Supplying only an access key should not enable credentials.");

            client.WithSecretKey("secret-key");
            AssertTrue(client.HasCredentials, "Supplying both access and secret keys should enable credentials.");

            client.WithAccessKey(null);
            AssertFalse(client.HasCredentials, "Clearing the access key should disable credentials.");

            client.WithAccessKey("access-key");
            client.WithSecretKey(null);
            AssertFalse(client.HasCredentials, "Clearing the secret key should disable credentials.");

            return Task.CompletedTask;
        }

        private static Task TestClientDefaultsAsync()
        {
            S3Client client = new S3Client();

            AssertEqual(65536, client.StreamBufferSize, "default stream buffer size");
            AssertEqual("amazonaws.com", client.Hostname, "default hostname");
            AssertEqual(443, client.Port, "default port");
            AssertEqual("us-west-1", client.Region, "default region");
            AssertEqual(ProtocolEnum.Https.ToString(), client.Protocol.ToString(), "default protocol");
            AssertEqual(RequestStyleEnum.VirtualHostedStyle.ToString(), client.RequestStyle.ToString(), "default request style");
            AssertEqual(SignatureVersionEnum.Version4.ToString(), client.SignatureVersion.ToString(), "default signature version");
            AssertFalse(client.HasCredentials, "S3Client should default to anonymous mode.");
            AssertNull(client.HttpClient, "default HttpClient");
            AssertNotNull(client.Service, "Service APIs");
            AssertNotNull(client.Bucket, "Bucket APIs");
            AssertNotNull(client.Object, "Object APIs");
            AssertNotNull(client.Serializer, "Serializer");
            AssertNotNull(client.Debug, "Debug settings");

            return Task.CompletedTask;
        }

        private static Task TestClientHttpClientRoundTripAsync()
        {
            using HttpClient httpClient = new HttpClient();
            S3Client client = new S3Client(httpClient);

            AssertSame(httpClient, client.HttpClient, "constructor HttpClient");

            client.WithHttpClient(null);
            AssertNull(client.HttpClient, "cleared HttpClient");

            client.WithHttpClient(httpClient);
            AssertSame(httpClient, client.HttpClient, "reassigned HttpClient");

            return Task.CompletedTask;
        }

        private static Task TestClientNullHttpClientConstructorAsync()
        {
            AssertThrows<ArgumentNullException>(() => new S3Client(null));
            return Task.CompletedTask;
        }

        private static Task TestClientPropertyGuardsAsync()
        {
            S3Client client = new S3Client();

            AssertThrows<ArgumentOutOfRangeException>(() => client.StreamBufferSize = 0);
            AssertThrows<ArgumentNullException>(() => client.Hostname = null);
            AssertThrows<ArgumentOutOfRangeException>(() => client.Port = -1);
            AssertThrows<ArgumentOutOfRangeException>(() => client.Port = 65536);
            AssertThrows<ArgumentNullException>(() => client.Serializer = null);
            AssertThrows<ArgumentNullException>(() => client.WithHostname(null));

            return Task.CompletedTask;
        }

        private static async Task TestObjectGuardClausesAsync()
        {
            S3Client client = new S3Client();

            await AssertThrowsAsync<ArgumentNullException>(() => client.Object.ExistsAsync(null, "key")).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Object.ExistsAsync("bucket", null)).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Object.GetMetadataAsync(null, "key")).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Object.GetMetadataAsync("bucket", null)).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Object.GetAsync(null, "key")).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Object.GetAsync("bucket", null)).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Object.WriteAsync(null, "key", Array.Empty<byte>())).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Object.WriteAsync("bucket", null, Array.Empty<byte>())).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Object.DeleteAsync(null, "key")).ConfigureAwait(false);
            await AssertThrowsAsync<ArgumentNullException>(() => client.Object.DeleteAsync("bucket", null)).ConfigureAwait(false);
        }

        private static Task TestSha256NullBytesAsync()
        {
            S3Client client = new S3Client();
            byte[] hash = client.Sha256((byte[]?)null!);

            AssertByteArraysEqual(BytesFromHexString(Constants.EmptySha256Hash), hash, "empty SHA256");
            return Task.CompletedTask;
        }

        private static Task TestSha256StreamMatchesByteArrayAsync()
        {
            S3Client client = new S3Client();
            byte[] data = System.Text.Encoding.UTF8.GetBytes("hash me");
            byte[] byteHash = client.Sha256(data);

            using (MemoryStream stream = new MemoryStream(data))
            {
                byte[] streamHash = client.Sha256(stream);
                AssertByteArraysEqual(byteHash, streamHash, "stream SHA256");
            }

            return Task.CompletedTask;
        }

        private static Task TestWebExceptionBuilderParsesErrorAsync()
        {
            S3Client client = new S3Client();
            Error error = new Error(ErrorCode.NoSuchKey, "missing.txt", "v1", "req-123", "/demo-bucket/missing.txt");
            string xml = client.Serializer.SerializeXml(error);
            WebException exception = client.WebExceptionBuilder(404, "http://localhost/demo-bucket/missing.txt", null, xml);

            AssertEqual("The specified key does not exist.", exception.Message, "web exception message");
            AssertWebException(exception, 404, ErrorCode.NoSuchKey.ToString(), "missing.txt");
            AssertEqual("v1", exception.Data["VersionId"]?.ToString(), "web exception version id");
            AssertEqual("req-123", exception.Data["RequestId"]?.ToString(), "web exception request id");
            AssertEqual("/demo-bucket/missing.txt", exception.Data["Resource"]?.ToString(), "web exception resource");

            return Task.CompletedTask;
        }

        private static Task TestWebExceptionBuilderWithoutStatusAsync()
        {
            S3Client client = new S3Client();

            AssertThrows<WebException>(
                () => client.WebExceptionBuilder(null, "http://localhost/demo-bucket/missing.txt", null, null),
                exception =>
                {
                    AssertEqual(
                        "Unable to connect to the specified URL: http://localhost/demo-bucket/missing.txt.",
                        exception.Message,
                        "statusless web exception message");
                });

            return Task.CompletedTask;
        }
    }
}
