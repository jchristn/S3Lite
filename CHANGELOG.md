# Changelog


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
