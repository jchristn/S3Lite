![alt tag](https://github.com/jchristn/S3Lite/raw/main/Assets/icon.ico)

# S3Lite

Simple AWS S3 client library without all of the heft and dependency drag of the official library.

[![NuGet Version](https://img.shields.io/nuget/v/S3Lite.svg?style=flat)](https://www.nuget.org/packages/S3Lite/) [![NuGet](https://img.shields.io/nuget/dt/S3Lite.svg)](https://www.nuget.org/packages/S3Lite) 

## Feedback and Enhancements

Encounter an issue or have an enhancement request?  Please file an issue or start a discussion here!

## New in v1.0.x

- Initial release

## Examples

Refer to the ```Test.S3``` and ```Test.S3Compatible``` projects for full examples.

### Create for AWS S3
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
```

### Create for S3-Compatible Storage
```csharp
using S3Lite;
using S3Lite.ApiObjects;

S3Client s3 = new S3Client()
  .WithRegion("us-west-1")
  .WithAccessKey("AKIAIOSFODNN7EXAMPLE")
  .WithSecretKey("wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY")
  .WithHostname("localhost")
  .WithPort(9000)
  .WithProtocol(ProtocolEnum.Http)
  .WithRequestStyle(RequestStyleEnum.PathStyle)
  .WithLogger(Console.WriteLine);
```

### Example APIs
```csharp
using S3Lite;
using S3Lite.ApiObjects;

// Service APIs
ListAllMyBucketsResult buckets = await s3.Service.ListBucketsAsync();

// Bucket APIs
bool exists = await s3.Bucket.ExistsAsync("bucket1");
await s3.Bucket.Write("bucket2", "us-west-1");
ListBucketResult objects = await s3.Bucket.ListAsync();
await s3.Bucket.DeleteAsync("bucket2");

// Object APIs
await s3.Object.WriteAsync("bucket1", "myobject", Encoding.UTF8.GetBytes("Hello, world!"));
bool exists = await s3.Object.ExistsAsync("bucket1", "myobject");
byte[] data = await s3.Object.ReadAsync("bucket1", "myobject");
await s3.Object.DeleteAsync("bucket1", "myobject");
```

## Version History

Refer to CHANGELOG.md for details.
