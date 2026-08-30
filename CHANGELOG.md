# Changelog

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
