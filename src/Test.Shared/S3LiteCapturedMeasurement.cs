namespace Test.Shared
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// One metric measurement captured from the S3Lite meter during a telemetry test.
    /// </summary>
    internal sealed class S3LiteCapturedMeasurement
    {
        /// <summary>
        /// Instrument name.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Instrument unit.
        /// </summary>
        internal string? Unit { get; }

        /// <summary>
        /// Recorded value.
        /// </summary>
        internal double Value { get; }

        /// <summary>
        /// Recorded tags.
        /// </summary>
        internal IReadOnlyDictionary<string, object?> Tags { get; }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="name">Instrument name.</param>
        /// <param name="unit">Instrument unit.</param>
        /// <param name="value">Recorded value.</param>
        /// <param name="tags">Recorded tags.</param>
        internal S3LiteCapturedMeasurement(string name, string? unit, double value, IReadOnlyDictionary<string, object?> tags)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Unit = unit;
            Value = value;
            Tags = tags ?? throw new ArgumentNullException(nameof(tags));
        }

        /// <summary>
        /// Return the string form of a tag, or null when absent.
        /// </summary>
        /// <param name="key">Tag key.</param>
        /// <returns>Tag value as a string, or null.</returns>
        internal string? Tag(string key)
        {
            object? value;
            if (Tags.TryGetValue(key, out value) && value != null) return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            return null;
        }
    }
}
