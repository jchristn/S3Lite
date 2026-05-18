namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using S3Lite;
    using S3Lite.ApiObjects;
    using Touchstone.Core;

    /// <summary>
    /// Shared Touchstone test suites for S3Lite.
    /// </summary>
    public static partial class S3LiteTestSuites
    {
        /// <summary>
        /// All S3Lite test suites created from the current active settings.
        /// </summary>
        public static IReadOnlyList<TestSuiteDescriptor> All
        {
            get
            {
                S3LiteTestSettings settings = S3LiteTestSettings.Current;

                return new List<TestSuiteDescriptor>
                {
                    BuildGuardSuite(),
                    BuildInternalSurfaceSuite(),
                    BuildSyntheticAnonymousSuite(RequestTransportMode.Internal),
                    BuildSyntheticAnonymousSuite(RequestTransportMode.External),
                    BuildSyntheticAuthenticatedSuite(RequestTransportMode.Internal),
                    BuildSyntheticAuthenticatedSuite(RequestTransportMode.External),
                    BuildConnectivitySuite(settings),
                    BuildObjectLifecycleSuite(settings)
                };
            }
        }

        private static TestSuiteDescriptor BuildConnectivitySuite(S3LiteTestSettings settings)
        {
            string suiteId = "Connectivity";
            S3LiteTestContext context = new S3LiteTestContext(settings);
            string bucketRequiredReason = "Set S3LITE_TEST_BUCKET or pass --bucket to run bucket and object connectivity tests.";
            string credentialsRequiredReason = "Provide credentials to run Service.ListBucketsAsync.";

            return new TestSuiteDescriptor(
                suiteId,
                "Connectivity and read operations",
                new List<TestCaseDescriptor>
                {
                    settings.HasCredentials
                        ? new TestCaseDescriptor(
                            suiteId,
                            "ListBuckets",
                            "Service.ListBucketsAsync returns a bucket list",
                            ct => TestListBucketsAsync(context, ct))
                        : CreateSkipCase(
                            suiteId,
                            "ListBuckets",
                            "Service.ListBucketsAsync returns a bucket list",
                            credentialsRequiredReason),
                    settings.HasBucket
                        ? new TestCaseDescriptor(
                            suiteId,
                            "BucketExists",
                            "Bucket.ExistsAsync confirms the configured bucket",
                            ct => TestBucketExistsAsync(context, ct))
                        : CreateSkipCase(
                            suiteId,
                            "BucketExists",
                            "Bucket.ExistsAsync confirms the configured bucket",
                            bucketRequiredReason),
                    settings.HasBucket
                        ? new TestCaseDescriptor(
                            suiteId,
                            "BucketList",
                            "Bucket.ListAsync returns object metadata",
                            ct => TestBucketListAsync(context, ct))
                        : CreateSkipCase(
                            suiteId,
                            "BucketList",
                            "Bucket.ListAsync returns object metadata",
                            bucketRequiredReason),
                    settings.HasBucket
                        ? new TestCaseDescriptor(
                            suiteId,
                            "BucketExistsWithCustomHttpClient",
                            "Bucket.ExistsAsync works with a caller-supplied HttpClient",
                            ct => TestBucketExistsWithCustomHttpClientAsync(context, ct))
                        : CreateSkipCase(
                            suiteId,
                            "BucketExistsWithCustomHttpClient",
                            "Bucket.ExistsAsync works with a caller-supplied HttpClient",
                            bucketRequiredReason),
                    settings.HasBucket
                        ? new TestCaseDescriptor(
                            suiteId,
                            "MissingObjectReturnsFalse",
                            "Object.ExistsAsync returns false for a missing key",
                            ct => TestMissingObjectAsync(context, ct))
                        : CreateSkipCase(
                            suiteId,
                            "MissingObjectReturnsFalse",
                            "Object.ExistsAsync returns false for a missing key",
                            bucketRequiredReason)
                });
        }

        private static TestSuiteDescriptor BuildObjectLifecycleSuite(S3LiteTestSettings settings)
        {
            string suiteId = "ObjectLifecycle";
            S3LiteTestContext context = new S3LiteTestContext(settings);
            string bucketRequiredReason = "Set S3LITE_TEST_BUCKET or pass --bucket to run write and cleanup tests.";
            string writeTestsSkippedReason = "Write tests were disabled through configuration.";
            string cleanupSkippedReason = "Cleanup was disabled through configuration.";

            return new TestSuiteDescriptor(
                suiteId,
                "Object write, read, overwrite, and cleanup operations",
                new List<TestCaseDescriptor>
                {
                    CreateWriteCaseOrSkip(settings, suiteId, "WriteSmallObject", "Object.WriteAsync writes a text object", bucketRequiredReason, writeTestsSkippedReason, ct => TestWriteSmallObjectAsync(context, ct)),
                    CreateWriteCaseOrSkip(settings, suiteId, "SmallObjectExists", "Object.ExistsAsync confirms the new text object", bucketRequiredReason, writeTestsSkippedReason, ct => TestSmallObjectExistsAsync(context, ct)),
                    CreateWriteCaseOrSkip(settings, suiteId, "SmallObjectMetadata", "Object.GetMetadataAsync returns metadata for the text object", bucketRequiredReason, writeTestsSkippedReason, ct => TestSmallObjectMetadataAsync(context, ct)),
                    CreateWriteCaseOrSkip(settings, suiteId, "ReadSmallObject", "Object.GetAsync reads the text object", bucketRequiredReason, writeTestsSkippedReason, ct => TestReadSmallObjectAsync(context, ct)),
                    CreateWriteCaseOrSkip(settings, suiteId, "OverwriteSmallObject", "Object.WriteAsync overwrites an existing object", bucketRequiredReason, writeTestsSkippedReason, ct => TestOverwriteSmallObjectAsync(context, ct)),
                    CreateWriteCaseOrSkip(settings, suiteId, "ReadOverwrittenObject", "Object.GetAsync reads overwritten content", bucketRequiredReason, writeTestsSkippedReason, ct => TestReadOverwrittenObjectAsync(context, ct)),
                    CreateWriteCaseOrSkip(settings, suiteId, "WriteLargeObject", "Object.WriteAsync writes a 1 MB payload", bucketRequiredReason, writeTestsSkippedReason, ct => TestWriteLargeObjectAsync(context, ct)),
                    CreateWriteCaseOrSkip(settings, suiteId, "ReadLargeObject", "Object.GetAsync reads the 1 MB payload", bucketRequiredReason, writeTestsSkippedReason, ct => TestReadLargeObjectAsync(context, ct)),
                    CreateWriteCaseOrSkip(settings, suiteId, "WriteEmptyObject", "Object.WriteAsync writes an empty object", bucketRequiredReason, writeTestsSkippedReason, ct => TestWriteEmptyObjectAsync(context, ct)),
                    CreateWriteCaseOrSkip(settings, suiteId, "ReadEmptyObject", "Object.GetAsync reads an empty object", bucketRequiredReason, writeTestsSkippedReason, ct => TestReadEmptyObjectAsync(context, ct)),
                    CreateWriteCaseOrSkip(settings, suiteId, "WriteSpecialCharacterObject", "Object.WriteAsync stores keys with spaces", bucketRequiredReason, writeTestsSkippedReason, ct => TestWriteSpecialCharacterObjectAsync(context, ct)),
                    CreateWriteCaseOrSkip(settings, suiteId, "ReadSpecialCharacterObject", "Object.GetAsync reads keys with spaces", bucketRequiredReason, writeTestsSkippedReason, ct => TestReadSpecialCharacterObjectAsync(context, ct)),
                    CreateWriteCaseOrSkip(settings, suiteId, "ListByPrefix", "Bucket.ListAsync filters by the suite prefix", bucketRequiredReason, writeTestsSkippedReason, ct => TestListByPrefixAsync(context, ct)),
                    settings.HasBucket && !settings.SkipWriteTests && !settings.SkipCleanup
                        ? new TestCaseDescriptor(
                            suiteId,
                            "CleanupObjects",
                            "Object.DeleteAsync removes all suite artifacts",
                            ct => TestCleanupObjectsAsync(context, ct))
                        : CreateSkipCase(
                            suiteId,
                            "CleanupObjects",
                            "Object.DeleteAsync removes all suite artifacts",
                            !settings.HasBucket ? bucketRequiredReason : (settings.SkipWriteTests ? writeTestsSkippedReason : cleanupSkippedReason))
                });
        }

        private static TestCaseDescriptor CreateWriteCaseOrSkip(
            S3LiteTestSettings settings,
            string suiteId,
            string caseId,
            string displayName,
            string bucketRequiredReason,
            string writeTestsSkippedReason,
            Func<CancellationToken, Task> executeAsync)
        {
            if (!settings.HasBucket) return CreateSkipCase(suiteId, caseId, displayName, bucketRequiredReason);
            if (settings.SkipWriteTests) return CreateSkipCase(suiteId, caseId, displayName, writeTestsSkippedReason);
            return new TestCaseDescriptor(suiteId, caseId, displayName, executeAsync);
        }

        private static TestCaseDescriptor CreateSkipCase(string suiteId, string caseId, string displayName, string skipReason)
        {
            return new TestCaseDescriptor(
                suiteId,
                caseId,
                displayName,
                _ => Task.CompletedTask,
                skip: true,
                skipReason: skipReason);
        }

        private static async Task TestListBucketsAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            ListAllMyBucketsResult result = await client.Service.ListBucketsAsync(token: cancellationToken).ConfigureAwait(false);
            Buckets buckets = result.Buckets ?? throw new InvalidOperationException("ListBucketsAsync returned a null bucket container.");
            List<Bucket> bucketList = buckets.BucketList ?? throw new InvalidOperationException("ListBucketsAsync returned a null bucket collection.");

            Require(result != null, "ListBucketsAsync returned null.");
            Require(bucketList.Count >= 0, "Bucket list count was invalid.");
        }

        private static async Task TestBucketExistsAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            bool exists = await client.Bucket.ExistsAsync(context.BucketName, token: cancellationToken).ConfigureAwait(false);
            Require(exists, "Configured bucket '" + context.BucketName + "' was not found.");
        }

        private static async Task TestBucketListAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            ListBucketResult result = await client.Bucket.ListAsync(context.BucketName, token: cancellationToken).ConfigureAwait(false);
            List<ObjectMetadata> contents = result.Contents ?? throw new InvalidOperationException("Bucket.ListAsync returned a null contents collection.");

            Require(result != null, "Bucket.ListAsync returned null.");
            Require(contents.Count >= 0, "Contents count was invalid.");
        }

        private static async Task TestBucketExistsWithCustomHttpClientAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using HttpClient httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            S3Client client = context.CreateClient(httpClient);
            bool exists = await client.Bucket.ExistsAsync(context.BucketName, token: cancellationToken).ConfigureAwait(false);

            Require(Object.ReferenceEquals(httpClient, client.HttpClient), "S3Client did not retain the caller-supplied HttpClient instance.");
            Require(exists, "Configured bucket '" + context.BucketName + "' was not found when using a caller-supplied HttpClient.");
        }

        private static async Task TestMissingObjectAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            bool exists = await client.Object.ExistsAsync(context.BucketName, context.MissingObjectKey, token: cancellationToken).ConfigureAwait(false);
            Require(!exists, "Missing object '" + context.MissingObjectKey + "' unexpectedly exists.");
        }

        private static async Task TestWriteSmallObjectAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            byte[] data = context.GetUtf8Bytes(context.SmallContent);

            await client.Object.WriteAsync(
                context.BucketName,
                context.SmallObjectKey,
                data,
                "text/plain",
                token: cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestSmallObjectExistsAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            bool exists = await client.Object.ExistsAsync(context.BucketName, context.SmallObjectKey, token: cancellationToken).ConfigureAwait(false);
            Require(exists, "Small test object '" + context.SmallObjectKey + "' does not exist after write.");
        }

        private static async Task TestSmallObjectMetadataAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            ObjectMetadata metadata = await client.Object.GetMetadataAsync(context.BucketName, context.SmallObjectKey, token: cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("GetMetadataAsync returned null for '" + context.SmallObjectKey + "'.");

            Require(String.Equals(metadata.Key, context.SmallObjectKey, StringComparison.Ordinal), "Metadata returned key '" + metadata.Key + "' instead of '" + context.SmallObjectKey + "'.");
            Require(metadata.Size == context.GetUtf8Bytes(context.SmallContent).Length, "Metadata reported size " + metadata.Size.ToString() + " instead of the expected " + context.GetUtf8Bytes(context.SmallContent).Length.ToString() + ".");
        }

        private static async Task TestReadSmallObjectAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            byte[] data = await client.Object.GetAsync(context.BucketName, context.SmallObjectKey, token: cancellationToken).ConfigureAwait(false);
            string content = Encoding.UTF8.GetString(data);

            Require(String.Equals(content, context.SmallContent, StringComparison.Ordinal), "Small object content did not match the original payload.");
        }

        private static async Task TestOverwriteSmallObjectAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            byte[] data = context.GetUtf8Bytes(context.OverwriteContent);

            await client.Object.WriteAsync(
                context.BucketName,
                context.SmallObjectKey,
                data,
                "text/plain",
                token: cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestReadOverwrittenObjectAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            byte[] data = await client.Object.GetAsync(context.BucketName, context.SmallObjectKey, token: cancellationToken).ConfigureAwait(false);
            string content = Encoding.UTF8.GetString(data);

            Require(String.Equals(content, context.OverwriteContent, StringComparison.Ordinal), "Overwritten object content did not match the expected payload.");
        }

        private static async Task TestWriteLargeObjectAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();

            await client.Object.WriteAsync(
                context.BucketName,
                context.LargeObjectKey,
                context.LargeContent,
                "application/octet-stream",
                token: cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestReadLargeObjectAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            byte[] data = await client.Object.GetAsync(context.BucketName, context.LargeObjectKey, token: cancellationToken).ConfigureAwait(false);

            Require(data.Length == context.LargeContent.Length, "Large object length " + data.Length.ToString() + " did not match the expected " + context.LargeContent.Length.ToString() + " bytes.");
            Require(data.SequenceEqual(context.LargeContent), "Large object bytes did not match the original payload.");
        }

        private static async Task TestWriteEmptyObjectAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();

            await client.Object.WriteAsync(
                context.BucketName,
                context.EmptyObjectKey,
                Array.Empty<byte>(),
                "text/plain",
                token: cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestReadEmptyObjectAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            byte[] data = await client.Object.GetAsync(context.BucketName, context.EmptyObjectKey, token: cancellationToken).ConfigureAwait(false);

            Require(data.Length == 0, "Expected an empty object but received " + data.Length.ToString() + " bytes.");
        }

        private static async Task TestWriteSpecialCharacterObjectAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            byte[] data = context.GetUtf8Bytes(context.SpecialContent);

            await client.Object.WriteAsync(
                context.BucketName,
                context.SpecialObjectKey,
                data,
                "text/plain",
                token: cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestReadSpecialCharacterObjectAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            byte[] data = await client.Object.GetAsync(context.BucketName, context.SpecialObjectKey, token: cancellationToken).ConfigureAwait(false);
            string content = Encoding.UTF8.GetString(data);

            Require(String.Equals(content, context.SpecialContent, StringComparison.Ordinal), "Special-character key content did not match the expected payload.");
        }

        private static async Task TestListByPrefixAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = context.CreateClient();
            ListBucketResult result = await client.Bucket.ListAsync(context.BucketName, prefix: context.ObjectPrefix, token: cancellationToken).ConfigureAwait(false);
            List<ObjectMetadata> contents = result.Contents ?? throw new InvalidOperationException("Bucket.ListAsync returned a null contents collection for prefix '" + context.ObjectPrefix + "'.");

            Require(result != null, "Bucket.ListAsync returned null when filtering by prefix.");
            List<string> keys = contents.Select(metadata => metadata.Key).ToList();

            Require(keys.Contains(context.SmallObjectKey), "Prefix listing did not include '" + context.SmallObjectKey + "'.");
            Require(keys.Contains(context.LargeObjectKey), "Prefix listing did not include '" + context.LargeObjectKey + "'.");
            Require(keys.Contains(context.EmptyObjectKey), "Prefix listing did not include '" + context.EmptyObjectKey + "'.");
            Require(keys.Contains(context.SpecialObjectKey), "Prefix listing did not include '" + context.SpecialObjectKey + "'.");
        }

        private static async Task TestCleanupObjectsAsync(S3LiteTestContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await context.CleanupObjectsAsync(cancellationToken).ConfigureAwait(false);

            S3Client client = context.CreateClient();

            foreach (string key in context.CleanupKeys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool exists = await client.Object.ExistsAsync(context.BucketName, key, token: cancellationToken).ConfigureAwait(false);
                Require(!exists, "Cleanup did not remove object '" + key + "'.");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
