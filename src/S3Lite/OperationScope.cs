namespace S3Lite
{
    using System;
    using System.Diagnostics;
    using System.Net;

    /// <summary>
    /// Telemetry scope for one S3 operation: a client span plus duration, outcome, and in-flight metrics.
    /// All members are best-effort and never throw.  Not thread-safe; one scope per operation invocation.
    /// </summary>
    internal sealed class OperationScope : IDisposable
    {
        #region Internal-Members

        internal string Operation
        {
            get
            {
                return _Operation;
            }
        }

        internal bool MetricsEnabled
        {
            get
            {
                return _Metrics;
            }
        }

        internal bool TracesEnabled
        {
            get
            {
                return _Activity != null;
            }
        }

        internal bool IncludeObjectKeys
        {
            get
            {
                return _IncludeKeys;
            }
        }

        #endregion

        #region Private-Members

        private readonly string _Operation;
        private readonly string _ServerAddress;
        private readonly int _ServerPort;
        private readonly bool _Metrics;
        private readonly bool _IncludeKeys;
        private readonly long _StartTimestamp;
        private Activity _Activity = null;
        private string _Outcome = null;
        private string _ErrorType = null;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        private OperationScope(S3Client client, string operation, string httpMethod, string bucket, string key, bool metrics, bool traces, bool includeKeys)
        {
            _Operation = operation;
            _ServerAddress = client.Hostname;
            _ServerPort = client.Port;
            _Metrics = metrics;
            _IncludeKeys = includeKeys;
            _StartTimestamp = Stopwatch.GetTimestamp();

            if (traces)
            {
                _Activity = S3LiteTelemetry.Source.StartActivity(S3LiteTelemetryNames.OperationSpanPrefix + operation, ActivityKind.Client);

                if (_Activity != null && _Activity.IsAllDataRequested)
                {
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeRpcSystem, "aws-api");
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeRpcService, "S3");
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeRpcMethod, operation);
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeOperation, operation);
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeHttpMethod, httpMethod);
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeServerAddress, _ServerAddress);
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeServerPort, _ServerPort);
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeUrlScheme, client.Protocol == ProtocolEnum.Https ? "https" : "http");
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeRequestStyle, client.RequestStyle.ToString());
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeAuthMode, client.HasCredentials ? "signed" : "anonymous");
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeGateway, client.Gateway != null && !String.IsNullOrEmpty(client.Gateway.Hostname));
                    if (!String.IsNullOrEmpty(bucket)) _Activity.SetTag(S3LiteTelemetryNames.AttributeBucket, bucket);
                    if (includeKeys && !String.IsNullOrEmpty(key)) _Activity.SetTag(S3LiteTelemetryNames.AttributeKey, key);
                }
            }

            if (_Metrics)
            {
                TagList tags = new TagList();
                tags.Add(S3LiteTelemetryNames.AttributeOperation, _Operation);
                tags.Add(S3LiteTelemetryNames.AttributeServerAddress, _ServerAddress);
                tags.Add(S3LiteTelemetryNames.AttributeServerPort, _ServerPort);
                S3LiteTelemetry.ActiveOperations.Add(1, tags);
            }
        }

        /// <summary>
        /// Start a scope, or return null when telemetry is disabled or nothing is listening.
        /// </summary>
        internal static OperationScope Start(S3Client client, string operation, string httpMethod, string bucket = null, string key = null)
        {
            try
            {
                TelemetrySettings settings = client?.Telemetry;
                if (settings == null || !settings.Enable) return null;

                bool metrics = settings.EnableMetrics && S3LiteTelemetry.MetricsObserved;
                bool traces = settings.EnableTraces && S3LiteTelemetry.Source.HasListeners();
                if (!metrics && !traces) return null;

                return new OperationScope(client, operation, httpMethod, bucket, key, metrics, traces, settings.IncludeObjectKeys);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Internal-Methods

        internal StageScope StartStage(string stage)
        {
            try
            {
                if (!_Metrics && _Activity == null) return null;
                return new StageScope(this, stage);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal void SetTag(string name, object value)
        {
            try
            {
                if (_Activity != null && _Activity.IsAllDataRequested) _Activity.SetTag(name, value);
            }
            catch (Exception)
            {
            }
        }

        internal void SetResponse(int statusCode, string requestId, long? requestBytes, long? responseBytes)
        {
            try
            {
                SetTag(S3LiteTelemetryNames.AttributeHttpStatusCode, statusCode);
                if (!String.IsNullOrEmpty(requestId)) SetTag(S3LiteTelemetryNames.AttributeRequestId, requestId);
                if (requestBytes != null) SetTag(S3LiteTelemetryNames.AttributeRequestBodySize, requestBytes.Value);
                if (responseBytes != null) SetTag(S3LiteTelemetryNames.AttributeResponseBodySize, responseBytes.Value);

                if (_Metrics)
                {
                    TagList tags = new TagList();
                    tags.Add(S3LiteTelemetryNames.AttributeOperation, _Operation);
                    if (requestBytes != null) S3LiteTelemetry.RequestBodySize.Record(requestBytes.Value, tags);
                    if (responseBytes != null) S3LiteTelemetry.ResponseBodySize.Record(responseBytes.Value, tags);
                }
            }
            catch (Exception)
            {
            }
        }

        internal void Succeed()
        {
            _Outcome = S3LiteTelemetryNames.OutcomeSuccess;
            _ErrorType = null;
        }

        internal void NotFound()
        {
            _Outcome = S3LiteTelemetryNames.OutcomeNotFound;
            _ErrorType = null;
        }

        internal void Fail(Exception e)
        {
            try
            {
                if (e is OperationCanceledException)
                {
                    _Outcome = S3LiteTelemetryNames.OutcomeCancelled;
                    _ErrorType = e.GetType().FullName;
                }
                else
                {
                    _Outcome = S3LiteTelemetryNames.OutcomeError;
                    _ErrorType = ClassifyError(e);
                }

                if (_Activity != null)
                {
                    _Activity.SetStatus(ActivityStatusCode.Error, _ErrorType);
                    if (_Activity.IsAllDataRequested)
                    {
                        WebException we = e as WebException;
                        if (we != null && we.Data.Contains("ErrorCode"))
                            _Activity.SetTag(S3LiteTelemetryNames.AttributeS3ErrorCode, Convert.ToString(we.Data["ErrorCode"]));
                        if (we != null && we.Data.Contains("RequestId"))
                            _Activity.SetTag(S3LiteTelemetryNames.AttributeRequestId, Convert.ToString(we.Data["RequestId"]));
                        RecordException(_Activity, e, _IncludeKeys);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        internal static void RecordException(Activity activity, Exception e, bool includeMessage)
        {
            ActivityTagsCollection tags = new ActivityTagsCollection();
            tags.Add("exception.type", e.GetType().FullName);
            if (includeMessage) tags.Add("exception.message", e.Message);
            if (!String.IsNullOrEmpty(e.StackTrace)) tags.Add("exception.stacktrace", e.StackTrace);
            activity.AddEvent(new ActivityEvent("exception", default(DateTimeOffset), tags));
        }

        /// <summary>
        /// Record the end of the operation.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            try
            {
                if (_Outcome == null)
                {
                    _Outcome = S3LiteTelemetryNames.OutcomeError;
                    _ErrorType = S3LiteTelemetryNames.ErrorTypeOther;
                }

                if (_Metrics)
                {
                    double seconds = (Stopwatch.GetTimestamp() - _StartTimestamp) / (double)Stopwatch.Frequency;

                    TagList tags = new TagList();
                    tags.Add(S3LiteTelemetryNames.AttributeOperation, _Operation);
                    tags.Add(S3LiteTelemetryNames.AttributeOutcome, _Outcome);
                    if (_ErrorType != null) tags.Add(S3LiteTelemetryNames.AttributeErrorType, _ErrorType);
                    tags.Add(S3LiteTelemetryNames.AttributeServerAddress, _ServerAddress);
                    tags.Add(S3LiteTelemetryNames.AttributeServerPort, _ServerPort);
                    S3LiteTelemetry.OperationDuration.Record(seconds, tags);
                    S3LiteTelemetry.Operations.Add(1, tags);

                    TagList activeTags = new TagList();
                    activeTags.Add(S3LiteTelemetryNames.AttributeOperation, _Operation);
                    activeTags.Add(S3LiteTelemetryNames.AttributeServerAddress, _ServerAddress);
                    activeTags.Add(S3LiteTelemetryNames.AttributeServerPort, _ServerPort);
                    S3LiteTelemetry.ActiveOperations.Add(-1, activeTags);
                }

                if (_Activity != null)
                {
                    _Activity.SetTag(S3LiteTelemetryNames.AttributeOutcome, _Outcome);
                    if (_ErrorType != null) _Activity.SetTag(S3LiteTelemetryNames.AttributeErrorType, _ErrorType);
                    if (_Activity.Status == ActivityStatusCode.Unset)
                    {
                        if (_Outcome == S3LiteTelemetryNames.OutcomeSuccess || _Outcome == S3LiteTelemetryNames.OutcomeNotFound)
                            _Activity.SetStatus(ActivityStatusCode.Ok);
                        else
                            _Activity.SetStatus(ActivityStatusCode.Error, _ErrorType);
                    }
                    _Activity.Dispose();
                }
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Private-Methods

        private static string ClassifyError(Exception e)
        {
            WebException we = e as WebException;
            if (we != null)
            {
                if (we.Data.Contains("ErrorCode"))
                {
                    string code = S3LiteTelemetry.NormalizeErrorCode(Convert.ToString(we.Data["ErrorCode"]));
                    if (code != null && code != S3LiteTelemetryNames.ErrorTypeOther) return code;
                }

                if (we.Data.Contains("StatusCode") && we.Data["StatusCode"] != null)
                    return Convert.ToString(we.Data["StatusCode"]);

                return S3LiteTelemetryNames.ErrorTypeNoResponse;
            }

            return e.GetType().FullName;
        }

        #endregion
    }
}
