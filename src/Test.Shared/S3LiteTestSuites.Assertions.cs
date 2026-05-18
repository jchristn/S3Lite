namespace Test.Shared
{
    using System;
    using System.Collections;
    using System.Net;
    using System.Threading.Tasks;

    public static partial class S3LiteTestSuites
    {
        private static void AssertByteArraysEqual(byte[] expected, byte[] actual, string label)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));

            if (expected.Length != actual.Length)
            {
                throw new InvalidOperationException(
                    "Expected " + label + " length " + expected.Length.ToString() + " but found " + actual.Length.ToString() + ".");
            }

            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] != actual[i])
                {
                    throw new InvalidOperationException(
                        "Expected " + label + " byte " + i.ToString() + " to be " + expected[i].ToString() + " but found " + actual[i].ToString() + ".");
                }
            }
        }

        private static void AssertEqual(bool expected, bool actual, string label)
        {
            if (expected != actual)
            {
                throw new InvalidOperationException(
                    "Expected " + label + " to be " + expected.ToString() + " but was " + actual.ToString() + ".");
            }
        }

        private static void AssertEqual(int expected, int actual, string label)
        {
            if (expected != actual)
            {
                throw new InvalidOperationException(
                    "Expected " + label + " to be " + expected.ToString() + " but was " + actual.ToString() + ".");
            }
        }

        private static void AssertEqual(long expected, long actual, string label)
        {
            if (expected != actual)
            {
                throw new InvalidOperationException(
                    "Expected " + label + " to be " + expected.ToString() + " but was " + actual.ToString() + ".");
            }
        }

        private static void AssertEqual(string? expected, string? actual, string label)
        {
            if (!String.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Expected " + label + " to be '" + expected + "' but was '" + actual + "'.");
            }
        }

        private static void AssertFalse(bool condition, string message)
        {
            if (condition) throw new InvalidOperationException(message);
        }

        private static void AssertNotNull(object? value, string label)
        {
            if (value == null)
            {
                throw new InvalidOperationException("Expected " + label + " to be non-null.");
            }
        }

        private static void AssertNull(object? value, string label)
        {
            if (value != null)
            {
                throw new InvalidOperationException("Expected " + label + " to be null.");
            }
        }

        private static void AssertSame(object expected, object actual, string label)
        {
            if (!Object.ReferenceEquals(expected, actual))
            {
                throw new InvalidOperationException("Expected " + label + " to reference the supplied instance.");
            }
        }

        private static void AssertThrows<TException>(Action action, Action<TException>? validator = null) where TException : Exception
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            try
            {
                action();
                throw new InvalidOperationException("Expected exception of type " + typeof(TException).Name + " was not thrown.");
            }
            catch (TException exception)
            {
                validator?.Invoke(exception);
            }
        }

        private static async Task AssertThrowsAsync<TException>(Func<Task> action, Action<TException>? validator = null) where TException : Exception
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            try
            {
                await action().ConfigureAwait(false);
                throw new InvalidOperationException("Expected exception of type " + typeof(TException).Name + " was not thrown.");
            }
            catch (TException exception)
            {
                validator?.Invoke(exception);
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void AssertWebException(WebException exception, int expectedStatusCode, string expectedErrorCode, string? expectedKey = null)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            object? statusCode = exception.Data["StatusCode"];
            if (statusCode is not int typedStatusCode)
            {
                throw new InvalidOperationException("WebException did not include an integer StatusCode.");
            }

            AssertEqual(expectedStatusCode, typedStatusCode, "web exception status code");

            if (!String.IsNullOrWhiteSpace(expectedErrorCode))
            {
                object? errorCode = exception.Data["ErrorCode"];
                AssertEqual(expectedErrorCode, errorCode?.ToString(), "web exception error code");
            }

            if (!String.IsNullOrWhiteSpace(expectedKey))
            {
                object? key = exception.Data["Key"];
                AssertEqual(expectedKey, key?.ToString(), "web exception key");
            }

            object? url = exception.Data["URL"];
            AssertTrue(url != null && !String.IsNullOrWhiteSpace(url.ToString()), "WebException should include a URL.");
        }

        private static byte[] BytesFromHexString(string hex)
        {
            if (String.IsNullOrWhiteSpace(hex)) throw new ArgumentNullException(nameof(hex));
            if ((hex.Length % 2) != 0) throw new ArgumentException("Hex string length must be even.", nameof(hex));

            byte[] bytes = new byte[hex.Length / 2];

            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        private static string ModeId(RequestTransportMode mode)
        {
            return mode == RequestTransportMode.Internal ? "Internal" : "External";
        }

        private static string ModeLabel(RequestTransportMode mode)
        {
            return mode == RequestTransportMode.Internal ? "internally-owned HttpClient" : "caller-supplied HttpClient";
        }
    }
}
