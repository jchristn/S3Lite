namespace Test.Script
{
    using System.IO;
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

        private static List<string> _ValidStorageTypes = new List<string>
        {
            "aws",
            "awslite",
            "azure",
            "cifs",
            "disk",
            "nfs"
        };

        private static List<string> _Keys = new List<string>
        {
            "root.txt",
            "dir1/",
            "dir1/1.txt",
            "dir1/dir2/",
            "dir1/dir2/2.txt",
            "dir1/dir2/dir3/",
            "dir1/dir2/dir3/3.txt"
        };

        public static async Task Main(string[] args)
        {
            InitializeClient();

            await CreateDirectoryStructure();
            await Enumerate("Post create enumeration");
            await ReadObjectMetadata();
            await ReadObjectContents();
            await CheckObjectExistence();
            await Enumerate("Prefix-based enumeration", "ro");
            await DeleteNonEmptyDirectories();
            await Enumerate("Post non-empty directory delete enumeration");
            await DeleteFiles();
            await DeleteEmptyDirectories();
            await Enumerate("Post directory delete enumeration");
            await CheckObjectExistence();
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

        static async Task CreateDirectoryStructure()
        {
            /*

        /
        |-- root.txt
        |-- dir1
            |-- 1.txt
            |-- dir2
                |-- 2.txt
                |-- dir3
                    |-- 3.txt
                
             */

            Console.WriteLine("");
            Console.WriteLine("Initializing repository");

            if (await _S3.Object.ExistsAsync(_Bucket, "root.txt"))
            {
                Console.WriteLine("| File root.txt already exists");
            }
            else
            {
                await _S3.Object.WriteAsync("root.txt", "text/plain", Encoding.UTF8.GetBytes("file root.txt"));
                Console.WriteLine("| Created file root.txt");
            }

            if (await _S3.Object.ExistsAsync(_Bucket, "dir1/"))
            {
                Console.WriteLine("| Directory dir1/ already exists");
            }
            else
            {
                await _S3.Object.WriteAsync(_Bucket, "dir1/", Array.Empty<byte>());
                Console.WriteLine("| Created directory dir1/");
            }

            if (await _S3.Object.ExistsAsync(_Bucket, "dir1/1.txt"))
            {
                Console.WriteLine("| File dir1/1.txt already exists");
            }
            else
            {
                await _S3.Object.WriteAsync(_Bucket, "dir1/1.txt", Encoding.UTF8.GetBytes("file dir1/1.txt"));
                Console.WriteLine("| Created file dir1/1.txt");
            }

            if (await _S3.Object.ExistsAsync(_Bucket, "dir1/dir2/"))
            {
                Console.WriteLine("| Directory dir1/dir2/ already exists");
            }
            else
            {
                await _S3.Object.WriteAsync(_Bucket, "dir1/dir2/", Array.Empty<byte>());
                Console.WriteLine("| Created directory dir1/dir2/");
            }

            if (await _S3.Object.ExistsAsync(_Bucket, "dir1/dir2/2.txt"))
            {
                Console.WriteLine("| File dir1/dir2/2.txt already exists");
            }
            else
            {
                await _S3.Object.WriteAsync(_Bucket, "dir1/dir2/2.txt", Encoding.UTF8.GetBytes("file dir1/dir2/2.txt"));
                Console.WriteLine("| Created file dir1/dir2/2.txt");
            }

            if (await _S3.Object.ExistsAsync(_Bucket, "dir1/dir2/dir3/"))
            {
                Console.WriteLine("| Directory dir1/dir2/dir3/ already exists");
            }
            else
            {
                await _S3.Object.WriteAsync(_Bucket, "dir1/dir2/dir3/", Array.Empty<byte>());
                Console.WriteLine("| Created directory dir1/dir2/dir3/");
            }

            if (await _S3.Object.ExistsAsync(_Bucket, "dir1/dir2/dir3/3.txt"))
            {
                Console.WriteLine("| File dir1/dir2/dir3/3.txt already exists");
            }
            else
            {
                await _S3.Object.WriteAsync(_Bucket, "dir1/dir2/dir3/3.txt", Encoding.UTF8.GetBytes("file dir1/dir2/dir3/3.txt"));
                Console.WriteLine("| Created file dir1/dir2/dir3/3.txt");
            }
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

        static async Task ReadObjectMetadata()
        {
            Console.WriteLine("");
            Console.WriteLine("Retrieving metadata");

            foreach (string key in _Keys)
            {
                ObjectMetadata md = await _S3.Object.GetMetadataAsync(_Bucket, key);
                Console.WriteLine(md.ToString());
            }
        }

        static async Task ReadObjectContents()
        {
            Console.WriteLine("");
            Console.WriteLine("Retrieving object content");

            foreach (string key in _Keys)
            {
                byte[] data = await _S3.Object.GetAsync(_Bucket, key);
                if (data != null)
                    Console.WriteLine("| " + key + ": " + Encoding.UTF8.GetString(data).Trim());
                else
                    Console.WriteLine("| " + key + ": (null)");
            }
        }

        static async Task DeleteNonEmptyDirectories()
        {
            Console.WriteLine("");
            Console.WriteLine("Deleting non-empty directories");

            try
            {
                await _S3.Object.DeleteAsync(_Bucket, "dir1/");
                Console.WriteLine("| This message indicates a failure for the operation on dir1/");
            }
            catch (Exception e)
            {
                Console.WriteLine("| Exception (expected): " + e.Message);
            }

            try
            {
                await _S3.Object.DeleteAsync(_Bucket, "dir1/dir2/");
                Console.WriteLine("| This message indicates a failure for the operation on dir1/dir2/");
            }
            catch (Exception e)
            {
                Console.WriteLine("| Exception (expected): " + e.Message);
            }

            try
            {
                await _S3.Object.DeleteAsync(_Bucket, "dir1/dir2/dir3/");
                Console.WriteLine("| This message indicates a failure for the operation on dir1/dir2/dir3/");
            }
            catch (Exception e)
            {
                Console.WriteLine("| Exception (expected): " + e.Message);
            }
        }

        static async Task DeleteFiles()
        {
            Console.WriteLine("");
            Console.WriteLine("Deleting files");

            /*

        /
        |-- root.txt
        |-- dir1
            |-- 1.txt
            |-- dir2
                |-- 2.txt
                |-- dir3
                    |-- 3.txt
                
             */

            await _S3.Object.DeleteAsync(_Bucket, "root.txt");
            await _S3.Object.DeleteAsync(_Bucket, "dir1/1.txt");
            await _S3.Object.DeleteAsync(_Bucket, "dir1/dir2/2.txt");
            await _S3.Object.DeleteAsync(_Bucket, "dir1/dir2/dir3/3.txt");
        }

        static async Task DeleteEmptyDirectories()
        {
            Console.WriteLine("");
            Console.WriteLine("Deleting empty directories");

            await _S3.Object.DeleteAsync(_Bucket, "dir1/dir2/dir3/");
            await _S3.Object.DeleteAsync(_Bucket, "dir1/dir2/");
            await _S3.Object.DeleteAsync(_Bucket, "dir1/");
        }

        static async Task CheckObjectExistence()
        {
            Console.WriteLine("");
            Console.WriteLine("Checking existence:");

            foreach (string key in _Keys)
            {
                Console.WriteLine("| " + key + ": " + await _S3.Object.ExistsAsync(_Bucket, key));
            }
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore IDE0059 // Unnecessary assignment of a value
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}