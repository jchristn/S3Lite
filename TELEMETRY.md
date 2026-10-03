# S3Lite Telemetry

S3Lite measures every S3 call it makes and hands the numbers to whatever your application already uses for observability. It emits metrics through a BCL `System.Diagnostics.Metrics.Meter` and traces through a `System.Diagnostics.ActivitySource`, both named `S3Lite`, and stops there. S3Lite takes no dependency on OpenTelemetry, Radiant, or any exporter, and never opens a connection to a telemetry backend. **S3Lite emits, your host collects.**

When nothing subscribes, an operation checks two flags and moves on: no spans are created, no tags are allocated, no measurements are recorded.

With a collector attached, an on-call engineer can answer two questions from dashboards and traces alone:

- **Where did the time go?** Each operation's duration is split into `sign`, `send`, and `response` stages, both as histograms and as child spans.
- **What failed?** Every failure carries a bounded `error.type`: the S3 error code (`NoSuchKey`, `AccessDenied`, `SlowDown`, ...), the HTTP status when the response had no body, `no_response`, or the exception type. The span also carries the `x-amz-request-id` for AWS support cases.

---

## Contents

1. [Subscribing](#subscribing)
2. [Configuration](#configuration)
3. [Metrics catalog](#metrics-catalog)
4. [Spans catalog](#spans-catalog)
5. [Context propagation](#context-propagation)
6. [Grafana dashboard](#grafana-dashboard)
7. [Recommended alerts](#recommended-alerts)
8. [Privacy and cardinality](#privacy-and-cardinality)
9. [What is not covered](#what-is-not-covered)

---

## Subscribing

The strings below are the entire contract. Use the constants on `S3LiteTelemetryNames` instead of literals.

| Signal | Name | Constant |
|---|---|---|
| Meter | `S3Lite` | `S3LiteTelemetryNames.MeterName` |
| ActivitySource | `S3Lite` | `S3LiteTelemetryNames.ActivitySourceName` |

### With Radiant

```csharp
RadiantSettings settings = new RadiantSettings("my-service");
settings.Sources.AddMeter(S3LiteTelemetryNames.MeterName);
settings.Sources.AddActivitySource(S3LiteTelemetryNames.ActivitySourceName);
// also add "Watson" and your own sources if you host a Watson service

using (RadiantHost host = RadiantHost.Start(settings))
{
    // run the app; every S3Client in the process is now observed
}
```

### With the OpenTelemetry SDK

```csharp
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using MeterProvider meters = Sdk.CreateMeterProviderBuilder()
    .AddMeter("S3Lite")
    .AddOtlpExporter()          // or .AddPrometheusExporter()
    .Build();

using TracerProvider traces = Sdk.CreateTracerProviderBuilder()
    .AddSource("S3Lite")
    .AddOtlpExporter()
    .Build();
```

### In a unit test

`MeterListener` and `ActivityListener` work directly. See `src/Test.Shared/S3LiteTelemetryCapture.cs` for a complete in-memory capture used by the test suite.

### Target frameworks

On `net8.0` and `net10.0` the meter and activity source come from the shared framework. On `netstandard2.0` and `netstandard2.1`, S3Lite references the `System.Diagnostics.DiagnosticSource` package (10.0.12), which is the only dependency this feature adds.

---

## Configuration

Each `S3Client` carries a `TelemetrySettings` instance in `client.Telemetry`. You can replace it with `client.WithTelemetry(new TelemetrySettings { ... })`. Setting it to null throws `ArgumentNullException`.

| Property | Default | Effect |
|---|---|---|
| `Enable` | `true` | Master switch. When false, this client emits no metrics, no spans, and no propagation headers. |
| `EnableMetrics` | `true` | Emit measurements on the `S3Lite` meter. |
| `EnableTraces` | `true` | Emit spans on the `S3Lite` activity source. |
| `PropagateContext` | `true` | Inject W3C `traceparent` / `tracestate` of the current activity into outbound requests (after signing). |
| `IncludeObjectKeys` | `false` | Record `aws.s3.key` and `exception.message` (which embeds the request URL) on spans. Keys never go on metrics. |

```csharp
S3Client client = new S3Client()
    .WithRegion("us-west-1")
    .WithAccessKey(accessKey)
    .WithSecretKey(secretKey)
    .WithTelemetry(new TelemetrySettings
    {
        IncludeObjectKeys = false,   // default; keys can contain user data
        PropagateContext = true
    });
```

There are no environment variables or config-file keys. Exporter endpoints, service names, and sampling belong to the host's collector (Radiant `RadiantSettings`, OpenTelemetry SDK, etc.).

---

## Metrics catalog

Instrument names are dotted. A Prometheus exporter rewrites them to snake case and adds unit and type suffixes, shown in the last column. Label keys are rewritten the same way (`s3.operation` becomes `s3_operation`).

| Instrument | Type | Unit | Labels | Prometheus name |
|---|---|---|---|---|
| `s3lite.client.operation.duration` | Histogram | `s` | `s3.operation`, `outcome`, `error.type`*, `server.address`, `server.port` | `s3lite_client_operation_duration_seconds` |
| `s3lite.client.operations` | Counter | `{operation}` | `s3.operation`, `outcome`, `error.type`*, `server.address`, `server.port` | `s3lite_client_operations_total` |
| `s3lite.client.active_operations` | UpDownCounter | `{operation}` | `s3.operation`, `server.address`, `server.port` | `s3lite_client_active_operations` |
| `s3lite.client.stage.duration` | Histogram | `s` | `s3.operation`, `s3lite.stage`, `outcome` | `s3lite_client_stage_duration_seconds` |
| `s3lite.client.stages` | Counter | `{stage}` | `s3.operation`, `s3lite.stage`, `outcome` | `s3lite_client_stages_total` |
| `s3lite.client.request.body.size` | Histogram | `By` | `s3.operation` | `s3lite_client_request_body_size_bytes` |
| `s3lite.client.response.body.size` | Histogram | `By` | `s3.operation` | `s3lite_client_response_body_size_bytes` |
| `s3lite.client.objects.listed` | Counter | `{object}` | none | `s3lite_client_objects_listed_total` |
| `s3lite.client.batch_delete.objects` | Counter | `{object}` | `outcome` (`deleted`/`failed`), `error.type`* | `s3lite_client_batch_delete_objects_total` |
| `s3lite.client.batch_delete.size` | Histogram | `{object}` | none | `s3lite_client_batch_delete_size` |
| `s3lite.build.info` | Observable gauge | none | `s3lite.version` | `s3lite_build_info` |

\* `error.type` is present only on failures.

### Label values

| Label | Values |
|---|---|
| `s3.operation` | `ListBuckets`, `HeadBucket`, `ListObjectsV2`, `CreateBucket`, `DeleteBucket`, `HeadObject`, `GetObject`, `PutObject`, `DeleteObject`, `DeleteObjects` |
| `outcome` | `success`, `not_found` (existence checks that returned 404; not an error), `error`, `cancelled` |
| `error.type` | A recognized S3 error code (`NoSuchKey`, `NoSuchBucket`, `AccessDenied`, `SlowDown`, ...); an HTTP status code (`404`, `503`) when there was no parseable body (for example HEAD); `no_response`; or an exception type name (`System.Net.Http.HttpRequestException`, `System.Threading.Tasks.TaskCanceledException`). Unrecognized S3 codes in batch-delete results map to `_OTHER`. |
| `s3lite.stage` | `sign` (SigV4 signing, skipped in anonymous mode), `send` (HTTP round trip to response headers), `response` (reading and deserializing the body; ListBuckets, ListObjectsV2, GetObject, DeleteObjects) |
| `server.address` / `server.port` | The configured `Hostname` / `Port` (the logical endpoint, not the bucket-prefixed or gateway host) |

### Public API to operation mapping

| API | `s3.operation` |
|---|---|
| `Service.ListBucketsAsync` | `ListBuckets` |
| `Bucket.ExistsAsync` | `HeadBucket` |
| `Bucket.ListAsync` | `ListObjectsV2` |
| `Bucket.WriteAsync` | `CreateBucket` |
| `Bucket.DeleteAsync` | `DeleteBucket` |
| `Object.ExistsAsync`, `Object.GetMetadataAsync` | `HeadObject` |
| `Object.GetAsync` | `GetObject` |
| `Object.WriteAsync` | `PutObject` |
| `Object.DeleteAsync` | `DeleteObject` |
| `Object.DeleteMultipleAsync` (both overloads) | `DeleteObjects` |

Argument validation failures (`ArgumentNullException`, `ArgumentOutOfRangeException`, ...) are thrown before the operation starts and are not counted.

---

## Spans catalog

One **client** span per operation, with one **internal** child span per stage. When the caller already has an activity (a Watson request span, a pipeline stage, etc.), the S3 span nests under it, giving one trace from the inbound request down to the S3 call.

```
GET /documents/{id}                   (Watson server span, if present)
  S3 GetObject                        client
    stage:sign                        internal
    stage:send                        internal   <- traceparent sent upstream points here
      GET (System.Net.Http span, if the host subscribes to it)
    stage:response                    internal
```

### `S3 <Operation>` (kind: Client)

| Attribute | Example | Notes |
|---|---|---|
| `rpc.system` | `aws-api` | |
| `rpc.service` | `S3` | |
| `rpc.method`, `s3.operation` | `GetObject` | |
| `http.request.method` | `GET` | |
| `http.response.status_code` | `200` | Set when a response was received |
| `server.address`, `server.port` | `s3.us-west-1.amazonaws.com`, `443` | Configured endpoint |
| `url.scheme` | `https` | |
| `aws.s3.bucket` | `my-bucket` | |
| `aws.s3.key` | `photos/1.jpg` | **Only** when `IncludeObjectKeys` is true |
| `aws.request_id` | `4442587FB7D0A2F9` | From `x-amz-request-id` or the error body |
| `aws.s3.error_code` | `NoSuchKey` | Failures with an S3 error body |
| `http.request.body.size`, `http.response.body.size` | `4096` | Bytes |
| `s3lite.request_style` | `PathStyle` | |
| `s3lite.auth_mode` | `signed` / `anonymous` | |
| `s3lite.gateway` | `true` | Routed through `GatewayConfig` |
| `s3lite.list.count`, `s3lite.list.truncated` | `1000`, `true` | ListObjectsV2 only |
| `s3lite.delete.requested`, `s3lite.delete.deleted`, `s3lite.delete.failed` | `3`, `2`, `1` | DeleteObjects only |
| `outcome`, `error.type` | `error`, `NoSuchKey` | Same values as the metrics |

**Status:** `Ok` for `success` and `not_found`; `Error` (description = `error.type`) for `error` and `cancelled`. Failures add an `exception` event with `exception.type` and `exception.stacktrace`, plus `exception.message` only when `IncludeObjectKeys` is true.

### `stage:<name>` (kind: Internal)

Attributes: `s3.operation`, `s3lite.stage`, `outcome`. Status is `Error` with an `exception` event when the stage threw (for example a connection failure in `send` or a malformed body in `response`). An HTTP error response does **not** fail the `send` stage, because the round trip itself completed. The failure is recorded on the operation span.

---

## Context propagation

With `PropagateContext` on (the default), S3Lite adds `traceparent` (and `tracestate` when present) for `Activity.Current` to every outbound request. The header is added **after** the SigV4 signature is computed, so it is never a signed header and cannot break signature validation at AWS, MinIO, Less3, or a gateway. S3Lite injects only W3C-format activities and never overwrites a `traceparent` the caller supplied in `headers`.

On .NET 8+, `HttpClient` also propagates on its own. S3Lite's injection happens first and the runtime keeps the existing value. On `netstandard` hosts such as .NET Framework, S3Lite's injection is what connects the S3 call to the caller's trace.

---

## Grafana dashboard

S3Lite is a library, so it ships no compose stack. The host application owns Prometheus, Tempo, and Grafana. An importable dashboard is included at **`Assets/grafana/s3lite-client.json`** (uid `s3lite-client`). It expects a Prometheus datasource with uid `prometheus`, and its header link to failed spans expects a Tempo datasource with uid `tempo`. These match the provisioning convention used by Pneuma.

Drop the file into your Grafana dashboards provisioning directory (for example next to your product's Integrations dashboard), or import it through the Grafana UI.

| Row | Panels | Question it answers |
|---|---|---|
| Overview | Operation rate, error ratio, p95 latency, in-flight operations | Is S3 healthy right now? |
| Operations | Rate by operation and outcome, errors by `error.type`, p95 by operation, p50/p95/p99 | Which operation is slow or failing, and why? |
| Stages | p95 per stage, p95 per stage by operation, stage failures | Is the time spent signing, on the wire, or reading the body? |
| Transfer and Domain | Bytes sent/received, batch-delete objects by outcome, objects listed, versions in use | How much data is moving, and are batch deletes partially failing? |

Variables: `$server` (filters on `server_address`) and `$operation` (filters on `s3_operation`).

---

## Recommended alerts

```yaml
groups:
  - name: s3lite
    rules:
      - alert: S3LiteErrorRatioHigh
        expr: |
          sum by (job, server_address) (rate(s3lite_client_operations_total{outcome="error"}[5m]))
            / clamp_min(sum by (job, server_address) (rate(s3lite_client_operations_total[5m])), 1e-9) > 0.05
        for: 10m
        labels: { severity: warning }
        annotations:
          summary: "More than 5% of S3 calls from {{ $labels.job }} to {{ $labels.server_address }} are failing"

      - alert: S3LiteLatencyHigh
        expr: |
          histogram_quantile(0.95,
            sum by (le, job, s3_operation) (rate(s3lite_client_operation_duration_seconds_bucket[5m]))) > 2
        for: 10m
        labels: { severity: warning }
        annotations:
          summary: "p95 {{ $labels.s3_operation }} latency above 2s for {{ $labels.job }}"

      - alert: S3LiteThrottled
        expr: sum by (job) (rate(s3lite_client_operations_total{error_type=~"SlowDown|503"}[5m])) > 0
        for: 5m
        labels: { severity: warning }
        annotations:
          summary: "{{ $labels.job }} is being throttled by S3 (SlowDown / 503)"

      - alert: S3LiteAccessDenied
        expr: sum by (job) (increase(s3lite_client_operations_total{error_type=~"AccessDenied|403|InvalidAccessKeyId|SignatureDoesNotMatch"}[15m])) > 0
        labels: { severity: critical }
        annotations:
          summary: "{{ $labels.job }} is getting authorization failures from S3 (credentials or policy)"

      - alert: S3LiteUnreachable
        expr: sum by (job, server_address) (rate(s3lite_client_operations_total{error_type=~"no_response|System.Net.Http.HttpRequestException"}[5m])) > 0
        for: 5m
        labels: { severity: critical }
        annotations:
          summary: "{{ $labels.job }} cannot reach S3 endpoint {{ $labels.server_address }}"

      - alert: S3LiteBatchDeletePartialFailures
        expr: sum by (job, error_type) (increase(s3lite_client_batch_delete_objects_total{outcome="failed"}[15m])) > 0
        labels: { severity: info }
        annotations:
          summary: "DeleteObjects calls from {{ $labels.job }} are leaving objects behind ({{ $labels.error_type }})"
```

Thresholds are starting points. Tune them to your workload's object sizes and endpoint.

---

## Privacy and cardinality

- **Metrics carry only bounded labels.** Operation, outcome, stage, error type, and the configured endpoint. Bucket names, object keys, request IDs, URLs, and version IDs never appear as metric labels. A test (`Telemetry.LabelsAreBounded`) enforces this.
- **Spans carry identifiers, not secrets.** Bucket name and request ID are recorded. Object keys and exception messages (which embed URLs) are opt-in via `IncludeObjectKeys`. Access keys, secret keys, signatures, `Authorization` headers, and payloads are never recorded anywhere.
- **Best-effort.** Every telemetry call is wrapped so that a listener or exporter failure can never change the result of an S3 call.
- **No in-process quantiles.** Durations are raw histogram measurements. Compute p50/p95/p99 in Prometheus/Grafana.

---

## What is not covered

- **Runtime and process metrics** (GC, thread pool, CPU) belong to the host. Enable them in Radiant or with `OpenTelemetry.Instrumentation.Runtime`.
- **Connection pool metrics** belong to `HttpClient` (`System.Net.Http` meter on .NET 8+). Subscribe to it in the host if you need socket-level detail.
- **Logs.** S3Lite has no background work. Its existing `Logger` callback remains for request-level debug logging. The callback runs while the S3 span is `Activity.Current`, so a host that routes it through `ILogger` with activity tracking enabled gets log lines correlated to the S3 trace.
- **Retries.** S3Lite does not retry, so there is no retry metric. Each caller-level retry shows up as a separate operation.
