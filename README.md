![alt tag](https://github.com/jchristn/S3Lite/raw/main/Assets/icon.ico)

# S3Lite

Simple AWS S3 client library without all of the heft and dependency drag of the official library.

[![NuGet Version](https://img.shields.io/nuget/v/S3Lite.svg?style=flat)](https://www.nuget.org/packages/S3Lite/) [![NuGet](https://img.shields.io/nuget/dt/S3Lite.svg)](https://www.nuget.org/packages/S3Lite)

## Feedback and Enhancements

Encounter an issue or have an enhancement request?  Please file an issue or start a discussion here!

## New in v1.0.x

- Initial release
- Anonymous access support (for public buckets that don't require authentication)

## Examples

Refer to the ```Test.S3```, ```Test.S3Compatible```, and ```Test.Automated``` projects for full examples.

### Client Configuration

#### AWS S3 with Credentials (Virtual-Hosted Style)

```csharp
using S3Lite;
using S3Lite.ApiObjects;

S3Client s3 = new S3Client()
  .WithRegion("us-west-1")
  .WithAccessKey("AKIAIOSFODNN7EXAMPLE")
  .WithSecretKey("wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY")
  .WithRequestStyle(RequestStyleEnum.VirtualHostedStyle)
  .WithSignatureVersion(SignatureVersionEnum.Version4)
  .WithLogger(Console.WriteLine);

// Check if credentials are configured
Console.WriteLine("Has credentials: " + s3.HasCredentials);  // True
```

#### AWS S3 with Anonymous Access (Public Buckets)

For accessing public S3 buckets that don't require authentication, simply omit the access key and secret key:

```csharp
using S3Lite;
using S3Lite.ApiObjects;

S3Client s3 = new S3Client()
  .WithRegion("us-west-1")
  .WithRequestStyle(RequestStyleEnum.VirtualHostedStyle)
  .WithLogger(Console.WriteLine);

// Check if credentials are configured
Console.WriteLine("Has credentials: " + s3.HasCredentials);  // False

// Access public bucket contents
ListBucketResult objects = await s3.Bucket.ListAsync("public-dataset-bucket");
```

#### S3-Compatible Storage (MinIO, LocalStack, etc.)

```csharp
using S3Lite;
using S3Lite.ApiObjects;

S3Client s3 = new S3Client()
  .WithRegion("us-west-1")
  .WithAccessKey("minioadmin")
  .WithSecretKey("minioadmin")
  .WithHostname("localhost")
  .WithPort(9000)
  .WithProtocol(ProtocolEnum.Http)
  .WithRequestStyle(RequestStyleEnum.PathStyle)
  .WithLogger(Console.WriteLine);
```

### Endpoint Configuration

The endpoint hostname varies based on the request style:

| Request Style | Default Hostname | Example URL |
|--------------|------------------|-------------|
| VirtualHostedStyle | `amazonaws.com` | `https://mybucket.s3.us-west-1.amazonaws.com/mykey` |
| PathStyle | `s3.<region>.amazonaws.com` | `https://s3.us-west-1.amazonaws.com/mybucket/mykey` |

For S3-compatible storage, use the hostname of your storage server (e.g., `localhost` for local development).

### API Reference

#### Service APIs

```csharp
// List all buckets (requires authentication)
ListAllMyBucketsResult buckets = await s3.Service.ListBucketsAsync();
```

#### Bucket APIs

```csharp
// Check if bucket exists
bool exists = await s3.Bucket.ExistsAsync("mybucket");

// Create a bucket
await s3.Bucket.WriteAsync("mybucket", "us-west-1");

// List objects in a bucket
ListBucketResult objects = await s3.Bucket.ListAsync("mybucket");

// List objects with prefix filter
ListBucketResult filtered = await s3.Bucket.ListAsync("mybucket", prefix: "folder/");

// List with pagination (max 1000 keys per request)
ListBucketResult page1 = await s3.Bucket.ListAsync("mybucket", maxKeys: 100);
if (!string.IsNullOrEmpty(page1.NextContinuationToken))
{
    ListBucketResult page2 = await s3.Bucket.ListAsync("mybucket",
        continuationToken: page1.NextContinuationToken, maxKeys: 100);
}

// Delete a bucket (must be empty)
await s3.Bucket.DeleteAsync("mybucket");
```

#### Object APIs

```csharp
// Write an object
await s3.Object.WriteAsync("mybucket", "mykey", Encoding.UTF8.GetBytes("Hello, world!"));

// Write with content type
await s3.Object.WriteAsync("mybucket", "mykey.json", jsonBytes, "application/json");

// Check if object exists
bool exists = await s3.Object.ExistsAsync("mybucket", "mykey");

// Get object metadata
ObjectMetadata metadata = await s3.Object.GetMetadataAsync("mybucket", "mykey");

// Read an object
byte[] data = await s3.Object.GetAsync("mybucket", "mykey");

// Delete an object
await s3.Object.DeleteAsync("mybucket", "mykey");
```

### Method Signatures

#### ServiceApis

| Method | Parameters | Returns |
|--------|------------|---------|
| `ListBucketsAsync` | `headers`, `token` | `ListAllMyBucketsResult` |

#### BucketApis

| Method | Parameters | Returns |
|--------|------------|---------|
| `ExistsAsync` | `bucket`, `headers`, `token` | `bool` |
| `ListAsync` | `bucket`, `prefix`, `marker`, `continuationToken`, `maxKeys`, `headers`, `token` | `ListBucketResult` |
| `WriteAsync` | `bucket`, `region`, `headers`, `token` | `Task` |
| `DeleteAsync` | `bucket`, `headers`, `token` | `Task` |

#### ObjectApis

| Method | Parameters | Returns |
|--------|------------|---------|
| `ExistsAsync` | `bucket`, `key`, `versionId`, `headers`, `token` | `bool` |
| `GetMetadataAsync` | `bucket`, `key`, `versionId`, `headers`, `token` | `ObjectMetadata` |
| `GetAsync` | `bucket`, `key`, `versionId`, `headers`, `token` | `byte[]` |
| `WriteAsync` | `bucket`, `key`, `data`, `contentType`, `versionId`, `headers`, `token` | `Task` |
| `DeleteAsync` | `bucket`, `key`, `versionId`, `headers`, `token` | `Task` |

### S3Client Properties

| Property | Type | Description |
|----------|------|-------------|
| `AccessKey` | `string` | AWS access key (null for anonymous access) |
| `SecretKey` | `string` | AWS secret key (null for anonymous access) |
| `HasCredentials` | `bool` | True if both AccessKey and SecretKey are configured |
| `Region` | `string` | AWS region (default: `us-west-1`) |
| `Hostname` | `string` | S3 endpoint hostname (default: `amazonaws.com`) |
| `Port` | `int` | Port number (default: `443`) |
| `Protocol` | `ProtocolEnum` | `Http` or `Https` (default: `Https`) |
| `RequestStyle` | `RequestStyleEnum` | `VirtualHostedStyle` or `PathStyle` (default: `VirtualHostedStyle`) |
| `SignatureVersion` | `SignatureVersionEnum` | `Version2` or `Version4` (default: `Version4`) |

## Version History

Refer to CHANGELOG.md for details.
