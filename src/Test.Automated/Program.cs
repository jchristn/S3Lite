namespace Test.Automated
{
    using System;
    using System.Threading.Tasks;
    using S3Lite;
    using Test.Shared;
    using Touchstone.Cli;

    /// <summary>
    /// Console host for the shared Touchstone S3Lite test suites.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Run the shared S3Lite Touchstone suites.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Exit code returned by the Touchstone console runner.</returns>
        private static async Task<int> Main(string[] args)
        {
            if (ContainsHelpArgument(args))
            {
                PrintUsage();
                return 0;
            }

            S3LiteTestSettings settings = S3LiteTestSettings.FromEnvironment();
            string? resultsPath = null;

            if (!ParseArguments(args, settings, out resultsPath))
            {
                PrintUsage();
                return 1;
            }

            if (settings.Verbose)
            {
                settings.Logger = LogMessage;
            }

            S3LiteTestSettings.Configure(settings);

            PrintConfiguration(settings, resultsPath);

            return await ConsoleRunner.RunAsync(
                S3LiteTestSuites.All,
                resultsPath: resultsPath).ConfigureAwait(false);
        }

        private static bool ParseArguments(string[] args, S3LiteTestSettings settings, out string? resultsPath)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            resultsPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                string argument = args[i];
                string normalized = argument.ToLowerInvariant();

                if (normalized == "-e" || normalized == "--endpoint")
                {
                    if (!TryReadValue(args, ref i, out string endpoint)) return false;
                    settings.Endpoint = endpoint;
                }
                else if (normalized == "-p" || normalized == "--port")
                {
                    if (!TryReadValue(args, ref i, out string portValue)) return false;
                    if (!Int32.TryParse(portValue, out int port)) return false;
                    settings.Port = port;
                }
                else if (normalized == "-r" || normalized == "--region")
                {
                    if (!TryReadValue(args, ref i, out string region)) return false;
                    settings.Region = region;
                }
                else if (normalized == "-a" || normalized == "--access-key")
                {
                    if (!TryReadValue(args, ref i, out string accessKey)) return false;
                    settings.AccessKey = accessKey;
                }
                else if (normalized == "-s" || normalized == "--secret-key")
                {
                    if (!TryReadValue(args, ref i, out string secretKey)) return false;
                    settings.SecretKey = secretKey;
                }
                else if (normalized == "-b" || normalized == "--bucket")
                {
                    if (!TryReadValue(args, ref i, out string bucket)) return false;
                    settings.Bucket = bucket;
                }
                else if (normalized == "--http")
                {
                    settings.Protocol = ProtocolEnum.Http;
                }
                else if (normalized == "--https")
                {
                    settings.Protocol = ProtocolEnum.Https;
                }
                else if (normalized == "--path-style")
                {
                    settings.RequestStyle = RequestStyleEnum.PathStyle;
                }
                else if (normalized == "--virtual-hosted")
                {
                    settings.RequestStyle = RequestStyleEnum.VirtualHostedStyle;
                }
                else if (normalized == "-v" || normalized == "--verbose")
                {
                    settings.Verbose = true;
                }
                else if (normalized == "--skip-cleanup")
                {
                    settings.SkipCleanup = true;
                }
                else if (normalized == "--skip-write-tests")
                {
                    settings.SkipWriteTests = true;
                }
                else if (normalized == "--results")
                {
                    if (!TryReadValue(args, ref i, out string parsedResultsPath)) return false;
                    resultsPath = parsedResultsPath;
                }
                else
                {
                    Console.WriteLine("Unknown argument: " + argument);
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsHelpArgument(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            foreach (string argument in args)
            {
                string normalized = argument.ToLowerInvariant();
                if (normalized == "-h" || normalized == "--help" || normalized == "/?")
                    return true;
            }

            return false;
        }

        private static bool TryReadValue(string[] args, ref int index, out string value)
        {
            if (index + 1 >= args.Length)
            {
                value = String.Empty;
                return false;
            }

            index++;
            value = args[index];
            return true;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: dotnet run --project src/Test.Automated -- [options]");
            Console.WriteLine("");
            Console.WriteLine("Connection Options:");
            Console.WriteLine("  -e, --endpoint <host>      S3 endpoint hostname (default: amazonaws.com)");
            Console.WriteLine("  -p, --port <port>          Port number (default: 443)");
            Console.WriteLine("  -r, --region <region>      AWS region (default: us-west-1)");
            Console.WriteLine("  --http                     Use HTTP");
            Console.WriteLine("  --https                    Use HTTPS (default)");
            Console.WriteLine("  --path-style               Use path-style requests");
            Console.WriteLine("  --virtual-hosted           Use virtual-hosted style requests (default)");
            Console.WriteLine("");
            Console.WriteLine("Authentication:");
            Console.WriteLine("  -a, --access-key <key>     AWS or S3-compatible access key");
            Console.WriteLine("  -s, --secret-key <key>     AWS or S3-compatible secret key");
            Console.WriteLine("");
            Console.WriteLine("Bucket and Test Options:");
            Console.WriteLine("  -b, --bucket <name>        Bucket used for bucket/object suites");
            Console.WriteLine("  -v, --verbose              Enable request logging");
            Console.WriteLine("  --skip-cleanup             Skip deletion of test artifacts");
            Console.WriteLine("  --skip-write-tests         Skip write, overwrite, and cleanup cases");
            Console.WriteLine("  --results <path>           Export Touchstone JSON results");
            Console.WriteLine("  -h, --help                 Show this help message");
            Console.WriteLine("");
            Console.WriteLine("Environment variables:");
            Console.WriteLine("  S3LITE_TEST_ENDPOINT, S3LITE_TEST_PORT, S3LITE_TEST_REGION");
            Console.WriteLine("  S3LITE_TEST_ACCESS_KEY, S3LITE_TEST_SECRET_KEY, S3LITE_TEST_BUCKET");
            Console.WriteLine("  S3LITE_TEST_PROTOCOL, S3LITE_TEST_REQUEST_STYLE");
            Console.WriteLine("  S3LITE_TEST_VERBOSE, S3LITE_TEST_SKIP_CLEANUP, S3LITE_TEST_SKIP_WRITE_TESTS");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run --project src/Test.Automated -- -b my-bucket -a AKIAEXAMPLE -s secretkey123 -r us-east-1");
            Console.WriteLine("  dotnet run --project src/Test.Automated -- -b public-bucket --skip-write-tests");
            Console.WriteLine("  dotnet run --project src/Test.Automated -- -b test-bucket -e localhost -p 9000 --http --path-style -a minioadmin -s minioadmin");
        }

        private static void PrintConfiguration(S3LiteTestSettings settings, string? resultsPath)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            Console.WriteLine("Configuration:");
            Console.WriteLine("  Endpoint:      " + settings.Endpoint + ":" + settings.Port.ToString());
            Console.WriteLine("  Region:        " + settings.Region);
            Console.WriteLine("  Bucket:        " + (String.IsNullOrWhiteSpace(settings.Bucket) ? "(not set)" : settings.Bucket));
            Console.WriteLine("  Protocol:      " + settings.Protocol.ToString());
            Console.WriteLine("  Request Style: " + settings.RequestStyle.ToString());
            Console.WriteLine("  Credentials:   " + (settings.HasCredentials ? "Configured" : "Anonymous"));
            Console.WriteLine("  Verbose:       " + settings.Verbose.ToString());
            Console.WriteLine("  Skip Cleanup:  " + settings.SkipCleanup.ToString());
            Console.WriteLine("  Skip Writes:   " + settings.SkipWriteTests.ToString());
            Console.WriteLine("  Results Path:  " + (String.IsNullOrWhiteSpace(resultsPath) ? "(none)" : resultsPath));
            Console.WriteLine("");
        }

        private static void LogMessage(string message)
        {
            Console.WriteLine("[LOG] " + message);
        }
    }
}
