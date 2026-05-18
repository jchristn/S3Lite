namespace Test.Shared
{
    using System;
    using S3Lite;

    /// <summary>
    /// Shared configuration for S3Lite Touchstone test suites.
    /// Values may be supplied through environment variables and then overridden by the console runner.
    /// </summary>
    public sealed class S3LiteTestSettings
    {
        private const string _EndpointEnvironmentVariable = "S3LITE_TEST_ENDPOINT";
        private const string _PortEnvironmentVariable = "S3LITE_TEST_PORT";
        private const string _RegionEnvironmentVariable = "S3LITE_TEST_REGION";
        private const string _AccessKeyEnvironmentVariable = "S3LITE_TEST_ACCESS_KEY";
        private const string _SecretKeyEnvironmentVariable = "S3LITE_TEST_SECRET_KEY";
        private const string _BucketEnvironmentVariable = "S3LITE_TEST_BUCKET";
        private const string _ProtocolEnvironmentVariable = "S3LITE_TEST_PROTOCOL";
        private const string _RequestStyleEnvironmentVariable = "S3LITE_TEST_REQUEST_STYLE";
        private const string _VerboseEnvironmentVariable = "S3LITE_TEST_VERBOSE";
        private const string _SkipCleanupEnvironmentVariable = "S3LITE_TEST_SKIP_CLEANUP";
        private const string _SkipWriteTestsEnvironmentVariable = "S3LITE_TEST_SKIP_WRITE_TESTS";

        private static S3LiteTestSettings _Current = FromEnvironment();

        private string _Endpoint = "amazonaws.com";
        private int _Port = 443;
        private string _Region = "us-west-1";
        private string? _AccessKey = null;
        private string? _SecretKey = null;
        private string? _Bucket = null;
        private ProtocolEnum _Protocol = ProtocolEnum.Https;
        private RequestStyleEnum _RequestStyle = RequestStyleEnum.VirtualHostedStyle;
        private bool _Verbose = false;
        private bool _SkipCleanup = false;
        private bool _SkipWriteTests = false;
        private Action<string>? _Logger = null;

        /// <summary>
        /// Active settings instance used by shared test descriptors.
        /// </summary>
        public static S3LiteTestSettings Current
        {
            get
            {
                return _Current;
            }
        }

        /// <summary>
        /// Endpoint hostname.
        /// Default value is <c>amazonaws.com</c>.
        /// </summary>
        public string Endpoint
        {
            get
            {
                return _Endpoint;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(Endpoint));
                _Endpoint = value;
            }
        }

        /// <summary>
        /// Endpoint port.
        /// Default value is <c>443</c>.
        /// Minimum value is <c>0</c>.
        /// Maximum value is <c>65535</c>.
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
            set
            {
                if (value < 0 || value > 65535) throw new ArgumentOutOfRangeException(nameof(Port));
                _Port = value;
            }
        }

        /// <summary>
        /// AWS region string.
        /// Default value is <c>us-west-1</c>.
        /// </summary>
        public string Region
        {
            get
            {
                return _Region;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(Region));
                _Region = value;
            }
        }

        /// <summary>
        /// Access key used for authenticated requests.
        /// Null or empty uses anonymous access mode.
        /// </summary>
        public string? AccessKey
        {
            get
            {
                return _AccessKey;
            }
            set
            {
                _AccessKey = String.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        /// <summary>
        /// Secret key used for authenticated requests.
        /// Null or empty uses anonymous access mode.
        /// </summary>
        public string? SecretKey
        {
            get
            {
                return _SecretKey;
            }
            set
            {
                _SecretKey = String.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        /// <summary>
        /// Bucket used for bucket and object tests.
        /// When null or empty, bucket and object suites are skipped.
        /// </summary>
        public string? Bucket
        {
            get
            {
                return _Bucket;
            }
            set
            {
                _Bucket = String.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        /// <summary>
        /// Protocol used for requests.
        /// Default value is <see cref="ProtocolEnum.Https"/>.
        /// </summary>
        public ProtocolEnum Protocol
        {
            get
            {
                return _Protocol;
            }
            set
            {
                if (!Enum.IsDefined(typeof(ProtocolEnum), value)) throw new ArgumentOutOfRangeException(nameof(Protocol));
                _Protocol = value;
            }
        }

        /// <summary>
        /// Request style used for URL construction.
        /// Default value is <see cref="RequestStyleEnum.VirtualHostedStyle"/>.
        /// </summary>
        public RequestStyleEnum RequestStyle
        {
            get
            {
                return _RequestStyle;
            }
            set
            {
                if (!Enum.IsDefined(typeof(RequestStyleEnum), value)) throw new ArgumentOutOfRangeException(nameof(RequestStyle));
                _RequestStyle = value;
            }
        }

        /// <summary>
        /// Enables verbose request logging when paired with <see cref="Logger"/>.
        /// Default value is <c>false</c>.
        /// </summary>
        public bool Verbose
        {
            get
            {
                return _Verbose;
            }
            set
            {
                _Verbose = value;
            }
        }

        /// <summary>
        /// Skips the cleanup test when true.
        /// Default value is <c>false</c>.
        /// </summary>
        public bool SkipCleanup
        {
            get
            {
                return _SkipCleanup;
            }
            set
            {
                _SkipCleanup = value;
            }
        }

        /// <summary>
        /// Skips write, overwrite, and delete tests when true.
        /// Default value is <c>false</c>.
        /// </summary>
        public bool SkipWriteTests
        {
            get
            {
                return _SkipWriteTests;
            }
            set
            {
                _SkipWriteTests = value;
            }
        }

        /// <summary>
        /// Optional logger delegate for request-level tracing.
        /// When null, request logging is disabled.
        /// </summary>
        public Action<string>? Logger
        {
            get
            {
                return _Logger;
            }
            set
            {
                _Logger = value;
            }
        }

        /// <summary>
        /// Indicates whether both access key and secret key are configured.
        /// </summary>
        public bool HasCredentials
        {
            get
            {
                return !String.IsNullOrWhiteSpace(_AccessKey) && !String.IsNullOrWhiteSpace(_SecretKey);
            }
        }

        internal bool HasBucket
        {
            get
            {
                return !String.IsNullOrWhiteSpace(_Bucket);
            }
        }

        /// <summary>
        /// Replace the active settings instance used by shared test descriptors.
        /// </summary>
        /// <param name="settings">Settings to use for subsequent suite creation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="settings"/> is null.</exception>
        public static void Configure(S3LiteTestSettings settings)
        {
            _Current = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Build a settings instance from environment variables.
        /// Missing values fall back to built-in defaults.
        /// </summary>
        /// <returns>Configured test settings.</returns>
        public static S3LiteTestSettings FromEnvironment()
        {
            S3LiteTestSettings settings = new S3LiteTestSettings();

            string? endpoint = Environment.GetEnvironmentVariable(_EndpointEnvironmentVariable);
            if (!String.IsNullOrWhiteSpace(endpoint)) settings.Endpoint = endpoint;

            string? port = Environment.GetEnvironmentVariable(_PortEnvironmentVariable);
            if (!String.IsNullOrWhiteSpace(port) && Int32.TryParse(port, out int parsedPort)) settings.Port = parsedPort;

            string? region = Environment.GetEnvironmentVariable(_RegionEnvironmentVariable);
            if (!String.IsNullOrWhiteSpace(region)) settings.Region = region;

            settings.AccessKey = Environment.GetEnvironmentVariable(_AccessKeyEnvironmentVariable);
            settings.SecretKey = Environment.GetEnvironmentVariable(_SecretKeyEnvironmentVariable);
            settings.Bucket = Environment.GetEnvironmentVariable(_BucketEnvironmentVariable);

            string? protocol = Environment.GetEnvironmentVariable(_ProtocolEnvironmentVariable);
            if (!String.IsNullOrWhiteSpace(protocol)) settings.Protocol = ParseProtocol(protocol);

            string? requestStyle = Environment.GetEnvironmentVariable(_RequestStyleEnvironmentVariable);
            if (!String.IsNullOrWhiteSpace(requestStyle)) settings.RequestStyle = ParseRequestStyle(requestStyle);

            string? verbose = Environment.GetEnvironmentVariable(_VerboseEnvironmentVariable);
            if (!String.IsNullOrWhiteSpace(verbose)) settings.Verbose = ParseBoolean(verbose);

            string? skipCleanup = Environment.GetEnvironmentVariable(_SkipCleanupEnvironmentVariable);
            if (!String.IsNullOrWhiteSpace(skipCleanup)) settings.SkipCleanup = ParseBoolean(skipCleanup);

            string? skipWriteTests = Environment.GetEnvironmentVariable(_SkipWriteTestsEnvironmentVariable);
            if (!String.IsNullOrWhiteSpace(skipWriteTests)) settings.SkipWriteTests = ParseBoolean(skipWriteTests);

            return settings;
        }

        private static bool ParseBoolean(string value)
        {
            if (String.Equals(value, "1", StringComparison.OrdinalIgnoreCase)) return true;
            if (String.Equals(value, "0", StringComparison.OrdinalIgnoreCase)) return false;
            if (Boolean.TryParse(value, out bool parsed)) return parsed;

            throw new ArgumentException("Unable to parse boolean value '" + value + "'.", nameof(value));
        }

        private static ProtocolEnum ParseProtocol(string value)
        {
            if (String.Equals(value, "http", StringComparison.OrdinalIgnoreCase)) return ProtocolEnum.Http;
            if (String.Equals(value, "https", StringComparison.OrdinalIgnoreCase)) return ProtocolEnum.Https;

            throw new ArgumentException("Unknown protocol value '" + value + "'.", nameof(value));
        }

        private static RequestStyleEnum ParseRequestStyle(string value)
        {
            if (String.Equals(value, "path", StringComparison.OrdinalIgnoreCase)) return RequestStyleEnum.PathStyle;
            if (String.Equals(value, "path-style", StringComparison.OrdinalIgnoreCase)) return RequestStyleEnum.PathStyle;
            if (String.Equals(value, "pathstyle", StringComparison.OrdinalIgnoreCase)) return RequestStyleEnum.PathStyle;
            if (String.Equals(value, "virtual", StringComparison.OrdinalIgnoreCase)) return RequestStyleEnum.VirtualHostedStyle;
            if (String.Equals(value, "virtual-hosted", StringComparison.OrdinalIgnoreCase)) return RequestStyleEnum.VirtualHostedStyle;
            if (String.Equals(value, "virtualhostedstyle", StringComparison.OrdinalIgnoreCase)) return RequestStyleEnum.VirtualHostedStyle;
            if (String.Equals(value, "virtual-hosted-style", StringComparison.OrdinalIgnoreCase)) return RequestStyleEnum.VirtualHostedStyle;

            throw new ArgumentException("Unknown request style value '" + value + "'.", nameof(value));
        }
    }
}
