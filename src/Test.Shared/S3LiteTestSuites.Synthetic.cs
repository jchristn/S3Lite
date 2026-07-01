namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AWSSignatureGenerator;
    using S3Lite;
    using S3Lite.ApiObjects;
    using Touchstone.Core;

    public static partial class S3LiteTestSuites
    {
        private const string _SyntheticBucketName = "synthetic-bucket";
        private const string _SyntheticFolderKeyOne = "folder/one.txt";
        private const string _SyntheticFolderKeyTwo = "folder/two.txt";
        private const string _SyntheticHelloContent = "Hello from the synthetic S3 server.";
        private const string _SyntheticHelloKey = "hello.txt";
        private const string _SyntheticMissingBucketName = "missing-bucket";
        private const string _SyntheticMissingObjectKey = "missing.txt";
        private const string _SyntheticOtherKey = "other/three.txt";

        private static TestSuiteDescriptor BuildSyntheticAnonymousSuite(RequestTransportMode mode)
        {
            string suiteId = "Synthetic.Anonymous." + ModeId(mode);
            string modeLabel = ModeLabel(mode);

            return new TestSuiteDescriptor(
                suiteId,
                "Synthetic anonymous and read-path behavior (" + modeLabel + ")",
                new[]
                {
                    new TestCaseDescriptor(suiteId, "ServiceListBucketsDenied", "Service.ListBucketsAsync denies anonymous access using " + modeLabel, ct => TestSyntheticServiceListBucketsDeniedAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "BucketExistsTrue", "Bucket.ExistsAsync returns true for an existing bucket using " + modeLabel, ct => TestSyntheticBucketExistsTrueAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "BucketExistsFalse", "Bucket.ExistsAsync returns false for a missing bucket using " + modeLabel, ct => TestSyntheticBucketExistsFalseAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "BucketListAll", "Bucket.ListAsync returns all seeded objects using " + modeLabel, ct => TestSyntheticBucketListAllAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "BucketListPrefix", "Bucket.ListAsync filters by prefix using " + modeLabel, ct => TestSyntheticBucketListPrefixAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "BucketListContinuation", "Bucket.ListAsync paginates using continuation tokens with " + modeLabel, ct => TestSyntheticBucketListContinuationAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "BucketListMissingBucket", "Bucket.ListAsync reports NoSuchBucket using " + modeLabel, ct => TestSyntheticBucketListMissingBucketAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectExistsTrue", "Object.ExistsAsync returns true for an existing object using " + modeLabel, ct => TestSyntheticObjectExistsTrueAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectExistsFalse", "Object.ExistsAsync returns false for a missing object using " + modeLabel, ct => TestSyntheticObjectExistsFalseAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectGetBytes", "Object.GetAsync returns the seeded payload using " + modeLabel, ct => TestSyntheticObjectGetBytesAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectGetMetadata", "Object.GetMetadataAsync returns headers and metadata using " + modeLabel, ct => TestSyntheticObjectGetMetadataAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectGetMissingObject", "Object.GetAsync reports NoSuchKey using " + modeLabel, ct => TestSyntheticObjectGetMissingObjectAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectGetMissingBucket", "Object.GetAsync reports NoSuchBucket using " + modeLabel, ct => TestSyntheticObjectGetMissingBucketAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectGetMetadataMissingObject", "Object.GetMetadataAsync reports 404 for a missing key using " + modeLabel, ct => TestSyntheticObjectGetMetadataMissingObjectAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectWriteDenied", "Object.WriteAsync rejects anonymous writes using " + modeLabel, ct => TestSyntheticObjectWriteDeniedAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectDeleteDenied", "Object.DeleteAsync rejects anonymous deletes using " + modeLabel, ct => TestSyntheticObjectDeleteDeniedAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "BucketWriteDenied", "Bucket.WriteAsync rejects anonymous bucket creation using " + modeLabel, ct => TestSyntheticBucketWriteDeniedAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "BucketDeleteDenied", "Bucket.DeleteAsync rejects anonymous bucket deletion using " + modeLabel, ct => TestSyntheticBucketDeleteDeniedAsync(mode, ct))
                });
        }

        private static TestSuiteDescriptor BuildSyntheticAuthenticatedSuite(RequestTransportMode mode)
        {
            string suiteId = "Synthetic.Authenticated." + ModeId(mode);
            string modeLabel = ModeLabel(mode);
            List<TestCaseDescriptor> cases = new List<TestCaseDescriptor>
            {
                new TestCaseDescriptor(suiteId, "ServiceListBuckets", "Service.ListBucketsAsync returns buckets using " + modeLabel, ct => TestSyntheticServiceListBucketsAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "BucketCreateStoresRegion", "Bucket.WriteAsync creates a bucket and stores its region using " + modeLabel, ct => TestSyntheticBucketCreateStoresRegionAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "BucketCreateExisting", "Bucket.WriteAsync reports conflict for an existing bucket using " + modeLabel, ct => TestSyntheticBucketCreateExistingAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "BucketDeleteEmpty", "Bucket.DeleteAsync deletes an empty bucket using " + modeLabel, ct => TestSyntheticBucketDeleteEmptyAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "BucketDeleteNonEmpty", "Bucket.DeleteAsync reports BucketNotEmpty using " + modeLabel, ct => TestSyntheticBucketDeleteNonEmptyAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "BucketDeleteMissing", "Bucket.DeleteAsync reports NoSuchBucket using " + modeLabel, ct => TestSyntheticBucketDeleteMissingAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectWriteReadDeleteRoundTrip", "Object write, read, and delete round-trip succeeds using " + modeLabel, ct => TestSyntheticObjectWriteReadDeleteRoundTripAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectOverwrite", "Object.WriteAsync overwrites existing content using " + modeLabel, ct => TestSyntheticObjectOverwriteAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectWriteNullData", "Object.WriteAsync accepts null data and stores an empty object using " + modeLabel, ct => TestSyntheticObjectWriteNullDataAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectDeleteMissing", "Object.DeleteAsync succeeds when the object is already missing using " + modeLabel, ct => TestSyntheticObjectDeleteMissingAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectSpecialCharacterKey", "Object APIs round-trip keys with spaces using " + modeLabel, ct => TestSyntheticObjectSpecialCharacterKeyAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectVersionIdParameter", "Object APIs accept versionId query parameters using " + modeLabel, ct => TestSyntheticObjectVersionIdParameterAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectWriteMissingBucket", "Object.WriteAsync reports NoSuchBucket using " + modeLabel, ct => TestSyntheticObjectWriteMissingBucketAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "GatewayRoutingPathStyle", "Gateway routing sends to the gateway host while the SigV4 signature stays bound to the path-style upstream authority (non-standard port) using " + modeLabel, ct => TestSyntheticGatewayRoutingPathStyleAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "GatewayRoutingVirtualHosted", "Gateway routing keeps the SigV4 signature bound to the bucket-prefixed virtual-hosted upstream authority for a nested key using " + modeLabel, ct => TestSyntheticGatewayRoutingVirtualHostedAsync(mode, ct)),
            };

            if (mode == RequestTransportMode.External)
            {
                cases.Add(new TestCaseDescriptor(
                    suiteId,
                    "CallerOwnedHttpClientUsableAfterRequest",
                    "The caller-supplied HttpClient remains usable after S3Lite requests",
                    ct => TestSyntheticCallerOwnedHttpClientUsableAfterRequestAsync(ct)));
            }

            return new TestSuiteDescriptor(suiteId, "Synthetic authenticated write-path behavior (" + modeLabel + ")", cases);
        }

        private static S3Client CreateSyntheticClient(S3LiteSyntheticServer server, HttpClient? httpClient, bool authenticated)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            S3Client client = httpClient == null ? new S3Client() : new S3Client(httpClient);

            client
                .WithHostname(server.Hostname)
                .WithPort(server.Port)
                .WithProtocol(ProtocolEnum.Http)
                .WithRegion(server.Region)
                .WithRequestStyle(RequestStyleEnum.PathStyle);

            if (authenticated)
            {
                client
                    .WithAccessKey("synthetic-access-key")
                    .WithSecretKey("synthetic-secret-key");
            }

            return client;
        }

        private static async Task ExecuteWithSyntheticClientAsync(
            RequestTransportMode mode,
            bool authenticated,
            Func<S3LiteSyntheticServer, HttpClient?, S3Client, CancellationToken, Task> executeAsync,
            CancellationToken cancellationToken)
        {
            await using S3LiteSyntheticServer server = await S3LiteSyntheticServer.StartAsync(cancellationToken).ConfigureAwait(false);
            SeedSyntheticServer(server);

            HttpClient? httpClient = null;

            try
            {
                if (mode == RequestTransportMode.External)
                {
                    httpClient = server.CreateHttpClient();
                }

                S3Client client = CreateSyntheticClient(server, httpClient, authenticated);

                if (mode == RequestTransportMode.Internal)
                {
                    AssertNull(client.HttpClient, "internal-mode client HttpClient");
                }
                else
                {
                    AssertSame(httpClient!, client.HttpClient, "external-mode client HttpClient");
                }

                await executeAsync(server, httpClient, client, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                httpClient?.Dispose();
            }
        }

        private static string GenerateBucketName()
        {
            return "bucket-" + Guid.NewGuid().ToString("N").Substring(0, 20);
        }

        private static string GenerateObjectKey()
        {
            return "object-" + Guid.NewGuid().ToString("N") + ".txt";
        }

        private static byte[] GetBytes(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return Encoding.UTF8.GetBytes(value);
        }

        private static void SeedSyntheticServer(S3LiteSyntheticServer server)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            server.SeedBucket(_SyntheticBucketName, server.Region);
            server.SeedObject(_SyntheticBucketName, _SyntheticHelloKey, GetBytes(_SyntheticHelloContent), "text/plain");
            server.SeedObject(_SyntheticBucketName, _SyntheticFolderKeyOne, GetBytes("one"), "text/plain");
            server.SeedObject(_SyntheticBucketName, _SyntheticFolderKeyTwo, GetBytes("two"), "text/plain");
            server.SeedObject(_SyntheticBucketName, _SyntheticOtherKey, GetBytes("three"), "text/plain");
        }

        private static async Task TestSyntheticBucketCreateExistingAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                await AssertThrowsAsync<WebException>(
                    () => client.Bucket.WriteAsync(_SyntheticBucketName, server.Region, token: ct),
                    exception => AssertWebException(exception, 409, ErrorCode.BucketAlreadyOwnedByYou.ToString())).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticBucketCreateStoresRegionAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                string bucketName = GenerateBucketName();
                string region = "eu-central-9";

                client.WithRegion(region);
                await client.Bucket.WriteAsync(bucketName, region, token: ct).ConfigureAwait(false);

                AssertTrue(server.BucketExists(bucketName), "Bucket should exist after creation.");
                AssertEqual(region, server.GetBucketRegion(bucketName), "stored bucket region");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticBucketDeleteDeniedAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                await AssertThrowsAsync<WebException>(
                    () => client.Bucket.DeleteAsync(_SyntheticBucketName, token: ct),
                    exception => AssertWebException(exception, 403, ErrorCode.AccessDenied.ToString())).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticBucketDeleteEmptyAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                string bucketName = GenerateBucketName();
                await client.Bucket.WriteAsync(bucketName, server.Region, token: ct).ConfigureAwait(false);
                await client.Bucket.DeleteAsync(bucketName, token: ct).ConfigureAwait(false);

                AssertFalse(server.BucketExists(bucketName), "Bucket should not exist after deletion.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticBucketDeleteMissingAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                string bucketName = GenerateBucketName();

                await AssertThrowsAsync<WebException>(
                    () => client.Bucket.DeleteAsync(bucketName, token: ct),
                    exception => AssertWebException(exception, 404, ErrorCode.NoSuchBucket.ToString())).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticBucketDeleteNonEmptyAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                string bucketName = GenerateBucketName();
                await client.Bucket.WriteAsync(bucketName, server.Region, token: ct).ConfigureAwait(false);
                await client.Object.WriteAsync(bucketName, "contents.txt", GetBytes("content"), "text/plain", token: ct).ConfigureAwait(false);

                await AssertThrowsAsync<WebException>(
                    () => client.Bucket.DeleteAsync(bucketName, token: ct),
                    exception => AssertWebException(exception, 409, ErrorCode.BucketNotEmpty.ToString())).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticBucketExistsFalseAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                bool exists = await client.Bucket.ExistsAsync(_SyntheticMissingBucketName, token: ct).ConfigureAwait(false);
                AssertFalse(exists, "Missing bucket should not exist.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticBucketExistsTrueAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                bool exists = await client.Bucket.ExistsAsync(_SyntheticBucketName, token: ct).ConfigureAwait(false);
                AssertTrue(exists, "Seeded bucket should exist.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticBucketListAllAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                ListBucketResult result = await client.Bucket.ListAsync(_SyntheticBucketName, token: ct).ConfigureAwait(false);

                AssertEqual(4, result.Contents.Count, "list-all object count");
                AssertTrue(result.Contents.Any(metadata => String.Equals(metadata.Key, _SyntheticHelloKey, StringComparison.Ordinal)), "List should contain hello.txt.");
                AssertTrue(result.Contents.Any(metadata => String.Equals(metadata.Key, _SyntheticFolderKeyOne, StringComparison.Ordinal)), "List should contain folder/one.txt.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticBucketListContinuationAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                ListBucketResult firstPage = await client.Bucket.ListAsync(_SyntheticBucketName, maxKeys: 2, token: ct).ConfigureAwait(false);

                AssertEqual(2, firstPage.Contents.Count, "first page count");
                AssertTrue(firstPage.IsTruncated, "First page should be truncated.");
                AssertTrue(!String.IsNullOrWhiteSpace(firstPage.NextContinuationToken), "First page should include a continuation token.");

                ListBucketResult secondPage = await client.Bucket.ListAsync(
                    _SyntheticBucketName,
                    continuationToken: firstPage.NextContinuationToken,
                    maxKeys: 2,
                    token: ct).ConfigureAwait(false);

                AssertEqual(2, secondPage.Contents.Count, "second page count");
                AssertFalse(secondPage.IsTruncated, "Second page should not be truncated.");
                AssertTrue(String.IsNullOrWhiteSpace(secondPage.NextContinuationToken), "Second page should not include a continuation token.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticBucketListMissingBucketAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                await AssertThrowsAsync<WebException>(
                    () => client.Bucket.ListAsync(_SyntheticMissingBucketName, token: ct),
                    exception => AssertWebException(exception, 404, ErrorCode.NoSuchBucket.ToString())).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticBucketListPrefixAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                ListBucketResult result = await client.Bucket.ListAsync(_SyntheticBucketName, prefix: "folder/", token: ct).ConfigureAwait(false);

                AssertEqual(2, result.Contents.Count, "prefix-filtered object count");
                AssertTrue(result.Contents.All(metadata => metadata.Key.StartsWith("folder/", StringComparison.Ordinal)), "All listed keys should match the supplied prefix.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticBucketWriteDeniedAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                await AssertThrowsAsync<WebException>(
                    () => client.Bucket.WriteAsync(GenerateBucketName(), server.Region, token: ct),
                    exception => AssertWebException(exception, 403, ErrorCode.AccessDenied.ToString())).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticCallerOwnedHttpClientUsableAfterRequestAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.External, true, async (server, httpClient, client, ct) =>
            {
                bool exists = await client.Bucket.ExistsAsync(_SyntheticBucketName, token: ct).ConfigureAwait(false);
                AssertTrue(exists, "Seeded bucket should exist.");

                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, server.BaseUrl + "/" + _SyntheticBucketName);
                using HttpResponseMessage response = await httpClient!.SendAsync(request, ct).ConfigureAwait(false);

                AssertEqual(200, (int)response.StatusCode, "direct HttpClient HEAD status");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectDeleteDeniedAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                await AssertThrowsAsync<WebException>(
                    () => client.Object.DeleteAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct),
                    exception => AssertWebException(exception, 403, ErrorCode.AccessDenied.ToString())).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectDeleteMissingAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                await client.Object.DeleteAsync(_SyntheticBucketName, _SyntheticMissingObjectKey, token: ct).ConfigureAwait(false);
                AssertFalse(server.ObjectExists(_SyntheticBucketName, _SyntheticMissingObjectKey), "Missing object should still be absent after delete.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectExistsFalseAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                bool exists = await client.Object.ExistsAsync(_SyntheticBucketName, _SyntheticMissingObjectKey, token: ct).ConfigureAwait(false);
                AssertFalse(exists, "Missing object should not exist.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectExistsTrueAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                bool exists = await client.Object.ExistsAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);
                AssertTrue(exists, "Seeded object should exist.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectGetBytesAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                byte[] data = await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);

                AssertByteArraysEqual(GetBytes(_SyntheticHelloContent), data, "synthetic object bytes");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectGetMetadataAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                ObjectMetadata metadata = await client.Object.GetMetadataAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);

                AssertEqual(_SyntheticHelloKey, metadata.Key, "metadata key");
                AssertEqual(GetBytes(_SyntheticHelloContent).LongLength, metadata.Size, "metadata size");
                AssertEqual("text/plain", metadata.ContentType, "metadata content type");
                AssertTrue(!String.IsNullOrWhiteSpace(metadata.ETag), "Metadata ETag should not be empty.");
                AssertTrue(metadata.ETag.StartsWith("\"", StringComparison.Ordinal), "Metadata ETag should be quoted.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectGetMetadataMissingObjectAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                await AssertThrowsAsync<WebException>(
                    () => client.Object.GetMetadataAsync(_SyntheticBucketName, _SyntheticMissingObjectKey, token: ct),
                    exception => AssertWebException(exception, 404, String.Empty)).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectGetMissingBucketAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                await AssertThrowsAsync<WebException>(
                    () => client.Object.GetAsync(_SyntheticMissingBucketName, _SyntheticHelloKey, token: ct),
                    exception => AssertWebException(exception, 404, ErrorCode.NoSuchBucket.ToString(), _SyntheticHelloKey)).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectGetMissingObjectAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                await AssertThrowsAsync<WebException>(
                    () => client.Object.GetAsync(_SyntheticBucketName, _SyntheticMissingObjectKey, token: ct),
                    exception => AssertWebException(exception, 404, ErrorCode.NoSuchKey.ToString(), _SyntheticMissingObjectKey)).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectOverwriteAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                string key = GenerateObjectKey();
                byte[] originalData = GetBytes("original");
                byte[] updatedData = GetBytes("updated");

                await client.Object.WriteAsync(_SyntheticBucketName, key, originalData, "text/plain", token: ct).ConfigureAwait(false);
                await client.Object.WriteAsync(_SyntheticBucketName, key, updatedData, "application/json", token: ct).ConfigureAwait(false);

                byte[] downloaded = await client.Object.GetAsync(_SyntheticBucketName, key, token: ct).ConfigureAwait(false);
                ObjectMetadata metadata = await client.Object.GetMetadataAsync(_SyntheticBucketName, key, token: ct).ConfigureAwait(false);

                AssertByteArraysEqual(updatedData, downloaded, "overwritten object data");
                AssertEqual("application/json", metadata.ContentType, "overwritten content type");
                AssertEqual(updatedData.LongLength, metadata.Size, "overwritten object size");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectSpecialCharacterKeyAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                string key = "folder/file with spaces.txt";
                byte[] payload = GetBytes("special payload");

                await client.Object.WriteAsync(_SyntheticBucketName, key, payload, "text/plain", token: ct).ConfigureAwait(false);
                byte[] downloaded = await client.Object.GetAsync(_SyntheticBucketName, key, token: ct).ConfigureAwait(false);

                AssertByteArraysEqual(payload, downloaded, "special-character key payload");
                AssertTrue(server.ObjectExists(_SyntheticBucketName, key), "Synthetic server should store keys with spaces.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectVersionIdParameterAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                string key = GenerateObjectKey();
                byte[] payload = GetBytes("versioned payload");

                await client.Object.WriteAsync(_SyntheticBucketName, key, payload, "text/plain", token: ct).ConfigureAwait(false);

                bool exists = await client.Object.ExistsAsync(_SyntheticBucketName, key, versionId: "version-1", token: ct).ConfigureAwait(false);
                ObjectMetadata metadata = await client.Object.GetMetadataAsync(_SyntheticBucketName, key, versionId: "version-1", token: ct).ConfigureAwait(false);
                byte[] downloaded = await client.Object.GetAsync(_SyntheticBucketName, key, versionId: "version-1", token: ct).ConfigureAwait(false);

                await client.Object.DeleteAsync(_SyntheticBucketName, key, versionId: "version-1", token: ct).ConfigureAwait(false);

                AssertTrue(exists, "ExistsAsync should succeed when a versionId is supplied.");
                AssertEqual(key, metadata.Key, "versioned metadata key");
                AssertByteArraysEqual(payload, downloaded, "versioned download payload");
                AssertFalse(server.ObjectExists(_SyntheticBucketName, key), "Versioned delete should remove the object.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectWriteDeniedAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                await AssertThrowsAsync<WebException>(
                    () => client.Object.WriteAsync(_SyntheticBucketName, GenerateObjectKey(), GetBytes("denied"), "text/plain", token: ct),
                    exception => AssertWebException(exception, 403, ErrorCode.AccessDenied.ToString())).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectWriteMissingBucketAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                await AssertThrowsAsync<WebException>(
                    () => client.Object.WriteAsync(_SyntheticMissingBucketName, GenerateObjectKey(), GetBytes("payload"), "text/plain", token: ct),
                    exception => AssertWebException(exception, 404, ErrorCode.NoSuchBucket.ToString())).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectWriteNullDataAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                string key = GenerateObjectKey();

                await client.Object.WriteAsync(_SyntheticBucketName, key, null, "application/octet-stream", token: ct).ConfigureAwait(false);
                byte[] downloaded = await client.Object.GetAsync(_SyntheticBucketName, key, token: ct).ConfigureAwait(false);

                AssertEqual(0, downloaded.Length, "null-data object length");
                AssertEqual(0, server.GetObjectData(_SyntheticBucketName, key).Length, "stored null-data object length");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectWriteReadDeleteRoundTripAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                string key = GenerateObjectKey();
                byte[] payload = GetBytes("{\"hello\":\"world\"}");

                await client.Object.WriteAsync(_SyntheticBucketName, key, payload, "application/json", token: ct).ConfigureAwait(false);
                AssertEqual("application/json", server.GetObjectContentType(_SyntheticBucketName, key), "stored content type");

                bool exists = await client.Object.ExistsAsync(_SyntheticBucketName, key, token: ct).ConfigureAwait(false);
                byte[] downloaded = await client.Object.GetAsync(_SyntheticBucketName, key, token: ct).ConfigureAwait(false);

                await client.Object.DeleteAsync(_SyntheticBucketName, key, token: ct).ConfigureAwait(false);

                AssertTrue(exists, "Written object should exist.");
                AssertByteArraysEqual(payload, downloaded, "round-trip payload");
                AssertFalse(server.ObjectExists(_SyntheticBucketName, key), "Deleted object should be removed from the synthetic server.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private const string _GatewayAccessKey = "synthetic-access-key";
        private const string _GatewaySecretKey = "synthetic-secret-key";
        private const string _GatewayUpstreamHostname = "s3.upstream.example";

        private static async Task TestSyntheticGatewayRoutingPathStyleAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await using S3LiteSyntheticServer server = await S3LiteSyntheticServer.StartAsync(cancellationToken).ConfigureAwait(false);
            SeedSyntheticServer(server);

            // A non-standard upstream port so the signed host authority is "s3.upstream.example:8443",
            // not just the bare hostname; this exercises the port-bearing path-style authority.
            const int upstreamPort = 8443;
            HttpClient? httpClient = null;

            try
            {
                if (mode == RequestTransportMode.External)
                {
                    httpClient = server.CreateHttpClient();
                }

                S3Client client = httpClient == null ? new S3Client() : new S3Client(httpClient);

                client
                    .WithHostname(_GatewayUpstreamHostname)
                    .WithPort(upstreamPort)
                    .WithProtocol(ProtocolEnum.Https)
                    .WithRegion(server.Region)
                    .WithRequestStyle(RequestStyleEnum.PathStyle)
                    .WithAccessKey(_GatewayAccessKey)
                    .WithSecretKey(_GatewaySecretKey)
                    .WithGateway(new GatewayConfig
                    {
                        Hostname = server.Hostname,
                        Port = server.Port,
                        Protocol = ProtocolEnum.Http
                    });

                byte[] data = await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: cancellationToken).ConfigureAwait(false);

                AssertByteArraysEqual(GetBytes(_SyntheticHelloContent), data, "A request routed through the gateway host should reach the synthetic server and return the seeded object.");
                AssertEqual(
                    server.Hostname + ":" + server.Port.ToString(),
                    server.LastHostHeader,
                    "The gateway should receive Host set to the gateway host; it is expected to rewrite Host to the upstream authority before forwarding.");
                AssertEqual(
                    "/" + _SyntheticBucketName + "/" + _SyntheticHelloKey,
                    server.LastRawPathAndQuery,
                    "Gateway routing must not alter the request path or query.");

                AssertGatewaySignatureBoundToUpstream(
                    server,
                    server.Region,
                    ProtocolEnum.Https,
                    _GatewayUpstreamHostname,
                    upstreamPort);
            }
            finally
            {
                httpClient?.Dispose();
            }
        }

        private static async Task TestSyntheticGatewayRoutingVirtualHostedAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await using S3LiteSyntheticServer server = await S3LiteSyntheticServer.StartAsync(cancellationToken).ConfigureAwait(false);
            SeedSyntheticServer(server);

            HttpClient? httpClient = null;

            try
            {
                if (mode == RequestTransportMode.External)
                {
                    httpClient = server.CreateHttpClient();
                }

                S3Client client = httpClient == null ? new S3Client() : new S3Client(httpClient);

                client
                    .WithHostname(_GatewayUpstreamHostname)
                    .WithPort(443)
                    .WithProtocol(ProtocolEnum.Https)
                    .WithRegion(server.Region)
                    .WithRequestStyle(RequestStyleEnum.VirtualHostedStyle)
                    .WithAccessKey(_GatewayAccessKey)
                    .WithSecretKey(_GatewaySecretKey)
                    .WithGateway(new GatewayConfig
                    {
                        Hostname = server.Hostname,
                        Port = server.Port,
                        Protocol = ProtocolEnum.Http
                    });

                // A nested key so we also confirm URL mutation does not disturb path signing.
                //
                // In virtual-hosted style the bucket lives in the Host header (bucket.s3.region.hostname), which
                // the gateway redirect replaces with the gateway host. The synthetic server resolves the bucket
                // from the request path, so once the bucket has left the Host it 404s as NoSuchBucket - exactly
                // as a path-routing endpoint would. That is expected here: this test verifies how the request is
                // signed and addressed, not that this particular server can serve a virtual-hosted request, so we
                // only require that the request was sent and captured.
                await AssertThrowsAsync<WebException>(
                    () => client.Object.GetAsync(_SyntheticBucketName, _SyntheticFolderKeyOne, token: cancellationToken)).ConfigureAwait(false);

                AssertEqual(
                    server.Hostname + ":" + server.Port.ToString(),
                    server.LastHostHeader,
                    "The gateway should receive Host set to the gateway host; it is expected to rewrite Host to the upstream authority before forwarding.");
                AssertEqual(
                    "/" + _SyntheticFolderKeyOne,
                    server.LastRawPathAndQuery,
                    "Virtual-hosted gateway routing keeps the bucket in the host and the key in the path unchanged.");

                // For virtual-hosted style the signed authority is bucket-prefixed: bucket.s3.region.hostname.
                string virtualHostedAuthority = _SyntheticBucketName + ".s3." + server.Region + "." + _GatewayUpstreamHostname;

                AssertGatewaySignatureBoundToUpstream(
                    server,
                    server.Region,
                    ProtocolEnum.Https,
                    virtualHostedAuthority,
                    443);
            }
            finally
            {
                httpClient?.Dispose();
            }
        }

        /// <summary>
        /// Proves the SigV4 signature carried by a gateway-routed request is bound to the upstream request
        /// authority (the host the gateway is expected to restore), not to the gateway host the request was
        /// physically sent to. Recomputes the signature against the upstream authority and confirms it matches
        /// the signature the client sent, while the gateway authority does not - so a gateway that rewrites Host
        /// back to the upstream authority yields a request that validates, and one that leaves the gateway host
        /// in place does not.
        /// </summary>
        private static void AssertGatewaySignatureBoundToUpstream(
            S3LiteSyntheticServer server,
            string region,
            ProtocolEnum upstreamProtocol,
            string upstreamHost,
            int upstreamPort)
        {
            NameValueCollection? received = server.LastRequestHeaders;
            AssertNotNull(received, "captured gateway request headers");
            AssertNotNull(server.LastHttpMethod, "captured gateway request method");
            AssertNotNull(server.LastRawPathAndQuery, "captured gateway request path");

            string? authorization = received!["Authorization"];
            AssertNotNull(authorization, "captured gateway Authorization header");

            string capturedSignature = ParseAuthorizationField(authorization!, "Signature=");
            List<string> signedHeaders = ParseAuthorizationField(authorization!, "SignedHeaders=")
                .Split(';')
                .Select(static h => h.Trim())
                .Where(static h => h.Length > 0)
                .ToList();
            AssertTrue(signedHeaders.Contains("host"), "The SigV4 SignedHeaders must include the host header.");

            string method = server.LastHttpMethod!;
            string pathAndQuery = server.LastRawPathAndQuery!;
            string timestamp = received["x-amz-date"]
                ?? throw new InvalidOperationException("The gateway request is missing the x-amz-date header.");

            string upstreamSignature = RecomputeGatewaySignature(
                received, signedHeaders, method, timestamp, region,
                upstreamProtocol == ProtocolEnum.Https ? "https" : "http",
                upstreamHost, upstreamPort, pathAndQuery);

            AssertEqual(
                capturedSignature,
                upstreamSignature,
                "The SigV4 signature must validate once the gateway rewrites Host to the upstream authority "
                + upstreamHost + " (it is bound to the upstream request, not the gateway host).");

            // Negative control: the signature must NOT match the gateway authority the request was sent to,
            // confirming the binding is genuinely to the upstream authority rather than incidentally to any host.
            string gatewayHost = server.Hostname;
            int gatewayPort = server.Port;
            string gatewaySignature = RecomputeGatewaySignature(
                received, signedHeaders, method, timestamp, region,
                "http", gatewayHost, gatewayPort, pathAndQuery);

            AssertTrue(
                !String.Equals(capturedSignature, gatewaySignature, StringComparison.Ordinal),
                "The signature must not match the gateway authority " + gatewayHost + ":" + gatewayPort.ToString()
                + "; otherwise the upstream would reject the forwarded request.");
        }

        private static string RecomputeGatewaySignature(
            NameValueCollection received,
            List<string> signedHeaders,
            string method,
            string timestamp,
            string region,
            string scheme,
            string host,
            int port,
            string pathAndQuery)
        {
            bool isStandardPort = (scheme == "https" && port == 443) || (scheme == "http" && port == 80);
            string authority = isStandardPort ? host : host + ":" + port.ToString();

            NameValueCollection signingHeaders = new NameValueCollection();
            foreach (string name in signedHeaders)
            {
                if (String.Equals(name, "host", StringComparison.OrdinalIgnoreCase))
                    signingHeaders[name] = authority;
                else
                    signingHeaders[name] = received[name];
            }

            string fullUrl = scheme + "://" + host + ":" + port.ToString() + pathAndQuery;

            V4SignatureResult signature = new V4SignatureResult(
                timestamp,
                method.ToUpperInvariant(),
                fullUrl,
                _GatewayAccessKey,
                _GatewaySecretKey,
                region,
                "s3",
                signingHeaders,
                null);

            return signature.Signature;
        }

        private static string ParseAuthorizationField(string authorization, string fieldPrefix)
        {
            int start = authorization.IndexOf(fieldPrefix, StringComparison.Ordinal);
            if (start < 0) throw new InvalidOperationException("Authorization header is missing '" + fieldPrefix + "'.");
            start += fieldPrefix.Length;

            int end = authorization.IndexOf(',', start);
            if (end < 0) end = authorization.Length;

            return authorization.Substring(start, end - start).Trim();
        }

        private static async Task TestSyntheticServiceListBucketsAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                ListAllMyBucketsResult result = await client.Service.ListBucketsAsync(token: ct).ConfigureAwait(false);

                AssertNotNull(result, "list buckets result");
                AssertNotNull(result.Owner, "list buckets owner");
                AssertNotNull(result.Buckets, "list buckets container");
                AssertTrue(result.Buckets.BucketList.Any(bucket => String.Equals(bucket.Name, _SyntheticBucketName, StringComparison.Ordinal)), "Synthetic bucket should appear in the bucket list.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticServiceListBucketsDeniedAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                await AssertThrowsAsync<WebException>(
                    () => client.Service.ListBucketsAsync(token: ct),
                    exception => AssertWebException(exception, 403, ErrorCode.AccessDenied.ToString())).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
