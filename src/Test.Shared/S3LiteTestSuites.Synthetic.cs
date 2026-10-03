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
                    new TestCaseDescriptor(suiteId, "ObjectGetMetadataRepeatedHeaders", "Object.GetMetadataAsync and GetAsync tolerate repeated response headers using " + modeLabel, ct => TestSyntheticObjectGetMetadataRepeatedHeadersAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectGetMissingObject", "Object.GetAsync reports NoSuchKey using " + modeLabel, ct => TestSyntheticObjectGetMissingObjectAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectGetMissingBucket", "Object.GetAsync reports NoSuchBucket using " + modeLabel, ct => TestSyntheticObjectGetMissingBucketAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectGetMetadataMissingObject", "Object.GetMetadataAsync reports 404 for a missing key using " + modeLabel, ct => TestSyntheticObjectGetMetadataMissingObjectAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectWriteDenied", "Object.WriteAsync rejects anonymous writes using " + modeLabel, ct => TestSyntheticObjectWriteDeniedAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectDeleteDenied", "Object.DeleteAsync rejects anonymous deletes using " + modeLabel, ct => TestSyntheticObjectDeleteDeniedAsync(mode, ct)),
                    new TestCaseDescriptor(suiteId, "ObjectDeleteMultipleDenied", "Object.DeleteMultipleAsync rejects anonymous batch deletes using " + modeLabel, ct => TestSyntheticObjectDeleteMultipleDeniedAsync(mode, ct)),
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
                new TestCaseDescriptor(suiteId, "ObjectDeleteMultipleRemovesObjects", "Object.DeleteMultipleAsync removes all supplied objects using " + modeLabel, ct => TestSyntheticObjectDeleteMultipleRemovesObjectsAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectDeleteMultipleByKeys", "Object.DeleteMultipleAsync (keys overload) removes objects using " + modeLabel, ct => TestSyntheticObjectDeleteMultipleByKeysAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectDeleteMultiplePartialFailure", "Object.DeleteMultipleAsync reports per-object errors for missing keys using " + modeLabel, ct => TestSyntheticObjectDeleteMultiplePartialFailureAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectDeleteMultipleQuiet", "Object.DeleteMultipleAsync quiet mode omits deleted entries using " + modeLabel, ct => TestSyntheticObjectDeleteMultipleQuietAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectDeleteMultipleMissingBucket", "Object.DeleteMultipleAsync reports NoSuchBucket using " + modeLabel, ct => TestSyntheticObjectDeleteMultipleMissingBucketAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectSpecialCharacterKey", "Object APIs round-trip keys with spaces using " + modeLabel, ct => TestSyntheticObjectSpecialCharacterKeyAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectVersionIdParameter", "Object APIs accept versionId query parameters using " + modeLabel, ct => TestSyntheticObjectVersionIdParameterAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "ObjectWriteMissingBucket", "Object.WriteAsync reports NoSuchBucket using " + modeLabel, ct => TestSyntheticObjectWriteMissingBucketAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "GatewayRoutingPathStyle", "Gateway routing sends to the gateway host while the SigV4 signature stays bound to the path-style upstream authority (non-standard port) using " + modeLabel, ct => TestSyntheticGatewayRoutingPathStyleAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "GatewayRoutingVirtualHosted", "Gateway routing keeps the SigV4 signature bound to the bucket-prefixed virtual-hosted upstream authority for a nested key using " + modeLabel, ct => TestSyntheticGatewayRoutingVirtualHostedAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "GatewayRoutingBasePath", "Gateway BasePath prefixes the sent path while the SigV4 signature stays bound to the un-prefixed upstream path (special-character key) using " + modeLabel, ct => TestSyntheticGatewayRoutingBasePathAsync(mode, ct)),
                new TestCaseDescriptor(suiteId, "GatewayRoutingPathRewrite", "Gateway PathRewrite transforms the sent path while the SigV4 signature stays bound to the untransformed upstream path using " + modeLabel, ct => TestSyntheticGatewayRoutingPathRewriteAsync(mode, ct)),
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

        private static async Task TestSyntheticObjectDeleteMultipleDeniedAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                await AssertThrowsAsync<WebException>(
                    () => client.Object.DeleteMultipleAsync(_SyntheticBucketName, new[] { _SyntheticHelloKey }, token: ct),
                    exception => AssertWebException(exception, 403, ErrorCode.AccessDenied.ToString())).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectDeleteMultipleRemovesObjectsAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                string keyA = GenerateObjectKey();
                string keyB = GenerateObjectKey();
                string keyC = GenerateObjectKey();

                await client.Object.WriteAsync(_SyntheticBucketName, keyA, GetBytes("a"), "text/plain", token: ct).ConfigureAwait(false);
                await client.Object.WriteAsync(_SyntheticBucketName, keyB, GetBytes("b"), "text/plain", token: ct).ConfigureAwait(false);
                await client.Object.WriteAsync(_SyntheticBucketName, keyC, GetBytes("c"), "text/plain", token: ct).ConfigureAwait(false);

                DeleteResult result = await client.Object.DeleteMultipleAsync(
                    _SyntheticBucketName,
                    new List<ObjectIdentifier>
                    {
                        new ObjectIdentifier(keyA),
                        new ObjectIdentifier(keyB),
                        new ObjectIdentifier(keyC)
                    },
                    token: ct).ConfigureAwait(false);

                AssertEqual(3, result.Deleted.Count, "deleted object count");
                AssertEqual(0, result.Errors.Count, "delete error count");
                AssertTrue(result.Deleted.Any(deleted => String.Equals(deleted.Key, keyA, StringComparison.Ordinal)), "Deleted list should contain keyA.");
                AssertTrue(result.Deleted.Any(deleted => String.Equals(deleted.Key, keyB, StringComparison.Ordinal)), "Deleted list should contain keyB.");
                AssertTrue(result.Deleted.Any(deleted => String.Equals(deleted.Key, keyC, StringComparison.Ordinal)), "Deleted list should contain keyC.");
                AssertFalse(server.ObjectExists(_SyntheticBucketName, keyA), "keyA should be removed from the synthetic server.");
                AssertFalse(server.ObjectExists(_SyntheticBucketName, keyB), "keyB should be removed from the synthetic server.");
                AssertFalse(server.ObjectExists(_SyntheticBucketName, keyC), "keyC should be removed from the synthetic server.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectDeleteMultipleByKeysAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                string keyA = GenerateObjectKey();
                string keyB = GenerateObjectKey();

                await client.Object.WriteAsync(_SyntheticBucketName, keyA, GetBytes("a"), "text/plain", token: ct).ConfigureAwait(false);
                await client.Object.WriteAsync(_SyntheticBucketName, keyB, GetBytes("b"), "text/plain", token: ct).ConfigureAwait(false);

                DeleteResult result = await client.Object.DeleteMultipleAsync(
                    _SyntheticBucketName,
                    new[] { keyA, keyB },
                    token: ct).ConfigureAwait(false);

                AssertEqual(2, result.Deleted.Count, "deleted object count (keys overload)");
                AssertEqual(0, result.Errors.Count, "delete error count (keys overload)");
                AssertFalse(server.ObjectExists(_SyntheticBucketName, keyA), "keyA should be removed via the keys overload.");
                AssertFalse(server.ObjectExists(_SyntheticBucketName, keyB), "keyB should be removed via the keys overload.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectDeleteMultiplePartialFailureAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                string existingKey = GenerateObjectKey();
                string missingKey = GenerateObjectKey();

                await client.Object.WriteAsync(_SyntheticBucketName, existingKey, GetBytes("x"), "text/plain", token: ct).ConfigureAwait(false);

                DeleteResult result = await client.Object.DeleteMultipleAsync(
                    _SyntheticBucketName,
                    new[] { existingKey, missingKey },
                    token: ct).ConfigureAwait(false);

                AssertEqual(1, result.Deleted.Count, "partial-failure deleted count");
                AssertEqual(1, result.Errors.Count, "partial-failure error count");
                AssertEqual(existingKey, result.Deleted[0].Key, "partial-failure deleted key");
                AssertEqual(missingKey, result.Errors[0].Key, "partial-failure errored key");
                AssertEqual(ErrorCode.NoSuchKey.ToString(), result.Errors[0].Code, "partial-failure error code");
                AssertFalse(server.ObjectExists(_SyntheticBucketName, existingKey), "The existing key should be removed even when another key fails.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectDeleteMultipleQuietAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                string keyA = GenerateObjectKey();
                string keyB = GenerateObjectKey();

                await client.Object.WriteAsync(_SyntheticBucketName, keyA, GetBytes("a"), "text/plain", token: ct).ConfigureAwait(false);
                await client.Object.WriteAsync(_SyntheticBucketName, keyB, GetBytes("b"), "text/plain", token: ct).ConfigureAwait(false);

                DeleteResult result = await client.Object.DeleteMultipleAsync(
                    _SyntheticBucketName,
                    new[] { keyA, keyB },
                    quiet: true,
                    token: ct).ConfigureAwait(false);

                AssertEqual(0, result.Deleted.Count, "quiet-mode deleted count");
                AssertEqual(0, result.Errors.Count, "quiet-mode error count");
                AssertFalse(server.ObjectExists(_SyntheticBucketName, keyA), "keyA should be removed in quiet mode.");
                AssertFalse(server.ObjectExists(_SyntheticBucketName, keyB), "keyB should be removed in quiet mode.");
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSyntheticObjectDeleteMultipleMissingBucketAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, true, async (server, httpClient, client, ct) =>
            {
                await AssertThrowsAsync<WebException>(
                    () => client.Object.DeleteMultipleAsync(_SyntheticMissingBucketName, new[] { "one.txt", "two.txt" }, token: ct),
                    exception => AssertWebException(exception, 404, ErrorCode.NoSuchBucket.ToString())).ConfigureAwait(false);
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

        private static async Task TestSyntheticObjectGetMetadataRepeatedHeadersAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(mode, false, async (server, httpClient, client, ct) =>
            {
                server.AddRepeatedObjectResponseHeader("Set-Cookie", "first=1; Path=/");
                server.AddRepeatedObjectResponseHeader("Set-Cookie", "second=2; Path=/");
                server.AddRepeatedObjectResponseHeader("x-amz-meta-tag", "one");
                server.AddRepeatedObjectResponseHeader("x-amz-meta-tag", "two");

                DateTime beforeRequestUtc = DateTime.UtcNow;
                ObjectMetadata metadata = await client.Object.GetMetadataAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);

                AssertEqual(_SyntheticHelloKey, metadata.Key, "metadata key");
                AssertEqual(GetBytes(_SyntheticHelloContent).LongLength, metadata.Size, "metadata size");
                AssertEqual("text/plain", metadata.ContentType, "metadata content type");
                AssertTrue(!String.IsNullOrWhiteSpace(metadata.ETag), "Metadata ETag should not be empty.");
                AssertTrue(metadata.ETag.StartsWith("\"", StringComparison.Ordinal), "Metadata ETag should be quoted.");
                AssertFalse(metadata.ETag.Contains(","), "Metadata ETag should not absorb repeated header values.");
                AssertTrue(metadata.LastModified.ToUniversalTime() <= beforeRequestUtc, "Metadata LastModified should be parsed from the seeded object.");

                byte[] data = await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);
                AssertByteArraysEqual(GetBytes(_SyntheticHelloContent), data, "synthetic object bytes with repeated headers");
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

        private static async Task TestSyntheticGatewayRoutingBasePathAsync(RequestTransportMode mode, CancellationToken cancellationToken)
        {
            await using S3LiteSyntheticServer server = await S3LiteSyntheticServer.StartAsync(cancellationToken).ConfigureAwait(false);
            SeedSyntheticServer(server);

            // A non-standard upstream port so the signed path-style authority is host:8443.
            const int upstreamPort = 8443;
            const string basePath = "/proxy/s3";
            HttpClient? httpClient = null;

            try
            {
                if (mode == RequestTransportMode.External)
                {
                    httpClient = server.CreateHttpClient();
                }

                S3Client client = httpClient == null ? new S3Client() : new S3Client(httpClient);

                Action<S3Client> configureUpstream = c => c
                    .WithHostname(_GatewayUpstreamHostname)
                    .WithPort(upstreamPort)
                    .WithProtocol(ProtocolEnum.Https)
                    .WithRegion(server.Region)
                    .WithRequestStyle(RequestStyleEnum.PathStyle)
                    .WithAccessKey(_GatewayAccessKey)
                    .WithSecretKey(_GatewaySecretKey);

                configureUpstream(client);
                client.WithGateway(new GatewayConfig
                {
                    Hostname = server.Hostname,
                    Port = server.Port,
                    Protocol = ProtocolEnum.Http,
                    BasePath = basePath
                });

                // A reference client with the identical upstream configuration but no gateway yields the path the
                // signature is bound to; the gateway is expected to strip BasePath to recover it before forwarding.
                S3Client reference = new S3Client();
                configureUpstream(reference);
                string upstreamPathAndQuery = new Uri(reference.BuildUrl(_SyntheticBucketName, _SyntheticHelloKey, null)).PathAndQuery;

                // The synthetic server resolves the bucket from the path; with BasePath prefixed it sees a
                // "proxy" bucket and 404s as a path-routing endpoint would. That is expected - this verifies how
                // the request is addressed and signed, not that this server serves a sub-path-mounted request.
                await AssertThrowsAsync<WebException>(
                    () => client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: cancellationToken)).ConfigureAwait(false);

                AssertEqual(
                    server.Hostname + ":" + server.Port.ToString(),
                    server.LastHostHeader,
                    "The gateway should receive Host set to the gateway host.");
                AssertEqual(
                    S3Client.CombineGatewayPath(basePath, upstreamPathAndQuery),
                    server.LastRawPathAndQuery,
                    "The sent path must be the signed upstream path prefixed with BasePath.");

                AssertGatewaySignatureBoundToUpstream(
                    server,
                    server.Region,
                    ProtocolEnum.Https,
                    _GatewayUpstreamHostname,
                    upstreamPort,
                    upstreamPathAndQuery);

                // Encoding round-trip: a key with a space must survive the BasePath rewrite byte-for-byte, so the
                // gateway can strip the prefix and hand the upstream a path that still matches the signature.
                const string spaceKey = "folder/file with spaces.txt";
                string upstreamSpacePathAndQuery = new Uri(reference.BuildUrl(_SyntheticBucketName, spaceKey, null)).PathAndQuery;

                await AssertThrowsAsync<WebException>(
                    () => client.Object.GetAsync(_SyntheticBucketName, spaceKey, token: cancellationToken)).ConfigureAwait(false);

                AssertEqual(
                    S3Client.CombineGatewayPath(basePath, upstreamSpacePathAndQuery),
                    server.LastRawPathAndQuery,
                    "BasePath prefixing must preserve the percent-encoding of the upstream path.");
            }
            finally
            {
                httpClient?.Dispose();
            }
        }

        private static async Task TestSyntheticGatewayRoutingPathRewriteAsync(RequestTransportMode mode, CancellationToken cancellationToken)
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

                Action<S3Client> configureUpstream = c => c
                    .WithHostname(_GatewayUpstreamHostname)
                    .WithPort(443)
                    .WithProtocol(ProtocolEnum.Https)
                    .WithRegion(server.Region)
                    .WithRequestStyle(RequestStyleEnum.PathStyle)
                    .WithAccessKey(_GatewayAccessKey)
                    .WithSecretKey(_GatewaySecretKey);

                configureUpstream(client);
                client.WithGateway(new GatewayConfig
                {
                    Hostname = server.Hostname,
                    Port = server.Port,
                    Protocol = ProtocolEnum.Http,
                    PathRewrite = p => "/api/v2" + p
                });

                S3Client reference = new S3Client();
                configureUpstream(reference);
                string upstreamPathAndQuery = new Uri(reference.BuildUrl(_SyntheticBucketName, _SyntheticFolderKeyOne, null)).PathAndQuery;

                await AssertThrowsAsync<WebException>(
                    () => client.Object.GetAsync(_SyntheticBucketName, _SyntheticFolderKeyOne, token: cancellationToken)).ConfigureAwait(false);

                AssertEqual(
                    "/api/v2" + upstreamPathAndQuery,
                    server.LastRawPathAndQuery,
                    "The sent path must be the signed upstream path transformed by PathRewrite.");

                AssertGatewaySignatureBoundToUpstream(
                    server,
                    server.Region,
                    ProtocolEnum.Https,
                    _GatewayUpstreamHostname,
                    443,
                    upstreamPathAndQuery);
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
            int upstreamPort,
            string? upstreamPathAndQuery = null)
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
            // The signature is bound to the upstream (signed) path, which differs from the captured sent path
            // when a gateway BasePath/PathRewrite is in effect; callers pass it explicitly in that case.
            string pathAndQuery = upstreamPathAndQuery ?? server.LastRawPathAndQuery!;
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
