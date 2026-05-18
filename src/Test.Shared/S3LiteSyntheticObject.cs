namespace Test.Shared
{
    using System;
    using System.Security.Cryptography;

    internal sealed class S3LiteSyntheticObject
    {
        internal S3LiteSyntheticObject(string key, byte[]? data, string? contentType)
        {
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            byte[] objectData = data ?? Array.Empty<byte>();

            Key = key;
            Data = (byte[])objectData.Clone();
            ContentType = String.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
            LastModifiedUtc = DateTime.UtcNow;
            ETag = ComputeETag(Data);
        }

        internal string ContentType { get; private set; }

        internal byte[] Data { get; private set; }

        internal string ETag { get; private set; }

        internal string Key { get; }

        internal DateTime LastModifiedUtc { get; private set; }

        internal void Update(byte[]? data, string? contentType)
        {
            byte[] objectData = data ?? Array.Empty<byte>();

            Data = (byte[])objectData.Clone();
            ContentType = String.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
            LastModifiedUtc = DateTime.UtcNow;
            ETag = ComputeETag(Data);
        }

        private static string ComputeETag(byte[] data)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(data ?? Array.Empty<byte>());
                return BitConverter.ToString(hash).Replace("-", String.Empty).ToLowerInvariant();
            }
        }
    }
}
