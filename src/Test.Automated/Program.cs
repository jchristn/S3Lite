namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using S3Lite;
    using S3Lite.ApiObjects;

    /// <summary>
    /// Automated test program for S3Lite library.
    /// Supports command-line arguments for configuration and runs comprehensive tests.
    /// </summary>
    internal class Program
    {
        #region Private-Members

        private static string _Endpoint = "amazonaws.com";
        private static int _Port = 443;
        private static string _Region = "us-west-1";
        private static string _AccessKey = null;
        private static string _SecretKey = null;
        private static string _Bucket = null;
        private static ProtocolEnum _Protocol = ProtocolEnum.Https;
        private static RequestStyleEnum _RequestStyle = RequestStyleEnum.VirtualHostedStyle;
        private static bool _Verbose = false;
        private static bool _SkipCleanup = false;
        private static bool _SkipWriteTests = false;

        private static S3Client _S3Client = null;
        private static int _TestsPassed = 0;
        private static int _TestsFailed = 0;
        private static List<string> _FailedTests = new List<string>();

        #endregion

        #region Main

        static int Main(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine(" S3Lite Automated Test Suite");
            Console.WriteLine("================================================================================");
            Console.WriteLine("");

            if (!ParseArguments(args))
            {
                PrintUsage();
                return 1;
            }

            PrintConfiguration();

            InitializeClient();

            RunTestsAsync().Wait();

            PrintSummary();

            return _TestsFailed > 0 ? 1 : 0;
        }

        #endregion

        #region Argument-Parsing

        private static bool ParseArguments(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();

                if (arg == "-h" || arg == "--help" || arg == "/?")
                {
                    return false;
                }
                else if (arg == "-e" || arg == "--endpoint")
                {
                    if (i + 1 >= args.Length) return false;
                    _Endpoint = args[++i];
                }
                else if (arg == "-p" || arg == "--port")
                {
                    if (i + 1 >= args.Length) return false;
                    if (!Int32.TryParse(args[++i], out _Port)) return false;
                }
                else if (arg == "-r" || arg == "--region")
                {
                    if (i + 1 >= args.Length) return false;
                    _Region = args[++i];
                }
                else if (arg == "-a" || arg == "--access-key")
                {
                    if (i + 1 >= args.Length) return false;
                    _AccessKey = args[++i];
                }
                else if (arg == "-s" || arg == "--secret-key")
                {
                    if (i + 1 >= args.Length) return false;
                    _SecretKey = args[++i];
                }
                else if (arg == "-b" || arg == "--bucket")
                {
                    if (i + 1 >= args.Length) return false;
                    _Bucket = args[++i];
                }
                else if (arg == "--http")
                {
                    _Protocol = ProtocolEnum.Http;
                }
                else if (arg == "--https")
                {
                    _Protocol = ProtocolEnum.Https;
                }
                else if (arg == "--path-style")
                {
                    _RequestStyle = RequestStyleEnum.PathStyle;
                }
                else if (arg == "--virtual-hosted")
                {
                    _RequestStyle = RequestStyleEnum.VirtualHostedStyle;
                }
                else if (arg == "-v" || arg == "--verbose")
                {
                    _Verbose = true;
                }
                else if (arg == "--skip-cleanup")
                {
                    _SkipCleanup = true;
                }
                else if (arg == "--skip-write-tests")
                {
                    _SkipWriteTests = true;
                }
                else
                {
                    Console.WriteLine("Unknown argument: " + args[i]);
                    return false;
                }
            }

            if (String.IsNullOrEmpty(_Bucket))
            {
                Console.WriteLine("Error: Bucket name is required (--bucket or -b)");
                return false;
            }

            return true;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: Test.Automated [options]");
            Console.WriteLine("");
            Console.WriteLine("Required:");
            Console.WriteLine("  -b, --bucket <name>        Bucket name to use for testing");
            Console.WriteLine("");
            Console.WriteLine("Connection Options:");
            Console.WriteLine("  -e, --endpoint <host>      S3 endpoint hostname (default: amazonaws.com)");
            Console.WriteLine("                             For AWS with virtual-hosted style: amazonaws.com");
            Console.WriteLine("                             For AWS with path style: s3.amazonaws.com or s3.<region>.amazonaws.com");
            Console.WriteLine("                             For S3-compatible (MinIO, etc.): localhost or your server hostname");
            Console.WriteLine("  -p, --port <port>          Port number (default: 443)");
            Console.WriteLine("  -r, --region <region>      AWS region (default: us-west-1)");
            Console.WriteLine("  --http                     Use HTTP protocol");
            Console.WriteLine("  --https                    Use HTTPS protocol (default)");
            Console.WriteLine("  --path-style               Use path-style requests");
            Console.WriteLine("  --virtual-hosted           Use virtual-hosted style requests (default)");
            Console.WriteLine("");
            Console.WriteLine("Authentication (omit both for anonymous access):");
            Console.WriteLine("  -a, --access-key <key>     AWS access key");
            Console.WriteLine("  -s, --secret-key <key>     AWS secret key");
            Console.WriteLine("");
            Console.WriteLine("Test Options:");
            Console.WriteLine("  -v, --verbose              Enable verbose logging");
            Console.WriteLine("  --skip-cleanup             Skip cleanup of test objects after tests");
            Console.WriteLine("  --skip-write-tests         Skip tests that write/modify data");
            Console.WriteLine("");
            Console.WriteLine("  -h, --help                 Show this help message");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("  # Test against AWS S3 with credentials (virtual-hosted style, default)");
            Console.WriteLine("  Test.Automated -b my-bucket -a AKIAEXAMPLE -s secretkey123 -r us-east-1");
            Console.WriteLine("");
            Console.WriteLine("  # Test against AWS S3 with path style");
            Console.WriteLine("  Test.Automated -b my-bucket -e s3.us-east-1.amazonaws.com -a AKIAEXAMPLE -s secretkey123 -r us-east-1 --path-style");
            Console.WriteLine("");
            Console.WriteLine("  # Test anonymous access to a public bucket");
            Console.WriteLine("  Test.Automated -b public-bucket --skip-write-tests");
            Console.WriteLine("");
            Console.WriteLine("  # Test against MinIO or S3-compatible storage");
            Console.WriteLine("  Test.Automated -b test-bucket -e localhost -p 9000 --http --path-style -a minioadmin -s minioadmin");
            Console.WriteLine("");
        }

        private static void PrintConfiguration()
        {
            Console.WriteLine("Configuration:");
            Console.WriteLine("  Endpoint:      " + _Endpoint + ":" + _Port);
            Console.WriteLine("  Region:        " + _Region);
            Console.WriteLine("  Bucket:        " + _Bucket);
            Console.WriteLine("  Protocol:      " + _Protocol);
            Console.WriteLine("  Request Style: " + _RequestStyle);
            Console.WriteLine("  Credentials:   " + (HasCredentials() ? "Configured" : "Anonymous (no credentials)"));
            Console.WriteLine("  Verbose:       " + _Verbose);
            Console.WriteLine("  Skip Cleanup:  " + _SkipCleanup);
            Console.WriteLine("  Skip Writes:   " + _SkipWriteTests);
            Console.WriteLine("");
        }

        private static bool HasCredentials()
        {
            return !String.IsNullOrEmpty(_AccessKey) && !String.IsNullOrEmpty(_SecretKey);
        }

        #endregion

        #region Client-Initialization

        private static void InitializeClient()
        {
            _S3Client = new S3Client()
                .WithHostname(_Endpoint)
                .WithPort(_Port)
                .WithRegion(_Region)
                .WithProtocol(_Protocol)
                .WithRequestStyle(_RequestStyle);

            if (HasCredentials())
            {
                _S3Client
                    .WithAccessKey(_AccessKey)
                    .WithSecretKey(_SecretKey);
            }

            if (_Verbose)
            {
                _S3Client.WithLogger(LogMessage);
            }

            Console.WriteLine("S3 client initialized");
            Console.WriteLine("  HasCredentials: " + _S3Client.HasCredentials);
            Console.WriteLine("");
        }

        private static void LogMessage(string message)
        {
            Console.WriteLine("[LOG] " + message);
        }

        #endregion

        #region Test-Runner

        private static async Task RunTestsAsync()
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine(" Running Tests");
            Console.WriteLine("================================================================================");
            Console.WriteLine("");

            // Service-level tests
            await RunTestAsync("Service.ListBuckets", TestListBucketsAsync);

            // Bucket-level tests
            await RunTestAsync("Bucket.Exists", TestBucketExistsAsync);
            await RunTestAsync("Bucket.List", TestBucketListAsync);

            if (!_SkipWriteTests)
            {
                // Object write/read/delete tests
                await RunTestAsync("Object.Write (small)", TestObjectWriteSmallAsync);
                await RunTestAsync("Object.Exists (after write)", TestObjectExistsAfterWriteAsync);
                await RunTestAsync("Object.GetMetadata", TestObjectGetMetadataAsync);
                await RunTestAsync("Object.Get (read)", TestObjectGetAsync);
                await RunTestAsync("Object.Write (overwrite)", TestObjectOverwriteAsync);
                await RunTestAsync("Object.Get (verify overwrite)", TestObjectGetAfterOverwriteAsync);
                await RunTestAsync("Object.Write (large)", TestObjectWriteLargeAsync);
                await RunTestAsync("Object.Get (large)", TestObjectGetLargeAsync);
                await RunTestAsync("Object.Write (empty)", TestObjectWriteEmptyAsync);
                await RunTestAsync("Object.Get (empty)", TestObjectGetEmptyAsync);
                await RunTestAsync("Object.Write (special chars in key)", TestObjectSpecialCharsKeyAsync);
                await RunTestAsync("Bucket.List (with objects)", TestBucketListWithObjectsAsync);

                if (!_SkipCleanup)
                {
                    await RunTestAsync("Cleanup.DeleteObjects", TestCleanupObjectsAsync);
                }
            }

            // Tests that should work even without write access
            await RunTestAsync("Object.Exists (non-existent)", TestObjectNotExistsAsync);

            Console.WriteLine("");
        }

        private static async Task RunTestAsync(string testName, Func<Task<TestResult>> testFunc)
        {
            Console.Write("  " + testName.PadRight(40));

            try
            {
                TestResult result = await testFunc();

                if (result.Success)
                {
                    Console.WriteLine("[PASS] " + result.Message);
                    _TestsPassed++;
                }
                else
                {
                    Console.WriteLine("[FAIL] " + result.Message);
                    _TestsFailed++;
                    _FailedTests.Add(testName + ": " + result.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FAIL] Exception: " + ex.Message);
                _TestsFailed++;
                _FailedTests.Add(testName + ": " + ex.Message);

                if (_Verbose)
                {
                    Console.WriteLine("    Stack trace: " + ex.StackTrace);
                }
            }
        }

        #endregion

        #region Service-Tests

        private static async Task<TestResult> TestListBucketsAsync()
        {
            // Note: ListBuckets requires authentication, so this may fail in anonymous mode
            if (!_S3Client.HasCredentials)
            {
                return new TestResult(true, "Skipped (requires credentials)");
            }

            ListAllMyBucketsResult result = await _S3Client.Service.ListBucketsAsync();

            if (result == null)
            {
                return new TestResult(false, "Result was null");
            }

            int bucketCount = result.Buckets != null && result.Buckets.BucketList != null
                ? result.Buckets.BucketList.Count
                : 0;

            return new TestResult(true, "Found " + bucketCount + " bucket(s)");
        }

        #endregion

        #region Bucket-Tests

        private static async Task<TestResult> TestBucketExistsAsync()
        {
            bool exists = await _S3Client.Bucket.ExistsAsync(_Bucket);
            return new TestResult(true, "Bucket exists: " + exists);
        }

        private static async Task<TestResult> TestBucketListAsync()
        {
            ListBucketResult result = await _S3Client.Bucket.ListAsync(_Bucket);

            if (result == null)
            {
                return new TestResult(false, "Result was null");
            }

            int objectCount = result.Contents != null ? result.Contents.Count : 0;
            return new TestResult(true, "Found " + objectCount + " object(s)");
        }

        private static async Task<TestResult> TestBucketListWithObjectsAsync()
        {
            ListBucketResult result = await _S3Client.Bucket.ListAsync(_Bucket, prefix: "test-automated/");

            if (result == null)
            {
                return new TestResult(false, "Result was null");
            }

            int objectCount = result.Contents != null ? result.Contents.Count : 0;

            if (objectCount < 1)
            {
                return new TestResult(false, "Expected at least 1 object with prefix, found " + objectCount);
            }

            return new TestResult(true, "Found " + objectCount + " object(s) with prefix");
        }

        #endregion

        #region Object-Tests

        private static string _TestKeySmall = "test-automated/small-object.txt";
        private static string _TestKeyLarge = "test-automated/large-object.bin";
        private static string _TestKeyEmpty = "test-automated/empty-object.txt";
        private static string _TestKeySpecial = "test-automated/file with spaces.txt";
        private static string _TestContentSmall = "Hello, S3Lite! This is a test object.";
        private static string _TestContentOverwrite = "This content has been overwritten.";
        private static byte[] _TestContentLarge = null;

        private static async Task<TestResult> TestObjectWriteSmallAsync()
        {
            byte[] data = Encoding.UTF8.GetBytes(_TestContentSmall);
            await _S3Client.Object.WriteAsync(_Bucket, _TestKeySmall, data, "text/plain");
            return new TestResult(true, "Wrote " + data.Length + " bytes");
        }

        private static async Task<TestResult> TestObjectExistsAfterWriteAsync()
        {
            bool exists = await _S3Client.Object.ExistsAsync(_Bucket, _TestKeySmall);

            if (!exists)
            {
                return new TestResult(false, "Object does not exist after write");
            }

            return new TestResult(true, "Object exists");
        }

        private static async Task<TestResult> TestObjectGetMetadataAsync()
        {
            ObjectMetadata metadata = await _S3Client.Object.GetMetadataAsync(_Bucket, _TestKeySmall);

            if (metadata == null)
            {
                return new TestResult(false, "Metadata was null");
            }

            if (String.IsNullOrEmpty(metadata.Key))
            {
                return new TestResult(false, "Key was null or empty");
            }

            return new TestResult(true, "Key=" + metadata.Key + ", Size=" + metadata.Size + ", ContentType=" + metadata.ContentType);
        }

        private static async Task<TestResult> TestObjectGetAsync()
        {
            byte[] data = await _S3Client.Object.GetAsync(_Bucket, _TestKeySmall);

            if (data == null)
            {
                return new TestResult(false, "Data was null");
            }

            string content = Encoding.UTF8.GetString(data);

            if (content != _TestContentSmall)
            {
                return new TestResult(false, "Content mismatch. Expected: '" + _TestContentSmall + "', Got: '" + content + "'");
            }

            return new TestResult(true, "Read " + data.Length + " bytes, content matches");
        }

        private static async Task<TestResult> TestObjectOverwriteAsync()
        {
            byte[] data = Encoding.UTF8.GetBytes(_TestContentOverwrite);
            await _S3Client.Object.WriteAsync(_Bucket, _TestKeySmall, data, "text/plain");
            return new TestResult(true, "Overwrote with " + data.Length + " bytes");
        }

        private static async Task<TestResult> TestObjectGetAfterOverwriteAsync()
        {
            byte[] data = await _S3Client.Object.GetAsync(_Bucket, _TestKeySmall);

            if (data == null)
            {
                return new TestResult(false, "Data was null");
            }

            string content = Encoding.UTF8.GetString(data);

            if (content != _TestContentOverwrite)
            {
                return new TestResult(false, "Content mismatch after overwrite");
            }

            return new TestResult(true, "Content correctly overwritten");
        }

        private static async Task<TestResult> TestObjectWriteLargeAsync()
        {
            // Generate 1MB of random data
            _TestContentLarge = new byte[1024 * 1024];
            Random random = new Random(42); // Fixed seed for reproducibility
            random.NextBytes(_TestContentLarge);

            await _S3Client.Object.WriteAsync(_Bucket, _TestKeyLarge, _TestContentLarge, "application/octet-stream");
            return new TestResult(true, "Wrote " + _TestContentLarge.Length + " bytes (1MB)");
        }

        private static async Task<TestResult> TestObjectGetLargeAsync()
        {
            byte[] data = await _S3Client.Object.GetAsync(_Bucket, _TestKeyLarge);

            if (data == null)
            {
                return new TestResult(false, "Data was null");
            }

            if (data.Length != _TestContentLarge.Length)
            {
                return new TestResult(false, "Size mismatch. Expected: " + _TestContentLarge.Length + ", Got: " + data.Length);
            }

            // Verify content matches
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != _TestContentLarge[i])
                {
                    return new TestResult(false, "Content mismatch at byte " + i);
                }
            }

            return new TestResult(true, "Read " + data.Length + " bytes, content matches");
        }

        private static async Task<TestResult> TestObjectWriteEmptyAsync()
        {
            byte[] data = new byte[0];
            await _S3Client.Object.WriteAsync(_Bucket, _TestKeyEmpty, data, "text/plain");
            return new TestResult(true, "Wrote empty object");
        }

        private static async Task<TestResult> TestObjectGetEmptyAsync()
        {
            byte[] data = await _S3Client.Object.GetAsync(_Bucket, _TestKeyEmpty);

            if (data == null)
            {
                return new TestResult(false, "Data was null");
            }

            if (data.Length != 0)
            {
                return new TestResult(false, "Expected empty, got " + data.Length + " bytes");
            }

            return new TestResult(true, "Read empty object successfully");
        }

        private static async Task<TestResult> TestObjectSpecialCharsKeyAsync()
        {
            byte[] data = Encoding.UTF8.GetBytes("Special character test");
            await _S3Client.Object.WriteAsync(_Bucket, _TestKeySpecial, data, "text/plain");

            byte[] readData = await _S3Client.Object.GetAsync(_Bucket, _TestKeySpecial);

            if (readData == null)
            {
                return new TestResult(false, "Failed to read back object with special chars in key");
            }

            string content = Encoding.UTF8.GetString(readData);

            if (content != "Special character test")
            {
                return new TestResult(false, "Content mismatch");
            }

            return new TestResult(true, "Write/read with special chars in key successful");
        }

        private static async Task<TestResult> TestObjectNotExistsAsync()
        {
            string nonExistentKey = "test-automated/this-object-does-not-exist-" + Guid.NewGuid().ToString() + ".txt";
            bool exists = await _S3Client.Object.ExistsAsync(_Bucket, nonExistentKey);

            if (exists)
            {
                return new TestResult(false, "Object should not exist");
            }

            return new TestResult(true, "Correctly reported non-existent");
        }

        #endregion

        #region Cleanup-Tests

        private static async Task<TestResult> TestCleanupObjectsAsync()
        {
            List<string> keysToDelete = new List<string>
            {
                _TestKeySmall,
                _TestKeyLarge,
                _TestKeyEmpty,
                _TestKeySpecial
            };

            int deleted = 0;
            List<string> failedDeletes = new List<string>();

            foreach (string key in keysToDelete)
            {
                try
                {
                    await _S3Client.Object.DeleteAsync(_Bucket, key);
                    deleted++;
                }
                catch (Exception ex)
                {
                    failedDeletes.Add(key + ": " + ex.Message);
                }
            }

            if (failedDeletes.Count > 0)
            {
                return new TestResult(false, "Deleted " + deleted + "/" + keysToDelete.Count + ". Failed: " + String.Join("; ", failedDeletes));
            }

            return new TestResult(true, "Deleted " + deleted + " test objects");
        }

        #endregion

        #region Summary

        private static void PrintSummary()
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine(" Test Summary");
            Console.WriteLine("================================================================================");
            Console.WriteLine("");
            Console.WriteLine("  Total:  " + (_TestsPassed + _TestsFailed));
            Console.WriteLine("  Passed: " + _TestsPassed);
            Console.WriteLine("  Failed: " + _TestsFailed);
            Console.WriteLine("");

            if (_FailedTests.Count > 0)
            {
                Console.WriteLine("Failed Tests:");
                foreach (string failure in _FailedTests)
                {
                    Console.WriteLine("  - " + failure);
                }
                Console.WriteLine("");
            }

            if (_TestsFailed == 0)
            {
                Console.WriteLine("All tests passed!");
            }
            else
            {
                Console.WriteLine("Some tests failed. Review the output above for details.");
            }

            Console.WriteLine("");
        }

        #endregion
    }

    #region Test-Result-Class

    /// <summary>
    /// Represents the result of a test execution.
    /// </summary>
    internal class TestResult
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }

        public TestResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    #endregion
}
