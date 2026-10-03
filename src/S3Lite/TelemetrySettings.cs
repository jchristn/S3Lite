namespace S3Lite
{
    /// <summary>
    /// Per-client telemetry settings.  S3Lite emits metrics and traces through the BCL <c>Meter</c> and
    /// <c>ActivitySource</c> named <see cref="S3LiteTelemetryNames.MeterName"/> and
    /// <see cref="S3LiteTelemetryNames.ActivitySourceName"/>.  Emission is on by default and costs effectively nothing
    /// until a collector subscribes to those names.  Instances are not thread-safe for concurrent mutation; configure
    /// before issuing requests.
    /// </summary>
    public class TelemetrySettings
    {
        #region Public-Members

        /// <summary>
        /// Master switch.  When false, the client emits no metrics, spans, or propagation headers.  Default is true.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Emit metrics on the S3Lite meter.  Ignored when <see cref="Enable"/> is false.  Default is true.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Emit spans on the S3Lite activity source.  Ignored when <see cref="Enable"/> is false.  Default is true.
        /// </summary>
        public bool EnableTraces { get; set; } = true;

        /// <summary>
        /// Inject the W3C <c>traceparent</c> and <c>tracestate</c> headers of the current activity into outbound
        /// requests.  The headers are added after signing, so they are not part of the SigV4 signature.
        /// Ignored when <see cref="Enable"/> is false.  Default is true.
        /// </summary>
        public bool PropagateContext { get; set; } = true;

        /// <summary>
        /// Record object keys (<c>aws.s3.key</c>) and exception messages (which embed request URLs) on spans.
        /// Object keys can carry user data, so this is off by default.  Keys are never recorded on metrics.
        /// Default is false.
        /// </summary>
        public bool IncludeObjectKeys { get; set; } = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate with defaults (enabled, metrics and traces on, propagation on, object keys off).
        /// </summary>
        public TelemetrySettings()
        {

        }

        #endregion
    }
}
