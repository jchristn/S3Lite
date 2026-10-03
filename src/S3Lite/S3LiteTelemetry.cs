namespace S3Lite
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Reflection;

    /// <summary>
    /// Process-wide S3Lite meter, activity source, and instruments.  Thread-safe.
    /// </summary>
    internal static class S3LiteTelemetry
    {
        #region Internal-Members

        internal static readonly string Version = GetVersion();

        internal static readonly ActivitySource Source = new ActivitySource(S3LiteTelemetryNames.ActivitySourceName, Version);

        internal static readonly Meter Meter = new Meter(S3LiteTelemetryNames.MeterName, Version);

        internal static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>(
            S3LiteTelemetryNames.OperationDuration, "s", "Duration of S3 operations.");

        internal static readonly Counter<long> Operations = Meter.CreateCounter<long>(
            S3LiteTelemetryNames.Operations, "{operation}", "Completed S3 operations by outcome.");

        internal static readonly UpDownCounter<long> ActiveOperations = Meter.CreateUpDownCounter<long>(
            S3LiteTelemetryNames.ActiveOperations, "{operation}", "S3 operations currently in flight.");

        internal static readonly Histogram<double> StageDuration = Meter.CreateHistogram<double>(
            S3LiteTelemetryNames.StageDuration, "s", "Duration of S3 operation sub-stages.");

        internal static readonly Counter<long> Stages = Meter.CreateCounter<long>(
            S3LiteTelemetryNames.Stages, "{stage}", "Completed S3 operation sub-stages by outcome.");

        internal static readonly Histogram<long> RequestBodySize = Meter.CreateHistogram<long>(
            S3LiteTelemetryNames.RequestBodySize, "By", "Request body size sent per S3 operation.");

        internal static readonly Histogram<long> ResponseBodySize = Meter.CreateHistogram<long>(
            S3LiteTelemetryNames.ResponseBodySize, "By", "Response body size received per S3 operation.");

        internal static readonly Counter<long> ObjectsListed = Meter.CreateCounter<long>(
            S3LiteTelemetryNames.ObjectsListed, "{object}", "Objects returned by ListObjectsV2 pages.");

        internal static readonly Counter<long> BatchDeleteObjects = Meter.CreateCounter<long>(
            S3LiteTelemetryNames.BatchDeleteObjects, "{object}", "Per-object results of DeleteObjects calls.");

        internal static readonly Histogram<long> BatchDeleteSize = Meter.CreateHistogram<long>(
            S3LiteTelemetryNames.BatchDeleteSize, "{object}", "Objects requested per DeleteObjects call.");

        internal static readonly ObservableGauge<int> BuildInfo = Meter.CreateObservableGauge<int>(
            S3LiteTelemetryNames.BuildInfo,
            () => new Measurement<int>(1, new KeyValuePair<string, object>(S3LiteTelemetryNames.AttributeVersion, Version)),
            null,
            "S3Lite build information (always 1).");

        internal static bool MetricsObserved
        {
            get
            {
                return OperationDuration.Enabled
                    || Operations.Enabled
                    || ActiveOperations.Enabled
                    || StageDuration.Enabled
                    || Stages.Enabled
                    || RequestBodySize.Enabled
                    || ResponseBodySize.Enabled
                    || ObjectsListed.Enabled
                    || BatchDeleteObjects.Enabled
                    || BatchDeleteSize.Enabled;
            }
        }

        #endregion

        #region Internal-Methods

        /// <summary>
        /// Record the objects returned by one ListObjectsV2 page.
        /// </summary>
        /// <param name="scope">Operation scope, may be null.</param>
        /// <param name="count">Objects returned.</param>
        /// <param name="truncated">True when more pages remain.</param>
        internal static void RecordListed(OperationScope scope, int count, bool truncated)
        {
            if (scope == null) return;

            try
            {
                scope.SetTag(S3LiteTelemetryNames.AttributeListCount, count);
                scope.SetTag(S3LiteTelemetryNames.AttributeListTruncated, truncated);
                if (scope.MetricsEnabled) ObjectsListed.Add(count);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Record the per-object results of one DeleteObjects call.
        /// </summary>
        /// <param name="scope">Operation scope, may be null.</param>
        /// <param name="requested">Objects requested.</param>
        /// <param name="result">Delete result, may be null.</param>
        internal static void RecordBatchDelete(OperationScope scope, int requested, ApiObjects.DeleteResult result)
        {
            if (scope == null) return;

            try
            {
                int deleted = result?.Deleted != null ? result.Deleted.Count : 0;
                int failed = result?.Errors != null ? result.Errors.Count : 0;

                scope.SetTag(S3LiteTelemetryNames.AttributeDeleteRequested, requested);
                scope.SetTag(S3LiteTelemetryNames.AttributeDeleteDeleted, deleted);
                scope.SetTag(S3LiteTelemetryNames.AttributeDeleteFailed, failed);

                if (!scope.MetricsEnabled) return;

                BatchDeleteSize.Record(requested);

                if (deleted > 0)
                {
                    BatchDeleteObjects.Add(deleted, new KeyValuePair<string, object>(S3LiteTelemetryNames.AttributeOutcome, S3LiteTelemetryNames.OutcomeDeleted));
                }

                if (failed > 0)
                {
                    foreach (ApiObjects.DeleteError error in result.Errors)
                    {
                        string errorType = NormalizeErrorCode(error?.Code) ?? S3LiteTelemetryNames.ErrorTypeOther;
                        BatchDeleteObjects.Add(
                            1,
                            new KeyValuePair<string, object>(S3LiteTelemetryNames.AttributeOutcome, S3LiteTelemetryNames.OutcomeFailed),
                            new KeyValuePair<string, object>(S3LiteTelemetryNames.AttributeErrorType, errorType));
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Map an S3 error code string to a bounded error.type label value.
        /// </summary>
        /// <param name="code">S3 error code, may be null.</param>
        /// <returns>The recognized code, <see cref="S3LiteTelemetryNames.ErrorTypeOther"/>, or null when no code was supplied.</returns>
        internal static string NormalizeErrorCode(string code)
        {
            if (String.IsNullOrEmpty(code)) return null;

            ApiObjects.ErrorCode parsed;
            if (Enum.TryParse(code, false, out parsed) && parsed.ToString() == code) return code;
            return S3LiteTelemetryNames.ErrorTypeOther;
        }

        #endregion

        #region Private-Methods

        private static string GetVersion()
        {
            try
            {
                Version v = typeof(S3LiteTelemetry).Assembly.GetName().Version;
                return v != null ? v.ToString(3) : "0.0.0";
            }
            catch (Exception)
            {
                return "0.0.0";
            }
        }

        #endregion
    }
}
