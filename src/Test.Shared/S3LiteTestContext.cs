namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using S3Lite;

    /// <summary>
    /// Shared state and helper methods used by the S3Lite Touchstone suites.
    /// </summary>
    internal sealed class S3LiteTestContext
    {
        private readonly S3LiteTestSettings _Settings;
        private readonly string _RunId;
        private readonly string _ObjectPrefix;
        private readonly string _SmallObjectKey;
        private readonly string _LargeObjectKey;
        private readonly string _EmptyObjectKey;
        private readonly string _SpecialObjectKey;
        private readonly string _MissingObjectKey;
        private readonly string _SmallContent;
        private readonly string _OverwriteContent;
        private readonly string _SpecialContent;
        private readonly IReadOnlyList<string> _CleanupKeys;
        private byte[]? _LargeContent = null;

        internal S3LiteTestContext(S3LiteTestSettings settings)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _RunId = Guid.NewGuid().ToString("N");
            _ObjectPrefix = "test-automated/" + _RunId + "/";
            _SmallObjectKey = _ObjectPrefix + "small-object.txt";
            _LargeObjectKey = _ObjectPrefix + "large-object.bin";
            _EmptyObjectKey = _ObjectPrefix + "empty-object.txt";
            _SpecialObjectKey = _ObjectPrefix + "file with spaces.txt";
            _MissingObjectKey = _ObjectPrefix + "missing-" + Guid.NewGuid().ToString("N") + ".txt";
            _SmallContent = "Hello, S3Lite! This is a test object.";
            _OverwriteContent = "This content has been overwritten.";
            _SpecialContent = "Special character test";
            _CleanupKeys = new List<string>
            {
                _SmallObjectKey,
                _LargeObjectKey,
                _EmptyObjectKey,
                _SpecialObjectKey
            };
        }

        internal string BucketName
        {
            get
            {
                return _Settings.Bucket ?? String.Empty;
            }
        }

        internal string ObjectPrefix
        {
            get
            {
                return _ObjectPrefix;
            }
        }

        internal string SmallObjectKey
        {
            get
            {
                return _SmallObjectKey;
            }
        }

        internal string LargeObjectKey
        {
            get
            {
                return _LargeObjectKey;
            }
        }

        internal string EmptyObjectKey
        {
            get
            {
                return _EmptyObjectKey;
            }
        }

        internal string SpecialObjectKey
        {
            get
            {
                return _SpecialObjectKey;
            }
        }

        internal string MissingObjectKey
        {
            get
            {
                return _MissingObjectKey;
            }
        }

        internal string SmallContent
        {
            get
            {
                return _SmallContent;
            }
        }

        internal string OverwriteContent
        {
            get
            {
                return _OverwriteContent;
            }
        }

        internal string SpecialContent
        {
            get
            {
                return _SpecialContent;
            }
        }

        internal IReadOnlyList<string> CleanupKeys
        {
            get
            {
                return _CleanupKeys;
            }
        }

        internal byte[] LargeContent
        {
            get
            {
                if (_LargeContent == null)
                {
                    byte[] content = new byte[1024 * 1024];
                    Random random = new Random(42);
                    random.NextBytes(content);
                    _LargeContent = content;
                }

                return _LargeContent;
            }
        }

        internal S3Client CreateClient()
        {
            return CreateClient(null);
        }

        internal S3Client CreateClient(HttpClient? httpClient)
        {
            S3Client client = httpClient == null ? new S3Client() : new S3Client(httpClient);

            client
                .WithHostname(_Settings.Endpoint)
                .WithPort(_Settings.Port)
                .WithRegion(_Settings.Region)
                .WithProtocol(_Settings.Protocol)
                .WithRequestStyle(_Settings.RequestStyle);

            if (_Settings.HasCredentials)
            {
                client
                    .WithAccessKey(_Settings.AccessKey)
                    .WithSecretKey(_Settings.SecretKey);
            }

            if (_Settings.Verbose && _Settings.Logger != null)
            {
                client.WithLogger(_Settings.Logger);
            }

            return client;
        }

        internal async Task CleanupObjectsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            S3Client client = CreateClient();
            List<string> failures = new List<string>();

            foreach (string key in _CleanupKeys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await client.Object.DeleteAsync(BucketName, key, token: cancellationToken).ConfigureAwait(false);
                }
                catch (WebException webException) when (IsNotFound(webException))
                {
                }
                catch (Exception exception)
                {
                    failures.Add(key + ": " + exception.Message);
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Cleanup failed for " + failures.Count.ToString() + " object(s): " + String.Join("; ", failures));
            }
        }

        internal byte[] GetUtf8Bytes(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return Encoding.UTF8.GetBytes(value);
        }

        private static bool IsNotFound(WebException exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            object? statusCode = exception.Data["StatusCode"];
            if (statusCode is int typedStatusCode)
            {
                return typedStatusCode == 404;
            }

            return false;
        }
    }
}
