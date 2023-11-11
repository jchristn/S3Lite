using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S3Lite
{
    internal static class Constants
    {
        internal static string DatestampFormatCompact = "yyyyMMdd";
        internal static string TimestampFormatVerbose = "ddd, dd MMM yyy HH:mm:ss zzz";
        internal static string TimestampFormatCompact = "yyyyMMddTHHmmssZ";

        internal static string EmptySha256Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        internal static string HeaderHost = "host";
        internal static string HeaderAmazonContentSha256 = "x-amz-content-sha256";
        internal static string HeaderAmazonRequestId = "x-amz-request-id";
        internal static string HeaderAmazonRequestId2 = "x-amz-id-2";
        internal static string HeaderAmazonDate = "x-amz-date";
        internal static string HeaderAmazonBucketRegion = "x-amz-bucket-region";
        internal static string HeaderContentType = "Content-Type";
        internal static string HeaderUserAgent = "User-Agent";
        internal static string HeaderUserAgentValue = "S3Lite";
        internal static string HeaderContentMd5 = "Content-Md5";

        internal static string ContentTypeXml = "application/xml";

        internal static string UrlPatternPathStyle = "{protocol}://{hostname}:{port}/{bucket}/{key}";
        internal static string UrlPatternVirtualHosted = "{protocol}://{bucket}.{service}.{region}.{hostname}:{port}/{key}";

        // AWS AKIAIOSFODNN7EXAMPLE:bV8+DXvs668hdEb4hCMlaBmBO10= (using secret key wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY)
        internal static string AuthorizationStringV2Pattern = "AWS {accesskey}:{signaturebase64}";

        // AWS4-HMAC-SHA256 Credential={{accesskey}}/20130524/us-east-1/s3/aws4_request, SignedHeaders=host;range;x-amz-date, Signature=fe5f80f77d5fa3beca038a248ff027d0445342fe2855ddc963176630326f1024
        internal static string AuthorizationStringV4Pattern = "AWS4-HMAC-SHA256 Credential={accesskey}/{datecompact}/{region}/s3/aws4_request, SignedHeaders={signedheaders}, Signature={signaturehex}";

        internal static string NoRestResponseExceptionMessage = "Unable to connect to the specified URL: ";
        internal static string RestRequestFailedExceptionMessage = "The request to the following URL failed: ";
    }
}
