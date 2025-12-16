namespace Test.S3Compatible
{
    using System;
    using System.Text;
    using GetSomeInput;
    using S3Lite;
    using S3Lite.ApiObjects;

    static class Program
    {
        static string _Hostname = "localhost";
        static int _Port = 9000;
        static bool _Ssl = false;
        static ProtocolEnum _Protocol = ProtocolEnum.Http;
        static RequestStyleEnum _RequestStyle = RequestStyleEnum.PathStyle;

        static string _Region = "us-west-1";
        static S3Client _S3 = null;
        static string _AccessKey = "AKIAIOSFODNN7EXAMPLE";
        static string _SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        static string _Bucket = null;
        static string _TestObjectKey1 = "testkey1";
        static string _TestObjectKey2 = "testkey2";

        static void Main(string[] args)
        {
            _Region =    Inputty.GetString("Region     :", _Region, false);
            _Hostname =  Inputty.GetString("Hostname   :", _Hostname, false);
            _Port =     Inputty.GetInteger("Port       :", _Port, true, true);
            _Ssl =      Inputty.GetBoolean("SSL        :", _Ssl);
            _AccessKey = Inputty.GetString("Access key :", _AccessKey, false);
            _SecretKey = Inputty.GetString("Secret key :", _SecretKey, false);
            _Bucket = Inputty.GetString("Bucket     :", _Bucket, false);

            if (_Ssl) _Protocol = ProtocolEnum.Https;
            else _Protocol = ProtocolEnum.Http;

            _S3 = new S3Client()
                .WithRegion(_Region)
                .WithAccessKey(_AccessKey)
                .WithSecretKey(_SecretKey)
                .WithHostname(_Hostname)
                .WithPort(_Port)
                .WithProtocol(_Protocol)
                .WithRequestStyle(_RequestStyle)
                .WithLogger(Logger);

            // create bucket
            _S3.Bucket.WriteAsync(_Bucket, _Region).Wait();
            Console.WriteLine("");

            // List buckets
            ListAllMyBucketsResult buckets = _S3.Service.ListBucketsAsync().Result;
            Console.WriteLine("Buckets: " + buckets.Buckets.BucketList.Count);
            foreach (Bucket bucket in buckets.Buckets.BucketList) Console.WriteLine("| " + bucket.Name);
            Console.WriteLine("");

            // Check bucket existence
            bool exists = _S3.Bucket.ExistsAsync(_Bucket).Result;
            Console.WriteLine("Exists: " + exists);
            Console.WriteLine("");

            // List bucket contents
            ListBucketResult bucketContents = _S3.Bucket.ListAsync(_Bucket).Result;
            Console.WriteLine("Objects: " + bucketContents.Contents.Count);
            foreach (ObjectMetadata obj in bucketContents.Contents) Console.WriteLine("| " + obj.Key + " " + obj.Size + " bytes");
            Console.WriteLine("");

            // Write object 1
            _S3.Object.WriteAsync(_Bucket, _TestObjectKey1, Encoding.UTF8.GetBytes("hello world!")).Wait();
            Console.WriteLine("Success");
            Console.WriteLine("");

            // Check existence object 1
            exists = _S3.Object.ExistsAsync(_Bucket, _TestObjectKey1).Result;
            Console.WriteLine("Exists: " + exists);
            Console.WriteLine("");

            // Read object 1
            byte[] objectData = _S3.Object.GetAsync(_Bucket, _TestObjectKey1).Result;
            Console.WriteLine("Data: " + Encoding.UTF8.GetString(objectData));
            Console.WriteLine("");

            // Delete object 1
            _S3.Object.DeleteAsync(_Bucket, _TestObjectKey1).Wait();
            Console.WriteLine("Success");
            Console.WriteLine("");

            // Write object 2
            _S3.Object.WriteAsync(_Bucket, _TestObjectKey2, Encoding.UTF8.GetBytes("hello world!")).Wait();
            Console.WriteLine("Success");
            Console.WriteLine("");

            // Check existence object 2
            exists = _S3.Object.ExistsAsync(_Bucket, _TestObjectKey2).Result;
            Console.WriteLine("Exists: " + exists);
            Console.WriteLine("");

            // Read object 2
            objectData = _S3.Object.GetAsync(_Bucket, _TestObjectKey2).Result;
            Console.WriteLine("Data: " + Encoding.UTF8.GetString(objectData));
            Console.WriteLine("");

            // Delete object 2
            _S3.Object.DeleteAsync(_Bucket, _TestObjectKey2).Wait();
            Console.WriteLine("Success");
            Console.WriteLine("");

            // delete bucket
            _S3.Bucket.DeleteAsync(_Bucket).Wait();
            Console.WriteLine("");
        }

        static void Logger(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}