namespace Test.LargeEnumeration
{
    using System.Text;
    using S3Lite;
    using S3Lite.ApiObjects;
    using GetSomeInput;

    public static class TestScript
    {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        static ProtocolEnum _Protocol = ProtocolEnum.Https;
        static string _Hostname = null;
        static int? _Port = null;
        static string _Region = "us-east-2";
        static string _AccessKey = null;
        static string _SecretKey = null;
        static string _Bucket = null;
        static RequestStyleEnum _RequestStyle = RequestStyleEnum.VirtualHostedStyle;
        static SignatureVersionEnum _SignatureVersion = SignatureVersionEnum.Version4;

        static S3Client _S3 = null;
        static int _NumFiles = 5000;
        static bool _Logging = true;
        static bool _Cleanup = false;

        public static async Task Main(string[] args)
        {
            InitializeClient();

            // await CreateFiles();
            await Enumerate("Post create enumeration");

            if (_Cleanup) await DeleteFiles();
        }

        static void InitializeClient()
        {
            _S3 = new S3Client()
                .WithProtocol(_Protocol)
                .WithRegion(_Region)
                .WithAccessKey(_AccessKey)
                .WithSecretKey(_SecretKey)
                .WithRequestStyle(_RequestStyle)
                .WithSignatureVersion(_SignatureVersion);

            if (!String.IsNullOrEmpty(_Hostname)) _S3 = _S3.WithHostname(_Hostname);
            if (_Port != null) _S3 = _S3.WithPort(_Port.Value);

            if (_Logging) _S3.Logger = Console.WriteLine;
        }

        static async Task CreateFiles()
        {
            Console.WriteLine("");
            Console.WriteLine("Creating " + _NumFiles + " files");

            for (int i = 0; i < _NumFiles; i++)
            {
                if (!await _S3.Object.ExistsAsync(_Bucket, i.ToString()))
                {
                    await _S3.Object.WriteAsync(_Bucket, i.ToString(), Encoding.UTF8.GetBytes(i.ToString()));
                    Console.Write(i.ToString() + " ");
                }
            }

            Console.WriteLine("");
        }

        static async Task Enumerate(string msg, string prefix = null)
        {
            Console.WriteLine("");
            Console.WriteLine(msg);
            if (!String.IsNullOrEmpty(prefix)) Console.WriteLine("| Using prefix   : " + prefix);

            string continuationToken = null;
            List<ObjectMetadata> objs = new List<ObjectMetadata>();

            Console.WriteLine("BLOBs:");

            while (true)
            {
                ListBucketResult lbr = await _S3.Bucket.ListAsync(_Bucket, prefix, null, continuationToken);

                if (lbr != null)
                {
                    foreach (ObjectMetadata md in lbr.Contents)
                        objs.Add(md);

                    if (!String.IsNullOrEmpty(lbr.NextContinuationToken))
                    {
                        Console.WriteLine("Continuation token: " + lbr.NextContinuationToken + " (" + lbr.Contents.Count + " objects in batch)");
                        continuationToken = lbr.NextContinuationToken;
                    }
                    else
                        break;
                }
                else
                {
                    Console.WriteLine("No response");
                }
            }

            Console.WriteLine("Objects:");
            foreach (ObjectMetadata md in objs)
                Console.WriteLine("| " + md.Key);

            Console.WriteLine("");
            Console.WriteLine(objs.Count + " objects");
        }

        static async Task DeleteFiles()
        {
            Console.WriteLine("");
            Console.WriteLine("Deleting files");

            for (int i = 0; i < _NumFiles; i++)
            {
                await _S3.Object.DeleteAsync(_Bucket, i.ToString());
            }
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore IDE0059 // Unnecessary assignment of a value
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}