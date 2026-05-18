namespace Test.Shared
{
    using System;
    using System.Collections.Generic;

    internal sealed class S3LiteSyntheticBucket
    {
        private readonly Dictionary<string, S3LiteSyntheticObject> _Objects = new Dictionary<string, S3LiteSyntheticObject>(StringComparer.Ordinal);

        internal S3LiteSyntheticBucket(string name, string region)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (String.IsNullOrWhiteSpace(region)) throw new ArgumentNullException(nameof(region));

            Name = name;
            Region = region;
            CreatedUtc = DateTime.UtcNow;
        }

        internal DateTime CreatedUtc { get; }

        internal string Name { get; }

        internal IDictionary<string, S3LiteSyntheticObject> Objects
        {
            get
            {
                return _Objects;
            }
        }

        internal string Region { get; }
    }
}
