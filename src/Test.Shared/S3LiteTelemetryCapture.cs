namespace Test.Shared
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using S3Lite;

    /// <summary>
    /// In-memory listener for the S3Lite meter and activity source.  Captures only measurements and spans that
    /// belong to the trace of the capture's own root activity, so concurrently running tests do not interfere.
    /// </summary>
    internal sealed class S3LiteTelemetryCapture : IDisposable
    {
        private const string _TestSourceName = "Test.S3Lite.Telemetry";
        private static readonly ActivitySource _TestSource = new ActivitySource(_TestSourceName);

        private readonly ActivityListener _ActivityListener;
        private readonly MeterListener _MeterListener;
        private readonly Activity _Root;
        private readonly ActivityTraceId _TraceId;
        private readonly ConcurrentQueue<Activity> _Activities = new ConcurrentQueue<Activity>();
        private readonly ConcurrentQueue<S3LiteCapturedMeasurement> _Measurements = new ConcurrentQueue<S3LiteCapturedMeasurement>();
        private int _Disposed = 0;

        /// <summary>
        /// Start capturing.  Subscribes to the S3Lite meter and activity source and starts a root test activity.
        /// </summary>
        /// <param name="listenToTraces">Subscribe to the S3Lite activity source.</param>
        /// <param name="listenToMetrics">Subscribe to the S3Lite meter.</param>
        internal S3LiteTelemetryCapture(bool listenToTraces = true, bool listenToMetrics = true)
        {
            _ActivityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == _TestSourceName || (listenToTraces && source.Name == S3LiteTelemetryNames.ActivitySourceName),
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    if (activity.Source.Name == S3LiteTelemetryNames.ActivitySourceName && activity.TraceId == _TraceId)
                        _Activities.Enqueue(activity);
                }
            };
            ActivitySource.AddActivityListener(_ActivityListener);

            _Root = _TestSource.StartActivity("telemetry-test", ActivityKind.Internal)
                ?? throw new InvalidOperationException("Unable to start the root test activity.");
            _TraceId = _Root.TraceId;

            _MeterListener = new MeterListener();
            _MeterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (listenToMetrics && instrument.Meter.Name == S3LiteTelemetryNames.MeterName)
                    listener.EnableMeasurementEvents(instrument);
            };
            _MeterListener.SetMeasurementEventCallback<long>((instrument, value, tags, state) => OnMeasurement(instrument, value, tags));
            _MeterListener.SetMeasurementEventCallback<int>((instrument, value, tags, state) => OnMeasurement(instrument, value, tags, true));
            _MeterListener.SetMeasurementEventCallback<double>((instrument, value, tags, state) => OnMeasurement(instrument, value, tags));
            _MeterListener.Start();
        }

        /// <summary>
        /// Trace ID of the root test activity.
        /// </summary>
        internal ActivityTraceId TraceId
        {
            get
            {
                return _TraceId;
            }
        }

        /// <summary>
        /// Captured S3Lite spans, in stop order.
        /// </summary>
        internal IReadOnlyList<Activity> Activities
        {
            get
            {
                return _Activities.ToList();
            }
        }

        /// <summary>
        /// Captured S3Lite measurements, in record order.
        /// </summary>
        internal IReadOnlyList<S3LiteCapturedMeasurement> Measurements
        {
            get
            {
                return _Measurements.ToList();
            }
        }

        /// <summary>
        /// Poll observable instruments (for example the build-info gauge).
        /// </summary>
        internal void RecordObservableInstruments()
        {
            _MeterListener.RecordObservableInstruments();
        }

        /// <summary>
        /// Captured measurements for one instrument.
        /// </summary>
        /// <param name="name">Instrument name.</param>
        /// <returns>Measurements.</returns>
        internal List<S3LiteCapturedMeasurement> For(string name)
        {
            return _Measurements.Where(m => m.Name == name).ToList();
        }

        /// <summary>
        /// Captured spans with the specified display name.
        /// </summary>
        /// <param name="name">Span name.</param>
        /// <returns>Spans.</returns>
        internal List<Activity> Spans(string name)
        {
            return _Activities.Where(a => a.DisplayName == name).ToList();
        }

        /// <summary>
        /// Stop capturing.
        /// </summary>
        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _Disposed, 1) == 1) return;
            _MeterListener.Dispose();
            _Root.Dispose();
            _ActivityListener.Dispose();
        }

        private void OnMeasurement<T>(Instrument instrument, T value, ReadOnlySpan<KeyValuePair<string, object?>> tags, bool observable = false)
        {
            // Synchronous instruments are filtered to this capture's trace; Activity.Current flows with the async
            // call that recorded the measurement.  Observable instruments are polled on the caller's thread.
            if (!observable && (Activity.Current == null || Activity.Current.TraceId != _TraceId)) return;

            Dictionary<string, object?> copy = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object?> tag in tags) copy[tag.Key] = tag.Value;

            _Measurements.Enqueue(new S3LiteCapturedMeasurement(instrument.Name, instrument.Unit, Convert.ToDouble(value), copy));
        }
    }
}
