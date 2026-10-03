namespace S3Lite
{
    /// <summary>
    /// Stable public names for every S3Lite telemetry point: the meter and activity source names, metric instrument
    /// names, span names, attribute keys, and the bounded values used for operation, stage, and outcome labels.
    /// These strings are a public contract consumed by collectors and Grafana dashboards.  Treat changes as breaking.
    /// This class is thread-safe (immutable constants).
    /// </summary>
    public static class S3LiteTelemetryNames
    {
        #region Sources

        /// <summary>
        /// Name of the <see cref="System.Diagnostics.Metrics.Meter"/> used by S3Lite.  Subscribe to this name to collect metrics.
        /// </summary>
        public const string MeterName = "S3Lite";

        /// <summary>
        /// Name of the <see cref="System.Diagnostics.ActivitySource"/> used by S3Lite.  Subscribe to this name to collect traces.
        /// </summary>
        public const string ActivitySourceName = "S3Lite";

        #endregion

        #region Metrics

        /// <summary>
        /// Histogram (s): end-to-end duration of each S3 operation.  Labels: s3.operation, outcome, error.type (failures only), server.address, server.port.
        /// </summary>
        public const string OperationDuration = "s3lite.client.operation.duration";

        /// <summary>
        /// Counter ({operation}): completed S3 operations.  Labels: s3.operation, outcome, error.type (failures only), server.address, server.port.
        /// </summary>
        public const string Operations = "s3lite.client.operations";

        /// <summary>
        /// UpDownCounter ({operation}): S3 operations currently in flight.  Labels: s3.operation, server.address, server.port.
        /// </summary>
        public const string ActiveOperations = "s3lite.client.active_operations";

        /// <summary>
        /// Histogram (s): duration of each operation sub-stage (sign, send, response).  Labels: s3.operation, s3lite.stage, outcome.
        /// </summary>
        public const string StageDuration = "s3lite.client.stage.duration";

        /// <summary>
        /// Counter ({stage}): completed operation sub-stages.  Labels: s3.operation, s3lite.stage, outcome.
        /// </summary>
        public const string Stages = "s3lite.client.stages";

        /// <summary>
        /// Histogram (By): request body size sent per operation.  Labels: s3.operation.
        /// </summary>
        public const string RequestBodySize = "s3lite.client.request.body.size";

        /// <summary>
        /// Histogram (By): response body size received per operation, as reported by the server.  Labels: s3.operation.
        /// </summary>
        public const string ResponseBodySize = "s3lite.client.response.body.size";

        /// <summary>
        /// Counter ({object}): objects returned by ListObjectsV2 pages.  Labels: none.
        /// </summary>
        public const string ObjectsListed = "s3lite.client.objects.listed";

        /// <summary>
        /// Counter ({object}): per-object results of DeleteObjects (batch delete).  Labels: outcome (deleted, failed), error.type (failed only).
        /// </summary>
        public const string BatchDeleteObjects = "s3lite.client.batch_delete.objects";

        /// <summary>
        /// Histogram ({object}): number of objects requested per DeleteObjects (batch delete) call.  Labels: none.
        /// </summary>
        public const string BatchDeleteSize = "s3lite.client.batch_delete.size";

        /// <summary>
        /// Observable gauge (no unit): constant 1 labeled with the S3Lite assembly version.  Labels: s3lite.version.
        /// </summary>
        public const string BuildInfo = "s3lite.build.info";

        #endregion

        #region Span-Names

        /// <summary>
        /// Prefix of the client span opened for each S3 operation, for example "S3 GetObject".
        /// </summary>
        public const string OperationSpanPrefix = "S3 ";

        /// <summary>
        /// Prefix of the internal span opened for each operation sub-stage, for example "stage:send".
        /// </summary>
        public const string StageSpanPrefix = "stage:";

        #endregion

        #region Attributes

        /// <summary>
        /// Attribute: logical S3 operation name (see the Operation* constants).
        /// </summary>
        public const string AttributeOperation = "s3.operation";

        /// <summary>
        /// Attribute: operation outcome (see the Outcome* constants).
        /// </summary>
        public const string AttributeOutcome = "outcome";

        /// <summary>
        /// Attribute: bounded error classifier.  An S3 error code (for example NoSuchKey), an HTTP status code, or an exception type name.
        /// </summary>
        public const string AttributeErrorType = "error.type";

        /// <summary>
        /// Attribute: operation sub-stage (see the Stage* constants).
        /// </summary>
        public const string AttributeStage = "s3lite.stage";

        /// <summary>
        /// Attribute: configured S3 endpoint hostname (not the bucket-prefixed or gateway hostname).
        /// </summary>
        public const string AttributeServerAddress = "server.address";

        /// <summary>
        /// Attribute: configured S3 endpoint port.
        /// </summary>
        public const string AttributeServerPort = "server.port";

        /// <summary>
        /// Span attribute: RPC system, always "aws-api".
        /// </summary>
        public const string AttributeRpcSystem = "rpc.system";

        /// <summary>
        /// Span attribute: RPC service, always "S3".
        /// </summary>
        public const string AttributeRpcService = "rpc.service";

        /// <summary>
        /// Span attribute: RPC method, equal to the S3 operation name.
        /// </summary>
        public const string AttributeRpcMethod = "rpc.method";

        /// <summary>
        /// Span attribute: HTTP request method.
        /// </summary>
        public const string AttributeHttpMethod = "http.request.method";

        /// <summary>
        /// Span attribute: HTTP response status code.
        /// </summary>
        public const string AttributeHttpStatusCode = "http.response.status_code";

        /// <summary>
        /// Span attribute: URL scheme (http or https).
        /// </summary>
        public const string AttributeUrlScheme = "url.scheme";

        /// <summary>
        /// Span attribute: bucket name.
        /// </summary>
        public const string AttributeBucket = "aws.s3.bucket";

        /// <summary>
        /// Span attribute: object key.  Only recorded when <see cref="TelemetrySettings.IncludeObjectKeys"/> is true.
        /// </summary>
        public const string AttributeKey = "aws.s3.key";

        /// <summary>
        /// Span attribute: S3 request ID returned in the x-amz-request-id response header.
        /// </summary>
        public const string AttributeRequestId = "aws.request_id";

        /// <summary>
        /// Span attribute: S3 error code parsed from the error response body.
        /// </summary>
        public const string AttributeS3ErrorCode = "aws.s3.error_code";

        /// <summary>
        /// Span attribute: request body size in bytes.
        /// </summary>
        public const string AttributeRequestBodySize = "http.request.body.size";

        /// <summary>
        /// Span attribute: response body size in bytes.
        /// </summary>
        public const string AttributeResponseBodySize = "http.response.body.size";

        /// <summary>
        /// Span attribute: request style (PathStyle or VirtualHostedStyle).
        /// </summary>
        public const string AttributeRequestStyle = "s3lite.request_style";

        /// <summary>
        /// Span attribute: authentication mode (signed or anonymous).
        /// </summary>
        public const string AttributeAuthMode = "s3lite.auth_mode";

        /// <summary>
        /// Span attribute: true when the request is routed through a <see cref="GatewayConfig"/>.
        /// </summary>
        public const string AttributeGateway = "s3lite.gateway";

        /// <summary>
        /// Span attribute: number of objects requested in a DeleteObjects call.
        /// </summary>
        public const string AttributeDeleteRequested = "s3lite.delete.requested";

        /// <summary>
        /// Span attribute: number of objects reported deleted by a DeleteObjects call.
        /// </summary>
        public const string AttributeDeleteDeleted = "s3lite.delete.deleted";

        /// <summary>
        /// Span attribute: number of objects reported failed by a DeleteObjects call.
        /// </summary>
        public const string AttributeDeleteFailed = "s3lite.delete.failed";

        /// <summary>
        /// Span attribute: number of objects returned by a ListObjectsV2 page.
        /// </summary>
        public const string AttributeListCount = "s3lite.list.count";

        /// <summary>
        /// Span attribute: true when a ListObjectsV2 page is truncated (more pages remain).
        /// </summary>
        public const string AttributeListTruncated = "s3lite.list.truncated";

        /// <summary>
        /// Metric attribute: S3Lite assembly version (build info only).
        /// </summary>
        public const string AttributeVersion = "s3lite.version";

        #endregion

        #region Operations

        /// <summary>
        /// Operation: Service.ListBucketsAsync.
        /// </summary>
        public const string OperationListBuckets = "ListBuckets";

        /// <summary>
        /// Operation: Bucket.ExistsAsync.
        /// </summary>
        public const string OperationHeadBucket = "HeadBucket";

        /// <summary>
        /// Operation: Bucket.ListAsync.
        /// </summary>
        public const string OperationListObjects = "ListObjectsV2";

        /// <summary>
        /// Operation: Bucket.WriteAsync.
        /// </summary>
        public const string OperationCreateBucket = "CreateBucket";

        /// <summary>
        /// Operation: Bucket.DeleteAsync.
        /// </summary>
        public const string OperationDeleteBucket = "DeleteBucket";

        /// <summary>
        /// Operation: Object.ExistsAsync and Object.GetMetadataAsync.
        /// </summary>
        public const string OperationHeadObject = "HeadObject";

        /// <summary>
        /// Operation: Object.GetAsync.
        /// </summary>
        public const string OperationGetObject = "GetObject";

        /// <summary>
        /// Operation: Object.WriteAsync.
        /// </summary>
        public const string OperationPutObject = "PutObject";

        /// <summary>
        /// Operation: Object.DeleteAsync.
        /// </summary>
        public const string OperationDeleteObject = "DeleteObject";

        /// <summary>
        /// Operation: Object.DeleteMultipleAsync.
        /// </summary>
        public const string OperationDeleteObjects = "DeleteObjects";

        #endregion

        #region Stages

        /// <summary>
        /// Stage: computing the SigV4 signature (skipped in anonymous mode).
        /// </summary>
        public const string StageSign = "sign";

        /// <summary>
        /// Stage: sending the HTTP request and receiving response headers.
        /// </summary>
        public const string StageSend = "send";

        /// <summary>
        /// Stage: reading and deserializing the response body.
        /// </summary>
        public const string StageResponse = "response";

        #endregion

        #region Outcomes

        /// <summary>
        /// Outcome: the operation succeeded.
        /// </summary>
        public const string OutcomeSuccess = "success";

        /// <summary>
        /// Outcome: an existence check returned 404 (an expected, non-error result).
        /// </summary>
        public const string OutcomeNotFound = "not_found";

        /// <summary>
        /// Outcome: the operation failed (HTTP error, connection failure, or exception).
        /// </summary>
        public const string OutcomeError = "error";

        /// <summary>
        /// Outcome: the operation was cancelled by the caller.
        /// </summary>
        public const string OutcomeCancelled = "cancelled";

        /// <summary>
        /// Batch delete outcome: the object was deleted.
        /// </summary>
        public const string OutcomeDeleted = "deleted";

        /// <summary>
        /// Batch delete outcome: the object could not be deleted.
        /// </summary>
        public const string OutcomeFailed = "failed";

        #endregion

        #region Error-Types

        /// <summary>
        /// error.type value used when no response was received from the server.
        /// </summary>
        public const string ErrorTypeNoResponse = "no_response";

        /// <summary>
        /// error.type value used for an S3 error code that S3Lite does not recognize (keeps the label bounded).
        /// </summary>
        public const string ErrorTypeOther = "_OTHER";

        #endregion
    }
}
