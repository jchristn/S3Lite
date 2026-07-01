![S3Lite icon](https://github.com/jchristn/S3Lite/raw/main/Assets/icon.ico)

# S3Lite

Lightweight Amazon S3 and S3-compatible storage client for .NET.

[![NuGet Version](https://img.shields.io/nuget/v/S3Lite.svg?style=flat)](https://www.nuget.org/packages/S3Lite/) [![NuGet](https://img.shields.io/nuget/dt/S3Lite.svg)](https://www.nuget.org/packages/S3Lite)

S3Lite keeps the surface area small while still covering the core bucket and object operations most applications actually need. It targets AWS S3, Less3, MinIO, LocalStack, and other S3-compatible endpoints without dragging in the official AWS SDK.

## Why S3Lite

- Small dependency footprint
- Simple fluent client configuration
- AWS S3 and S3-compatible endpoint support
- Anonymous access support for public buckets
- Caller-supplied `HttpClient` support for DI, proxying, custom handlers, and connection reuse
- Reverse-proxy / gateway routing that keeps the SigV4 signature bound to the upstream endpoint (useful for non-standard corporate reverse proxy configurations)
- Multi-targeted package: `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`

## New in v1.1.0

- Upgraded to `RestWrapper` `v3.2.0`
- Added support for caller-supplied `HttpClient` instances
- Reworked the automated test infrastructure around Touchstone
- Added xUnit and NUnit runner projects alongside the console runner

## Installation

```bash
dotnet add package S3Lite
```

## Quick Start

### AWS S3 with Credentials

```csharp
using System;
using System.Text;
using S3Lite;
using S3Lite.ApiObjects;

S3Client s3 = new S3Client()
    .WithRegion("us-west-1")
    .WithAccessKey("AKIAIOSFODNN7EXAMPLE")
    .WithSecretKey("wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY")
    .WithRequestStyle(RequestStyleEnum.VirtualHostedStyle)
    .WithLogger(Console.WriteLine);

ListAllMyBucketsResult buckets = await s3.Service.ListBucketsAsync();

await s3.Object.WriteAsync(
    "my-bucket",
    "hello.txt",
    Encoding.UTF8.GetBytes("hello from s3lite"),
    "text/plain");

byte[] data = await s3.Object.GetAsync("my-bucket", "hello.txt");
Console.WriteLine(Encoding.UTF8.GetString(data));
```

### Anonymous Access for Public Buckets

If a bucket is public, simply omit credentials:

```csharp
using System;
using S3Lite;
using S3Lite.ApiObjects;

S3Client s3 = new S3Client()
    .WithRegion("us-west-1")
    .WithRequestStyle(RequestStyleEnum.VirtualHostedStyle);

ListBucketResult result = await s3.Bucket.ListAsync("public-dataset-bucket");
Console.WriteLine(result.Contents.Count);
```

### S3-Compatible Storage

```csharp
using S3Lite;

S3Client s3 = new S3Client()
    .WithHostname("localhost")
    .WithPort(9000)
    .WithProtocol(ProtocolEnum.Http)
    .WithRegion("us-west-1")
    .WithRequestStyle(RequestStyleEnum.PathStyle)
    .WithAccessKey("minioadmin")
    .WithSecretKey("minioadmin");
```

## Bring Your Own HttpClient

`S3Lite` now exposes the caller-supplied `HttpClient` support added in `RestWrapper` `v3.2.0`. Use this when you already manage `HttpClient` instances through dependency injection, need a custom handler pipeline, or want to centralize transport settings.

```csharp
using System;
using System.Net.Http;
using S3Lite;

HttpClient httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromSeconds(30);

S3Client s3 = new S3Client(httpClient)
    .WithRegion("us-east-1")
    .WithHostname("s3.us-east-1.amazonaws.com")
    .WithRequestStyle(RequestStyleEnum.PathStyle)
    .WithAccessKey("AKIAIOSFODNN7EXAMPLE")
    .WithSecretKey("wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");

bool exists = await s3.Bucket.ExistsAsync("my-bucket");
```

You can also attach one fluently:

```csharp
S3Client s3 = new S3Client()
    .WithHttpClient(httpClient)
    .WithRegion("us-east-1");
```

Notes:

- The caller owns the lifetime of the supplied `HttpClient`
- S3Lite does not dispose a caller-supplied `HttpClient`
- Transport behavior such as proxying, TLS, decompression, retries, and timeouts should be configured on your `HttpClient` or its handler

## Routing Through a Reverse Proxy / Gateway

Some environments require S3 traffic to egress through an intermediate host that forwards to the real endpoint. For example, `s3.gateway.example.com` can be configured to act as a reverse proxy to `s3.us-west-1.amazonaws.com`.

SigV4 binds the signature to the request `Host`, so simply pointing the client at the gateway makes it sign for the gateway; once the gateway rewrites `Host` to the upstream endpoint, the signature no longer validates.

`GatewayConfig` solves this. When set, S3Lite sends the request to the gateway while the SigV4 signature stays bound to the upstream endpoint configured via `WithHostname`. Attach it with `WithGateway`:

```csharp
using S3Lite;

S3Client s3 = new S3Client()
    .WithRegion("us-west-1")
    .WithHostname("s3.us-west-1.amazonaws.com")   // the real upstream endpoint the signature is bound to
    .WithPort(443)
    .WithProtocol(ProtocolEnum.Https)
    .WithRequestStyle(RequestStyleEnum.PathStyle)
    .WithAccessKey("AKIAIOSFODNN7EXAMPLE")
    .WithSecretKey("wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY")
    .WithGateway(new GatewayConfig
    {
        Hostname = "s3.gateway.example.com",      // gateway host only, no scheme or port
        Port = 8443,                              // omit (or 0) to reuse the client Port
        Protocol = ProtocolEnum.Https             // omit (or null) to reuse the client Protocol
    });

bool exists = await s3.Bucket.ExistsAsync("my-bucket");
```

How it works:

- The connection (scheme, host, port) is created for `https://s3.gateway.example.com`, so the gateway receives `Host=s3.gateway.example.com`.
- The signature is computed for the **upstream request authority**, which is not necessarily `Hostname` verbatim. For `PathStyle` it is the endpoint (for example `s3.us-west-1.amazonaws.com`, including a non-standard port when configured); for `VirtualHostedStyle` it is bucket-prefixed (for example `my-bucket.s3.us-west-1.amazonaws.com`).
- The gateway is expected to rewrite `Host` back to that upstream authority before forwarding upstream, so the signature validates. The path, query, and body are unchanged.

`GatewayConfig.Hostname` must be the **gateway host only** — supply the protocol via `GatewayConfig.Protocol` and the port via `GatewayConfig.Port` separately. When `Protocol` is null or `Port` is 0, the client's own `Protocol` / `Port` values are reused. Leave `Gateway` unset (or `Hostname` empty) to send requests directly to `Hostname`.

This is **not** a forward / HTTP (`CONNECT`) proxy. A forward proxy preserves the upstream `Host` end-to-end and needs no rewrite; to use one, leave `Gateway` unset and supply an `HttpClient` configured with a `WebProxy` via [`WithHttpClient`](#bring-your-own-httpclient) instead.

## Common Operations

### Service APIs

```csharp
ListAllMyBucketsResult buckets = await s3.Service.ListBucketsAsync();
```

### Bucket APIs

```csharp
bool exists = await s3.Bucket.ExistsAsync("my-bucket");

await s3.Bucket.WriteAsync("my-bucket", "us-west-1");

ListBucketResult objects = await s3.Bucket.ListAsync("my-bucket");

ListBucketResult filtered = await s3.Bucket.ListAsync("my-bucket", prefix: "images/");

ListBucketResult page = await s3.Bucket.ListAsync("my-bucket", continuationToken: "token-value", maxKeys: 100);

await s3.Bucket.DeleteAsync("my-bucket");
```

### Object APIs

```csharp
await s3.Object.WriteAsync("my-bucket", "notes/hello.txt", Encoding.UTF8.GetBytes("hello"));

bool exists = await s3.Object.ExistsAsync("my-bucket", "notes/hello.txt");

ObjectMetadata metadata = await s3.Object.GetMetadataAsync("my-bucket", "notes/hello.txt");

byte[] data = await s3.Object.GetAsync("my-bucket", "notes/hello.txt");

await s3.Object.DeleteAsync("my-bucket", "notes/hello.txt");
```

## Endpoint Guidance

The right hostname depends on the request style you choose:

| Request Style | Typical Hostname | Example URL |
|---|---|---|
| `VirtualHostedStyle` | `amazonaws.com` | `https://mybucket.s3.us-west-1.amazonaws.com/mykey` |
| `PathStyle` | `s3.us-west-1.amazonaws.com` | `https://s3.us-west-1.amazonaws.com/mybucket/mykey` |

For S3-compatible platforms such as Less3, MinIO, or LocalStack, point `Hostname` and `Port` at your service endpoint and usually prefer `PathStyle`.

## Key Client Properties

| Property | Description |
|---|---|
| `AccessKey` | Access key, or null for anonymous mode |
| `SecretKey` | Secret key, or null for anonymous mode |
| `HasCredentials` | True when both access key and secret key are configured |
| `Region` | Region used in request signing and URL construction |
| `Hostname` | Endpoint hostname |
| `Port` | Endpoint port |
| `Protocol` | `Http` or `Https` |
| `RequestStyle` | `VirtualHostedStyle` or `PathStyle` |
| `SignatureVersion` | Signature version used by the client |
| `HttpClient` | Optional caller-supplied `HttpClient` instance |
| `Gateway` | Optional `GatewayConfig` for reverse-proxy / gateway routing while signing for the upstream `Hostname` |
| `Logger` | Optional request logger callback |

## Error Behavior

S3Lite throws `WebException` for failed requests and includes useful context in the exception `Data` collection, including:

- `StatusCode`
- `URL`
- `RequestBody`
- `ResponseBody`
- S3 error metadata such as `RequestId`, `VersionId`, `Resource`, and `ErrorCode` when available

## Automated Testing

The repository now uses Touchstone so the same shared descriptors can run through multiple hosts:

- `src/Test.Shared`: shared Touchstone descriptors and test configuration
- `src/Test.Automated`: console runner using `Touchstone.Cli`
- `src/Test.Xunit`: xUnit adapter host
- `src/Test.Nunit`: NUnit adapter host

### Run the Console Runner

```bash
dotnet run --framework net8.0 --project src/Test.Automated -- -b my-bucket -a ACCESS_KEY -s SECRET_KEY
```

Optional arguments:

- `--endpoint <host>`
- `--port <port>`
- `--region <region>`
- `--http`
- `--https`
- `--path-style`
- `--virtual-hosted`
- `--verbose`
- `--skip-cleanup`
- `--skip-write-tests`
- `--results <path>`

### Run xUnit and NUnit

```bash
dotnet test src/Test.Xunit/Test.Xunit.csproj
dotnet test src/Test.Nunit/Test.Nunit.csproj
```

These runners read the same configuration from environment variables:

- `S3LITE_TEST_ENDPOINT`
- `S3LITE_TEST_PORT`
- `S3LITE_TEST_REGION`
- `S3LITE_TEST_ACCESS_KEY`
- `S3LITE_TEST_SECRET_KEY`
- `S3LITE_TEST_BUCKET`
- `S3LITE_TEST_PROTOCOL`
- `S3LITE_TEST_REQUEST_STYLE`
- `S3LITE_TEST_VERBOSE`
- `S3LITE_TEST_SKIP_CLEANUP`
- `S3LITE_TEST_SKIP_WRITE_TESTS`

## Example Projects

- `src/Test.S3`: interactive AWS S3 example
- `src/Test.S3Compatible`: interactive S3-compatible example
- `src/Test.Script`: script-style object hierarchy walkthrough
- `src/Test.LargeEnumeration`: large-listing exercise

## Feedback and Enhancements

Encounter an issue or have an enhancement request? Please open an issue or start a discussion in the repository.

## Version History

See [CHANGELOG.md](CHANGELOG.md) for release details.
