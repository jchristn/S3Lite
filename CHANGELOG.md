# Changelog

## v1.3.0 - 2026-10-02

- Added built-in observability through BCL `System.Diagnostics.Metrics.Meter` and `ActivitySource`, both named `S3Lite`, with no exporter dependency and near-zero cost when unobserved
- Added an `S3 <Operation>` client span per operation with `stage:sign`, `stage:send`, and `stage:response` child spans, explicit status, exception events, `aws.request_id`, and S3 error codes
- Added metrics: `s3lite.client.operation.duration`, `s3lite.client.operations`, `s3lite.client.active_operations`, `s3lite.client.stage.duration`, `s3lite.client.stages`, `s3lite.client.request.body.size`, `s3lite.client.response.body.size`, `s3lite.client.objects.listed`, `s3lite.client.batch_delete.objects`, `s3lite.client.batch_delete.size`, and `s3lite.build.info`
- Added W3C `traceparent` / `tracestate` injection on outbound requests, applied after signing so SigV4 is unaffected
- Added `TelemetrySettings` (`Enable`, `EnableMetrics`, `EnableTraces`, `PropagateContext`, `IncludeObjectKeys`), `S3Client.Telemetry`, `S3Client.WithTelemetry`, and the `S3LiteTelemetryNames` constants class
- Added `System.Diagnostics.DiagnosticSource` 10.0.12 as a dependency for the `netstandard2.0` and `netstandard2.1` targets only
- Added `TELEMETRY.md` and an importable Grafana dashboard at `Assets/grafana/s3lite-client.json`
- Added a Touchstone `Telemetry` suite covering every operation, outcome, failure path, propagation, toggles, and label cardinality

## v1.2.3 - 2026-08-30

- Added `Object.DeleteMultipleAsync` for S3 batch delete (`POST /{bucket}?delete`) with a `Content-MD5` header
- Two overloads: one accepting object keys (`IEnumerable<string>`) and one accepting `ObjectIdentifier` entries (for per-object version IDs)
- Added quiet mode and a `DeleteResult` return type reporting both deleted objects and per-object errors
- Added `ObjectIdentifier`, `DeleteMultipleRequest`, `DeleteResult`, `DeletedObject`, and `DeleteError` API objects
- Expanded the shared Touchstone suites with positive and negative batch-delete coverage (guards, synthetic internal/external transport, and live-endpoint)

## v1.2.0 - 2026-07-01

- Added reverse-proxy / gateway routing via `GatewayConfig` and `S3Client.WithGateway(GatewayConfig)`

## v1.1.0 - 2026-05-18

- Upgraded `RestWrapper` from `3.1.8` to `3.2.0`
- Added caller-supplied `HttpClient` support to `S3Client`
- Added `S3Client(HttpClient)` and `WithHttpClient(HttpClient)` entry points
- Reworked automated testing around Touchstone with a shared descriptor project
- Added `Test.Xunit` and `Test.Nunit` runner projects
- Expanded the README with clearer setup guidance, API examples, and test-runner documentation

## v1.0.x

- Initial release
- Anonymous access support for public buckets
