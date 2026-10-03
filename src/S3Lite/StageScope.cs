namespace S3Lite
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Telemetry scope for one sub-stage of an S3 operation: a "stage:&lt;name&gt;" child span plus per-stage
    /// duration and outcome metrics.  All members are best-effort and never throw.  Not thread-safe.
    /// </summary>
    internal sealed class StageScope : IDisposable
    {
        #region Internal-Members

        internal Activity Activity
        {
            get
            {
                return _Activity;
            }
        }

        #endregion

        #region Private-Members

        private readonly OperationScope _Parent;
        private readonly string _Stage;
        private readonly long _StartTimestamp;
        private Activity _Activity = null;
        private string _Outcome = S3LiteTelemetryNames.OutcomeSuccess;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        internal StageScope(OperationScope parent, string stage)
        {
            _Parent = parent;
            _Stage = stage;
            _StartTimestamp = Stopwatch.GetTimestamp();

            if (parent.TracesEnabled)
            {
                _Activity = S3LiteTelemetry.Source.StartActivity(S3LiteTelemetryNames.StageSpanPrefix + stage, ActivityKind.Internal);
                if (_Activity != null && _Activity.IsAllDataRequested)
                {
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeOperation, parent.Operation);
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeStage, stage);
                }
            }
        }

        #endregion

        #region Internal-Methods

        internal void Fail(Exception e)
        {
            try
            {
                _Outcome = e is OperationCanceledException ? S3LiteTelemetryNames.OutcomeCancelled : S3LiteTelemetryNames.OutcomeError;
                if (_Activity != null)
                {
                    _Activity.SetStatus(ActivityStatusCode.Error, e.GetType().FullName);
                    if (_Activity.IsAllDataRequested) OperationScope.RecordException(_Activity, e, _Parent.IncludeObjectKeys);
                }
            }
            catch (Exception)
            {
            }
        }

        internal void Fail(string description)
        {
            try
            {
                _Outcome = S3LiteTelemetryNames.OutcomeError;
                if (_Activity != null) _Activity.SetStatus(ActivityStatusCode.Error, description);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Record the end of the stage.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            try
            {
                if (_Parent.MetricsEnabled)
                {
                    double seconds = (Stopwatch.GetTimestamp() - _StartTimestamp) / (double)Stopwatch.Frequency;

                    TagList tags = new TagList();
                    tags.Add(S3LiteTelemetryNames.AttributeOperation, _Parent.Operation);
                    tags.Add(S3LiteTelemetryNames.AttributeStage, _Stage);
                    tags.Add(S3LiteTelemetryNames.AttributeOutcome, _Outcome);
                    S3LiteTelemetry.StageDuration.Record(seconds, tags);
                    S3LiteTelemetry.Stages.Add(1, tags);
                }

                if (_Activity != null)
                {
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeOutcome, _Outcome);
                    if (_Activity.Status == ActivityStatusCode.Unset) _Activity.SetStatus(ActivityStatusCode.Ok);
                    _Activity.Dispose();
                }
            }
            catch (Exception)
            {
            }
        }

        #endregion
    }
}
