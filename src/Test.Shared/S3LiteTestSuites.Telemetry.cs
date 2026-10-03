namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using S3Lite;
    using S3Lite.ApiObjects;
    using Touchstone.Core;

    public static partial class S3LiteTestSuites
    {
        private static TestSuiteDescriptor BuildTelemetrySuite()
        {
            string suiteId = "Telemetry";

            return new TestSuiteDescriptor(
                suiteId,
                "Metrics and traces emitted on the S3Lite meter and activity source",
                new[]
                {
                    new TestCaseDescriptor(suiteId, "NamesAreStable", "Meter, activity source, and instrument names match the documented contract", ct => TestTelemetryNamesAreStableAsync(ct)),
                    new TestCaseDescriptor(suiteId, "DefaultSettings", "TelemetrySettings defaults are enabled with object keys off, and null is rejected", ct => TestTelemetryDefaultSettingsAsync(ct)),
                    new TestCaseDescriptor(suiteId, "NoListenerDoesNotThrow", "Operations succeed and fail normally when nothing subscribes", ct => TestTelemetryNoListenerDoesNotThrowAsync(ct)),
                    new TestCaseDescriptor(suiteId, "DisabledEmitsNothing", "Telemetry.Enable = false emits no spans, metrics, or traceparent", ct => TestTelemetryDisabledEmitsNothingAsync(ct)),
                    new TestCaseDescriptor(suiteId, "EveryOperationEmitsSpanAndMetrics", "Every S3 operation emits a client span, stage spans, duration, counter, and stage metrics", ct => TestTelemetryEveryOperationAsync(ct)),
                    new TestCaseDescriptor(suiteId, "ExistsNotFoundOutcome", "Existence checks that return 404 record outcome not_found with an Ok span", ct => TestTelemetryExistsNotFoundAsync(ct)),
                    new TestCaseDescriptor(suiteId, "S3ErrorClassified", "S3 error responses record outcome error, error.type = S3 error code, and an Error span with an exception event", ct => TestTelemetryS3ErrorClassifiedAsync(ct)),
                    new TestCaseDescriptor(suiteId, "AnonymousSkipsSignStage", "Anonymous requests skip the sign stage and classify AccessDenied", ct => TestTelemetryAnonymousSkipsSignStageAsync(ct)),
                    new TestCaseDescriptor(suiteId, "ConnectionFailure", "Connection failures record outcome error and a failed send stage", ct => TestTelemetryConnectionFailureAsync(ct)),
                    new TestCaseDescriptor(suiteId, "Cancellation", "Cancelled operations record outcome cancelled", ct => TestTelemetryCancellationAsync(ct)),
                    new TestCaseDescriptor(suiteId, "BatchDeleteDomainMetrics", "DeleteObjects records per-object deleted/failed counts and batch size", ct => TestTelemetryBatchDeleteAsync(ct)),
                    new TestCaseDescriptor(suiteId, "ListDomainMetrics", "ListObjectsV2 records objects listed and page attributes", ct => TestTelemetryListAsync(ct)),
                    new TestCaseDescriptor(suiteId, "BodySizeMetrics", "Request and response body sizes are recorded in bytes", ct => TestTelemetryBodySizeAsync(ct)),
                    new TestCaseDescriptor(suiteId, "ActiveOperationsBalanced", "The in-flight operations counter returns to zero", ct => TestTelemetryActiveOperationsBalancedAsync(ct)),
                    new TestCaseDescriptor(suiteId, "ContextPropagation", "W3C traceparent is injected (unsigned) and can be disabled", ct => TestTelemetryContextPropagationAsync(ct)),
                    new TestCaseDescriptor(suiteId, "ObjectKeysOptIn", "Object keys appear on spans only when IncludeObjectKeys is true, and never on metrics", ct => TestTelemetryObjectKeysOptInAsync(ct)),
                    new TestCaseDescriptor(suiteId, "MetricsOnlyAndTracesOnly", "EnableMetrics and EnableTraces toggle each signal independently", ct => TestTelemetryTogglesAsync(ct)),
                    new TestCaseDescriptor(suiteId, "BuildInfoGauge", "s3lite.build.info reports 1 with the assembly version", ct => TestTelemetryBuildInfoAsync(ct)),
                    new TestCaseDescriptor(suiteId, "LabelsAreBounded", "Metric labels never carry bucket names, keys, or request IDs", ct => TestTelemetryLabelsAreBoundedAsync(ct))
                });
        }

        private static Task TestTelemetryNamesAreStableAsync(CancellationToken cancellationToken)
        {
            AssertEqual("S3Lite", S3LiteTelemetryNames.MeterName, "meter name");
            AssertEqual("S3Lite", S3LiteTelemetryNames.ActivitySourceName, "activity source name");
            AssertEqual("s3lite.client.operation.duration", S3LiteTelemetryNames.OperationDuration, "operation duration name");
            AssertEqual("s3lite.client.operations", S3LiteTelemetryNames.Operations, "operations name");
            AssertEqual("s3lite.client.active_operations", S3LiteTelemetryNames.ActiveOperations, "active operations name");
            AssertEqual("s3lite.client.stage.duration", S3LiteTelemetryNames.StageDuration, "stage duration name");
            AssertEqual("s3lite.client.stages", S3LiteTelemetryNames.Stages, "stages name");
            AssertEqual("s3lite.client.request.body.size", S3LiteTelemetryNames.RequestBodySize, "request size name");
            AssertEqual("s3lite.client.response.body.size", S3LiteTelemetryNames.ResponseBodySize, "response size name");
            AssertEqual("s3lite.client.objects.listed", S3LiteTelemetryNames.ObjectsListed, "objects listed name");
            AssertEqual("s3lite.client.batch_delete.objects", S3LiteTelemetryNames.BatchDeleteObjects, "batch delete objects name");
            AssertEqual("s3lite.client.batch_delete.size", S3LiteTelemetryNames.BatchDeleteSize, "batch delete size name");
            AssertEqual("s3lite.build.info", S3LiteTelemetryNames.BuildInfo, "build info name");
            return Task.CompletedTask;
        }

        private static Task TestTelemetryDefaultSettingsAsync(CancellationToken cancellationToken)
        {
            S3Client client = new S3Client();
            AssertNotNull(client.Telemetry, "default telemetry settings");
            AssertTrue(client.Telemetry.Enable, "Enable should default to true.");
            AssertTrue(client.Telemetry.EnableMetrics, "EnableMetrics should default to true.");
            AssertTrue(client.Telemetry.EnableTraces, "EnableTraces should default to true.");
            AssertTrue(client.Telemetry.PropagateContext, "PropagateContext should default to true.");
            AssertFalse(client.Telemetry.IncludeObjectKeys, "IncludeObjectKeys should default to false.");
            AssertThrows<ArgumentNullException>(() => client.Telemetry = null!);
            AssertThrows<ArgumentNullException>(() => client.WithTelemetry(null!));

            TelemetrySettings custom = new TelemetrySettings { IncludeObjectKeys = true };
            AssertSame(custom, client.WithTelemetry(custom).Telemetry, "assigned telemetry settings");
            return Task.CompletedTask;
        }

        private static async Task TestTelemetryNoListenerDoesNotThrowAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, true, async (server, httpClient, client, ct) =>
            {
                string key = GenerateObjectKey();
                await client.Object.WriteAsync(_SyntheticBucketName, key, GetBytes("x"), "text/plain", token: ct).ConfigureAwait(false);
                AssertTrue(await client.Object.ExistsAsync(_SyntheticBucketName, key, token: ct).ConfigureAwait(false), "object should exist");
                await AssertThrowsAsync<WebException>(() => client.Object.GetAsync(_SyntheticBucketName, _SyntheticMissingObjectKey, token: ct)).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryDisabledEmitsNothingAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, true, async (server, httpClient, client, ct) =>
            {
                client.Telemetry.Enable = false;

                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    await WithoutRuntimePropagationAsync(async () =>
                    {
                        await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);
                        AssertNull(server.LastRequestHeaders?["traceparent"], "traceparent header when disabled");
                    }).ConfigureAwait(false);

                    await AssertThrowsAsync<WebException>(() => client.Object.GetAsync(_SyntheticBucketName, _SyntheticMissingObjectKey, token: ct)).ConfigureAwait(false);

                    AssertEqual(0, capture.Activities.Count, "captured spans");
                    AssertEqual(0, capture.Measurements.Count, "captured measurements");
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryEveryOperationAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.External, true, async (server, httpClient, client, ct) =>
            {
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    string bucket = GenerateBucketName();
                    string key = GenerateObjectKey();

                    await client.Service.ListBucketsAsync(token: ct).ConfigureAwait(false);
                    await client.Bucket.WriteAsync(bucket, server.Region, token: ct).ConfigureAwait(false);
                    await client.Bucket.ExistsAsync(bucket, token: ct).ConfigureAwait(false);
                    await client.Object.WriteAsync(bucket, key, GetBytes("payload"), "text/plain", token: ct).ConfigureAwait(false);
                    await client.Object.ExistsAsync(bucket, key, token: ct).ConfigureAwait(false);
                    await client.Object.GetMetadataAsync(bucket, key, token: ct).ConfigureAwait(false);
                    await client.Object.GetAsync(bucket, key, token: ct).ConfigureAwait(false);
                    await client.Bucket.ListAsync(bucket, token: ct).ConfigureAwait(false);
                    await client.Object.DeleteAsync(bucket, key, token: ct).ConfigureAwait(false);
                    await client.Object.WriteAsync(bucket, key, GetBytes("payload"), "text/plain", token: ct).ConfigureAwait(false);
                    await client.Object.DeleteMultipleAsync(bucket, new[] { key }, token: ct).ConfigureAwait(false);
                    await client.Bucket.DeleteAsync(bucket, token: ct).ConfigureAwait(false);

                    Dictionary<string, int> expectedCounts = new Dictionary<string, int>
                    {
                        { S3LiteTelemetryNames.OperationListBuckets, 1 },
                        { S3LiteTelemetryNames.OperationCreateBucket, 1 },
                        { S3LiteTelemetryNames.OperationHeadBucket, 1 },
                        { S3LiteTelemetryNames.OperationPutObject, 2 },
                        { S3LiteTelemetryNames.OperationHeadObject, 2 },
                        { S3LiteTelemetryNames.OperationGetObject, 1 },
                        { S3LiteTelemetryNames.OperationListObjects, 1 },
                        { S3LiteTelemetryNames.OperationDeleteObject, 1 },
                        { S3LiteTelemetryNames.OperationDeleteObjects, 1 },
                        { S3LiteTelemetryNames.OperationDeleteBucket, 1 }
                    };

                    HashSet<string> responseStageOps = new HashSet<string>
                    {
                        S3LiteTelemetryNames.OperationListBuckets,
                        S3LiteTelemetryNames.OperationGetObject,
                        S3LiteTelemetryNames.OperationListObjects,
                        S3LiteTelemetryNames.OperationDeleteObjects
                    };

                    foreach (KeyValuePair<string, int> expected in expectedCounts)
                    {
                        string op = expected.Key;
                        List<Activity> spans = capture.Spans(S3LiteTelemetryNames.OperationSpanPrefix + op);
                        AssertEqual(expected.Value, spans.Count, "span count for " + op);

                        foreach (Activity span in spans)
                        {
                            AssertEqual(ActivityKind.Client.ToString(), span.Kind.ToString(), "span kind for " + op);
                            AssertEqual(ActivityStatusCode.Ok.ToString(), span.Status.ToString(), "span status for " + op);
                            AssertEqual("aws-api", span.GetTagItem(S3LiteTelemetryNames.AttributeRpcSystem) as string, "rpc.system for " + op);
                            AssertEqual(op, span.GetTagItem(S3LiteTelemetryNames.AttributeRpcMethod) as string, "rpc.method for " + op);
                            AssertEqual(S3LiteTelemetryNames.OutcomeSuccess, span.GetTagItem(S3LiteTelemetryNames.AttributeOutcome) as string, "span outcome for " + op);
                            AssertEqual(server.Hostname, span.GetTagItem(S3LiteTelemetryNames.AttributeServerAddress) as string, "server.address for " + op);
                            AssertEqual("signed", span.GetTagItem(S3LiteTelemetryNames.AttributeAuthMode) as string, "auth mode for " + op);
                            AssertNotNull(span.GetTagItem(S3LiteTelemetryNames.AttributeHttpStatusCode), "status code tag for " + op);
                            AssertEqual(capture.TraceId.ToString(), span.TraceId.ToString(), "trace id for " + op);

                            List<Activity> children = capture.Activities.Where(a => a.ParentSpanId == span.SpanId).ToList();
                            AssertTrue(children.Any(c => c.DisplayName == "stage:sign"), "stage:sign span expected for " + op);
                            AssertTrue(children.Any(c => c.DisplayName == "stage:send"), "stage:send span expected for " + op);
                            AssertEqual(responseStageOps.Contains(op), children.Any(c => c.DisplayName == "stage:response"), "stage:response presence for " + op);
                        }

                        List<S3LiteCapturedMeasurement> counts = capture.For(S3LiteTelemetryNames.Operations)
                            .Where(m => m.Tag(S3LiteTelemetryNames.AttributeOperation) == op).ToList();
                        AssertEqual(expected.Value, (int)counts.Sum(m => m.Value), "operations counter for " + op);
                        AssertTrue(counts.All(m => m.Tag(S3LiteTelemetryNames.AttributeOutcome) == S3LiteTelemetryNames.OutcomeSuccess), "success outcome for " + op);
                        AssertTrue(counts.All(m => m.Tag(S3LiteTelemetryNames.AttributeErrorType) == null), "no error.type on success for " + op);
                        AssertTrue(counts.All(m => m.Tag(S3LiteTelemetryNames.AttributeServerPort) == server.Port.ToString()), "server.port for " + op);

                        List<S3LiteCapturedMeasurement> durations = capture.For(S3LiteTelemetryNames.OperationDuration)
                            .Where(m => m.Tag(S3LiteTelemetryNames.AttributeOperation) == op).ToList();
                        AssertEqual(expected.Value, durations.Count, "duration samples for " + op);
                        AssertTrue(durations.All(m => m.Value >= 0 && m.Value < 60), "duration in seconds for " + op);
                        AssertTrue(durations.All(m => m.Unit == "s"), "duration unit for " + op);

                        List<S3LiteCapturedMeasurement> stages = capture.For(S3LiteTelemetryNames.StageDuration)
                            .Where(m => m.Tag(S3LiteTelemetryNames.AttributeOperation) == op).ToList();
                        AssertTrue(stages.Any(m => m.Tag(S3LiteTelemetryNames.AttributeStage) == S3LiteTelemetryNames.StageSign), "sign stage metric for " + op);
                        AssertTrue(stages.Any(m => m.Tag(S3LiteTelemetryNames.AttributeStage) == S3LiteTelemetryNames.StageSend), "send stage metric for " + op);
                        AssertEqual(responseStageOps.Contains(op), stages.Any(m => m.Tag(S3LiteTelemetryNames.AttributeStage) == S3LiteTelemetryNames.StageResponse), "response stage metric for " + op);
                    }

                    AssertTrue(capture.For(S3LiteTelemetryNames.Stages).All(m => m.Tag(S3LiteTelemetryNames.AttributeOutcome) == S3LiteTelemetryNames.OutcomeSuccess), "all stages succeed");
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryExistsNotFoundAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, true, async (server, httpClient, client, ct) =>
            {
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    AssertFalse(await client.Object.ExistsAsync(_SyntheticBucketName, _SyntheticMissingObjectKey, token: ct).ConfigureAwait(false), "missing object");
                    AssertFalse(await client.Bucket.ExistsAsync(_SyntheticMissingBucketName, token: ct).ConfigureAwait(false), "missing bucket");

                    foreach (string op in new[] { S3LiteTelemetryNames.OperationHeadObject, S3LiteTelemetryNames.OperationHeadBucket })
                    {
                        Activity span = capture.Spans(S3LiteTelemetryNames.OperationSpanPrefix + op).Single();
                        AssertEqual(ActivityStatusCode.Ok.ToString(), span.Status.ToString(), "span status for " + op);
                        AssertEqual(S3LiteTelemetryNames.OutcomeNotFound, span.GetTagItem(S3LiteTelemetryNames.AttributeOutcome) as string, "span outcome for " + op);

                        S3LiteCapturedMeasurement count = capture.For(S3LiteTelemetryNames.Operations).Single(m => m.Tag(S3LiteTelemetryNames.AttributeOperation) == op);
                        AssertEqual(S3LiteTelemetryNames.OutcomeNotFound, count.Tag(S3LiteTelemetryNames.AttributeOutcome), "metric outcome for " + op);
                    }
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryS3ErrorClassifiedAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.External, true, async (server, httpClient, client, ct) =>
            {
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    await AssertThrowsAsync<WebException>(() => client.Object.GetAsync(_SyntheticBucketName, _SyntheticMissingObjectKey, token: ct)).ConfigureAwait(false);
                    await AssertThrowsAsync<WebException>(() => client.Bucket.ListAsync(_SyntheticMissingBucketName, token: ct)).ConfigureAwait(false);
                    await AssertThrowsAsync<WebException>(() => client.Object.GetMetadataAsync(_SyntheticBucketName, _SyntheticMissingObjectKey, token: ct)).ConfigureAwait(false);

                    AssertErrorRecorded(capture, S3LiteTelemetryNames.OperationGetObject, ErrorCode.NoSuchKey.ToString());
                    AssertErrorRecorded(capture, S3LiteTelemetryNames.OperationListObjects, ErrorCode.NoSuchBucket.ToString());

                    // HEAD responses carry no body, so the classifier falls back to the HTTP status code.
                    AssertErrorRecorded(capture, S3LiteTelemetryNames.OperationHeadObject, "404");

                    Activity getSpan = capture.Spans(S3LiteTelemetryNames.OperationSpanPrefix + S3LiteTelemetryNames.OperationGetObject).Single();
                    AssertEqual(ErrorCode.NoSuchKey.ToString(), getSpan.GetTagItem(S3LiteTelemetryNames.AttributeS3ErrorCode) as string, "aws.s3.error_code");
                    ActivityEvent exceptionEvent = getSpan.Events.Single(e => e.Name == "exception");
                    AssertEqual(typeof(WebException).FullName, exceptionEvent.Tags.First(t => t.Key == "exception.type").Value as string, "exception.type");
                    AssertFalse(exceptionEvent.Tags.Any(t => t.Key == "exception.message"), "exception.message must be omitted when IncludeObjectKeys is false");
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryAnonymousSkipsSignStageAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, false, async (server, httpClient, client, ct) =>
            {
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);
                    await AssertThrowsAsync<WebException>(() => client.Object.WriteAsync(_SyntheticBucketName, "denied.txt", GetBytes("x"), token: ct)).ConfigureAwait(false);

                    AssertEqual(0, capture.Spans("stage:sign").Count, "sign spans in anonymous mode");
                    AssertFalse(capture.For(S3LiteTelemetryNames.StageDuration).Any(m => m.Tag(S3LiteTelemetryNames.AttributeStage) == S3LiteTelemetryNames.StageSign), "sign stage metric in anonymous mode");
                    Activity getSpan = capture.Spans(S3LiteTelemetryNames.OperationSpanPrefix + S3LiteTelemetryNames.OperationGetObject).Single();
                    AssertEqual("anonymous", getSpan.GetTagItem(S3LiteTelemetryNames.AttributeAuthMode) as string, "auth mode");
                    AssertErrorRecorded(capture, S3LiteTelemetryNames.OperationPutObject, ErrorCode.AccessDenied.ToString());
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryConnectionFailureAsync(CancellationToken cancellationToken)
        {
            S3Client client = new S3Client()
                .WithHostname("127.0.0.1")
                .WithPort(GetUnusedPort())
                .WithProtocol(ProtocolEnum.Http)
                .WithRegion("us-test-1")
                .WithRequestStyle(RequestStyleEnum.PathStyle)
                .WithAccessKey("synthetic-access-key")
                .WithSecretKey("synthetic-secret-key");

            using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
            {
                bool threw = false;
                try
                {
                    await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: cancellationToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    threw = true;
                }

                AssertTrue(threw, "A connection failure should surface to the caller.");

                Activity span = capture.Spans(S3LiteTelemetryNames.OperationSpanPrefix + S3LiteTelemetryNames.OperationGetObject).Single();
                AssertEqual(ActivityStatusCode.Error.ToString(), span.Status.ToString(), "span status");
                AssertEqual(S3LiteTelemetryNames.OutcomeError, span.GetTagItem(S3LiteTelemetryNames.AttributeOutcome) as string, "span outcome");

                S3LiteCapturedMeasurement count = capture.For(S3LiteTelemetryNames.Operations).Single();
                AssertEqual(S3LiteTelemetryNames.OutcomeError, count.Tag(S3LiteTelemetryNames.AttributeOutcome), "metric outcome");
                AssertNotNull(count.Tag(S3LiteTelemetryNames.AttributeErrorType), "error.type");

                S3LiteCapturedMeasurement send = capture.For(S3LiteTelemetryNames.Stages).Single(m => m.Tag(S3LiteTelemetryNames.AttributeStage) == S3LiteTelemetryNames.StageSend);
                AssertEqual(S3LiteTelemetryNames.OutcomeError, send.Tag(S3LiteTelemetryNames.AttributeOutcome), "send stage outcome");
                Activity sendSpan = capture.Spans("stage:send").Single();
                AssertEqual(ActivityStatusCode.Error.ToString(), sendSpan.Status.ToString(), "send span status");
            }
        }

        private static async Task TestTelemetryCancellationAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, true, async (server, httpClient, client, ct) =>
            {
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                using (CancellationTokenSource cts = new CancellationTokenSource())
                {
                    cts.Cancel();
                    await AssertThrowsAsync<OperationCanceledException>(() => client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: cts.Token)).ConfigureAwait(false);

                    S3LiteCapturedMeasurement count = capture.For(S3LiteTelemetryNames.Operations).Single();
                    AssertEqual(S3LiteTelemetryNames.OutcomeCancelled, count.Tag(S3LiteTelemetryNames.AttributeOutcome), "metric outcome");
                    Activity span = capture.Spans(S3LiteTelemetryNames.OperationSpanPrefix + S3LiteTelemetryNames.OperationGetObject).Single();
                    AssertEqual(ActivityStatusCode.Error.ToString(), span.Status.ToString(), "span status");
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryBatchDeleteAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, true, async (server, httpClient, client, ct) =>
            {
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    DeleteResult result = await client.Object.DeleteMultipleAsync(
                        _SyntheticBucketName,
                        new[] { _SyntheticFolderKeyOne, _SyntheticFolderKeyTwo, _SyntheticMissingObjectKey },
                        token: ct).ConfigureAwait(false);

                    int expectedFailed = result.Errors.Count;
                    int expectedDeleted = result.Deleted.Count;

                    List<S3LiteCapturedMeasurement> objects = capture.For(S3LiteTelemetryNames.BatchDeleteObjects);
                    AssertEqual(expectedDeleted, (int)objects.Where(m => m.Tag(S3LiteTelemetryNames.AttributeOutcome) == S3LiteTelemetryNames.OutcomeDeleted).Sum(m => m.Value), "deleted objects counter");
                    AssertEqual(expectedFailed, (int)objects.Where(m => m.Tag(S3LiteTelemetryNames.AttributeOutcome) == S3LiteTelemetryNames.OutcomeFailed).Sum(m => m.Value), "failed objects counter");
                    AssertTrue(objects.Where(m => m.Tag(S3LiteTelemetryNames.AttributeOutcome) == S3LiteTelemetryNames.OutcomeFailed).All(m => m.Tag(S3LiteTelemetryNames.AttributeErrorType) != null), "failed objects carry error.type");

                    S3LiteCapturedMeasurement size = capture.For(S3LiteTelemetryNames.BatchDeleteSize).Single();
                    AssertEqual(3, (int)size.Value, "batch size");

                    Activity span = capture.Spans(S3LiteTelemetryNames.OperationSpanPrefix + S3LiteTelemetryNames.OperationDeleteObjects).Single();
                    AssertEqual(3, Convert.ToInt32(span.GetTagItem(S3LiteTelemetryNames.AttributeDeleteRequested)), "requested tag");
                    AssertEqual(expectedDeleted, Convert.ToInt32(span.GetTagItem(S3LiteTelemetryNames.AttributeDeleteDeleted)), "deleted tag");
                    AssertEqual(expectedFailed, Convert.ToInt32(span.GetTagItem(S3LiteTelemetryNames.AttributeDeleteFailed)), "failed tag");
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryListAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, false, async (server, httpClient, client, ct) =>
            {
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    ListBucketResult result = await client.Bucket.ListAsync(_SyntheticBucketName, maxKeys: 2, token: ct).ConfigureAwait(false);

                    S3LiteCapturedMeasurement listed = capture.For(S3LiteTelemetryNames.ObjectsListed).Single();
                    AssertEqual(result.Contents.Count, (int)listed.Value, "objects listed");

                    Activity span = capture.Spans(S3LiteTelemetryNames.OperationSpanPrefix + S3LiteTelemetryNames.OperationListObjects).Single();
                    AssertEqual(result.Contents.Count, Convert.ToInt32(span.GetTagItem(S3LiteTelemetryNames.AttributeListCount)), "list count tag");
                    AssertEqual(result.IsTruncated, (bool)span.GetTagItem(S3LiteTelemetryNames.AttributeListTruncated)!, "list truncated tag");
                    AssertEqual(_SyntheticBucketName, span.GetTagItem(S3LiteTelemetryNames.AttributeBucket) as string, "bucket tag");
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryBodySizeAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, true, async (server, httpClient, client, ct) =>
            {
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    byte[] payload = new byte[4096];
                    string key = GenerateObjectKey();
                    await client.Object.WriteAsync(_SyntheticBucketName, key, payload, token: ct).ConfigureAwait(false);
                    await client.Object.GetAsync(_SyntheticBucketName, key, token: ct).ConfigureAwait(false);

                    S3LiteCapturedMeasurement sent = capture.For(S3LiteTelemetryNames.RequestBodySize).Single(m => m.Tag(S3LiteTelemetryNames.AttributeOperation) == S3LiteTelemetryNames.OperationPutObject);
                    AssertEqual(4096, (int)sent.Value, "request body size");
                    AssertEqual("By", sent.Unit, "request body size unit");

                    S3LiteCapturedMeasurement received = capture.For(S3LiteTelemetryNames.ResponseBodySize).Single(m => m.Tag(S3LiteTelemetryNames.AttributeOperation) == S3LiteTelemetryNames.OperationGetObject);
                    AssertEqual(4096, (int)received.Value, "response body size");
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryActiveOperationsBalancedAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, true, async (server, httpClient, client, ct) =>
            {
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    await Task.WhenAll(
                        client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct),
                        client.Object.GetAsync(_SyntheticBucketName, _SyntheticFolderKeyOne, token: ct),
                        client.Object.ExistsAsync(_SyntheticBucketName, _SyntheticMissingObjectKey, token: ct)).ConfigureAwait(false);
                    await AssertThrowsAsync<WebException>(() => client.Object.GetAsync(_SyntheticBucketName, _SyntheticMissingObjectKey, token: ct)).ConfigureAwait(false);

                    List<S3LiteCapturedMeasurement> active = capture.For(S3LiteTelemetryNames.ActiveOperations);
                    AssertEqual(4, active.Count(m => m.Value > 0), "increments");
                    AssertEqual(0, (int)active.Sum(m => m.Value), "net in-flight operations");
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryContextPropagationAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, true, async (server, httpClient, client, ct) =>
            {
                // The runtime HttpClient propagator is suppressed so these assertions observe only S3Lite's own injection.
                await WithoutRuntimePropagationAsync(async () =>
                {
                    using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                    {
                        await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);

                        string? traceparent = server.LastRequestHeaders?["traceparent"];
                        AssertNotNull(traceparent, "traceparent header");
                        Activity sendSpan = capture.Spans("stage:send").Single();
                        AssertEqual("00-" + capture.TraceId.ToString() + "-" + sendSpan.SpanId.ToString() + "-01", traceparent, "traceparent value (caller trace, send stage parent)");

                        string? authorization = server.LastRequestHeaders?["Authorization"];
                        AssertNotNull(authorization, "authorization header");
                        AssertFalse(authorization!.Contains("traceparent"), "traceparent must not be a signed header");

                        client.Telemetry.PropagateContext = false;
                        await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);
                        AssertNull(server.LastRequestHeaders?["traceparent"], "traceparent when PropagateContext is false");
                        client.Telemetry.PropagateContext = true;
                    }

                    // With no S3Lite listener, the caller's ambient activity is still propagated.
                    using (Activity ambient = new Activity("ambient").SetIdFormat(ActivityIdFormat.W3C).Start())
                    {
                        await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);
                        AssertEqual(ambient.Id, server.LastRequestHeaders?["traceparent"], "ambient traceparent");
                    }
                }).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task WithoutRuntimePropagationAsync(Func<Task> action)
        {
            DistributedContextPropagator original = DistributedContextPropagator.Current;
            DistributedContextPropagator.Current = DistributedContextPropagator.CreateNoOutputPropagator();
            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                DistributedContextPropagator.Current = original;
            }
        }

        private static async Task TestTelemetryObjectKeysOptInAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, true, async (server, httpClient, client, ct) =>
            {
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);
                    Activity span = capture.Spans(S3LiteTelemetryNames.OperationSpanPrefix + S3LiteTelemetryNames.OperationGetObject).Single();
                    AssertNull(span.GetTagItem(S3LiteTelemetryNames.AttributeKey), "aws.s3.key when IncludeObjectKeys is false");
                }

                client.Telemetry.IncludeObjectKeys = true;

                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);
                    await AssertThrowsAsync<WebException>(() => client.Object.GetAsync(_SyntheticBucketName, _SyntheticMissingObjectKey, token: ct)).ConfigureAwait(false);

                    Activity ok = capture.Spans(S3LiteTelemetryNames.OperationSpanPrefix + S3LiteTelemetryNames.OperationGetObject).First(a => a.Status == ActivityStatusCode.Ok);
                    AssertEqual(_SyntheticHelloKey, ok.GetTagItem(S3LiteTelemetryNames.AttributeKey) as string, "aws.s3.key when IncludeObjectKeys is true");

                    Activity failed = capture.Spans(S3LiteTelemetryNames.OperationSpanPrefix + S3LiteTelemetryNames.OperationGetObject).First(a => a.Status == ActivityStatusCode.Error);
                    AssertTrue(failed.Events.Single(e => e.Name == "exception").Tags.Any(t => t.Key == "exception.message"), "exception.message when IncludeObjectKeys is true");

                    AssertFalse(capture.Measurements.Any(m => m.Tags.ContainsKey(S3LiteTelemetryNames.AttributeKey)), "keys must never be metric labels");
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryTogglesAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, true, async (server, httpClient, client, ct) =>
            {
                client.Telemetry.EnableTraces = false;
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);
                    AssertEqual(0, capture.Activities.Count, "spans with traces disabled");
                    AssertEqual(1, capture.For(S3LiteTelemetryNames.Operations).Count, "operations with traces disabled");
                    AssertTrue(capture.For(S3LiteTelemetryNames.StageDuration).Count >= 2, "stage metrics with traces disabled");
                }

                client.Telemetry.EnableTraces = true;
                client.Telemetry.EnableMetrics = false;
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);
                    AssertEqual(1, capture.Spans(S3LiteTelemetryNames.OperationSpanPrefix + S3LiteTelemetryNames.OperationGetObject).Count, "spans with metrics disabled");
                    AssertEqual(0, capture.Measurements.Count, "measurements with metrics disabled");
                }

                client.Telemetry.EnableMetrics = true;
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture(listenToTraces: false, listenToMetrics: true))
                {
                    await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);
                    AssertEqual(0, capture.Activities.Count, "spans with no trace listener");
                    AssertEqual(1, capture.For(S3LiteTelemetryNames.Operations).Count, "operations with no trace listener");
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryBuildInfoAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, true, async (server, httpClient, client, ct) =>
            {
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    await client.Object.GetAsync(_SyntheticBucketName, _SyntheticHelloKey, token: ct).ConfigureAwait(false);
                    capture.RecordObservableInstruments();

                    S3LiteCapturedMeasurement info = capture.For(S3LiteTelemetryNames.BuildInfo).Single();
                    AssertEqual(1, (int)info.Value, "build info value");
                    string expectedVersion = typeof(S3Client).Assembly.GetName().Version!.ToString(3);
                    AssertEqual(expectedVersion, info.Tag(S3LiteTelemetryNames.AttributeVersion), "build info version");
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task TestTelemetryLabelsAreBoundedAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithSyntheticClientAsync(RequestTransportMode.Internal, true, async (server, httpClient, client, ct) =>
            {
                using (S3LiteTelemetryCapture capture = new S3LiteTelemetryCapture())
                {
                    string key = GenerateObjectKey();
                    await client.Object.WriteAsync(_SyntheticBucketName, key, GetBytes("x"), token: ct).ConfigureAwait(false);
                    await client.Object.GetAsync(_SyntheticBucketName, key, token: ct).ConfigureAwait(false);
                    await AssertThrowsAsync<WebException>(() => client.Object.GetAsync(_SyntheticBucketName, _SyntheticMissingObjectKey, token: ct)).ConfigureAwait(false);
                    await client.Object.DeleteMultipleAsync(_SyntheticBucketName, new[] { key, _SyntheticMissingObjectKey }, token: ct).ConfigureAwait(false);

                    HashSet<string> allowed = new HashSet<string>
                    {
                        S3LiteTelemetryNames.AttributeOperation,
                        S3LiteTelemetryNames.AttributeOutcome,
                        S3LiteTelemetryNames.AttributeErrorType,
                        S3LiteTelemetryNames.AttributeStage,
                        S3LiteTelemetryNames.AttributeServerAddress,
                        S3LiteTelemetryNames.AttributeServerPort,
                        S3LiteTelemetryNames.AttributeVersion
                    };

                    AssertTrue(capture.Measurements.Count > 0, "measurements captured");
                    foreach (S3LiteCapturedMeasurement m in capture.Measurements)
                    {
                        foreach (string tagKey in m.Tags.Keys)
                        {
                            AssertTrue(allowed.Contains(tagKey), "Unexpected metric label '" + tagKey + "' on " + m.Name + ".");
                        }

                        foreach (object? value in m.Tags.Values)
                        {
                            string text = Convert.ToString(value) ?? String.Empty;
                            AssertFalse(text.Contains(key) || text.Contains(_SyntheticBucketName), "Metric label on " + m.Name + " carries a bucket or key.");
                        }
                    }
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private static void AssertErrorRecorded(S3LiteTelemetryCapture capture, string operation, string expectedErrorType)
        {
            Activity span = capture.Spans(S3LiteTelemetryNames.OperationSpanPrefix + operation).Single();
            AssertEqual(ActivityStatusCode.Error.ToString(), span.Status.ToString(), "span status for " + operation);
            AssertEqual(S3LiteTelemetryNames.OutcomeError, span.GetTagItem(S3LiteTelemetryNames.AttributeOutcome) as string, "span outcome for " + operation);
            AssertEqual(expectedErrorType, span.GetTagItem(S3LiteTelemetryNames.AttributeErrorType) as string, "span error.type for " + operation);
            AssertTrue(span.Events.Any(e => e.Name == "exception"), "exception event for " + operation);

            S3LiteCapturedMeasurement count = capture.For(S3LiteTelemetryNames.Operations).Single(m => m.Tag(S3LiteTelemetryNames.AttributeOperation) == operation);
            AssertEqual(S3LiteTelemetryNames.OutcomeError, count.Tag(S3LiteTelemetryNames.AttributeOutcome), "metric outcome for " + operation);
            AssertEqual(expectedErrorType, count.Tag(S3LiteTelemetryNames.AttributeErrorType), "metric error.type for " + operation);

            S3LiteCapturedMeasurement duration = capture.For(S3LiteTelemetryNames.OperationDuration).Single(m => m.Tag(S3LiteTelemetryNames.AttributeOperation) == operation);
            AssertEqual(expectedErrorType, duration.Tag(S3LiteTelemetryNames.AttributeErrorType), "duration error.type for " + operation);
        }

        private static int GetUnusedPort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
