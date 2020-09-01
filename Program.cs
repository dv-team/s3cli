using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using CommandLine;
using s3cli.src;

namespace s3cli
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 1) {
                Console.WriteLine("Missing Command.");
                Console.WriteLine("Operation could be one of: list, sync, has, download, upload, remove");
                Environment.ExitCode = 1;
                return;
            }

            var command = args[0].ToLower();

            args.Skip(1);

            switch(command) {
                case "list":     RunTask<ListObjectsOptions>(args, ListObjects); break;
                case "has":      RunTask<HasObjectOptions>(args, HasObject); break;
                case "upload":   RunTask<UploadObjectOptions>(args, UploadObject); break;
                case "download": RunTask<DownloadObjectOptions>(args, DownloadObject); break;
                default:
                    Console.WriteLine("Invalid Command.");
                    Console.WriteLine("Operation could be one of: list, sync, has, download, upload, remove");
                    Environment.ExitCode = 1;
                    return;
            }
        }

        private static void RunTask<T>(string[] args, Func<T, Task> func) {
            CommandLine.Parser.Default.ParseArguments<T>(args).WithParsed((T opts) => func(opts).GetAwaiter().GetResult())
            .WithNotParsed((IEnumerable<Error> errs) => {});
        }

        /**
         * Checks if an objects exists on a bucket
         */ 
        private static async Task HasObject(HasObjectOptions opts)
        {
            AmazonS3Client s3Client = GetClient(opts);

            try {
                var result = await s3Client.GetObjectAsync(opts.BucketName, opts.ObjectKey);
                Console.WriteLine(result.Key);
                Environment.ExitCode = 0;
            } catch (Amazon.S3.AmazonS3Exception e) {
                if(e.ErrorCode == "NoSuchKey") {
                    Environment.ExitCode = 1;
                } else {
                    Environment.ExitCode = 999;
                }
                Console.WriteLine(e.ErrorCode);
            }
        }

        /**
         * Lists all objects in a bucket
         */
        private static async Task ListObjects(ListObjectsOptions opts)
        {
            AmazonS3Client s3Client = GetClient(opts);
            var request = new ListObjectsV2Request {
                BucketName = opts.BucketName,
                Prefix = opts.Prefix ?? ""
            };
            ListObjectsV2Response response;
            do {
                response = await s3Client.ListObjectsV2Async(request);
                foreach(var obj in response.S3Objects) {
                    Console.WriteLine(obj.Key.ToString());
                }
                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated);
        }

        /**
         * Upload an Object to S3
         */
        private static async Task DownloadObject(DownloadObjectOptions opts)
        {
            try {
                var client = GetClient(opts);
                var request = new Amazon.S3.Model.GetObjectRequest {
                    BucketName = opts.BucketName,
                    Key = opts.ObjectKey
                };
                using (GetObjectResponse response = await client.GetObjectAsync(request))
                using (Stream responseStream = response.ResponseStream)
                using (StreamReader reader = new StreamReader(responseStream)) {
                    Console.WriteLine("Content type: {0}", response.Headers["Content-Type"]);
                    foreach(var key in response.Metadata.Keys) {
                        Console.WriteLine("{0}\t{1}", key, response.Metadata[key]);
                    }
                    FileStream fs = File.OpenWrite(opts.Filename);
                    fs.SetLength(0);
                    await responseStream.CopyToAsync(fs);
                }
            } catch (AmazonS3Exception e) {
                // If bucket or object does not exist
                Console.WriteLine("Error encountered ***. Message:'{0}' when reading object", e.Message);
            } catch (Exception e) {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when reading object", e.Message);
            }
        }

        /**
         * Upload an Object to S3
         */
        private static async Task UploadObject(UploadObjectOptions opts)
        {
            try {
                AmazonS3Client s3Client = GetClient(opts);
                MetadataCollection metadata = new MetadataCollection();
                var fileTransferUtility = new TransferUtility(s3Client);
                var fileTransferUtilityRequest = new TransferUtilityUploadRequest {
                        BucketName = opts.BucketName,
                        FilePath = opts.Filename,
                        StorageClass = S3StorageClass.StandardInfrequentAccess,
                        PartSize = new FileInfo(opts.Filename).Length, // Size of the file to be uploaded
                        Key = opts.ObjectKey,
                        CannedACL = S3CannedACL.AuthenticatedRead
                    };
                fileTransferUtilityRequest.Metadata.Add("OriginalFilename", Path.GetFileName(opts.Filename));
                await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                Environment.ExitCode = 1;
            }
        }

        private static AmazonS3Client GetClient(BasicOptions opts) {
            if(opts.SettingsFile != "") {
                // Handle Settings file
                return new AmazonS3Client(opts.AccessKeyId, opts.SecretKeyId, new AmazonS3Config() { ServiceURL = opts.ServiceURL });
            }
            return new AmazonS3Client(opts.AccessKeyId, opts.SecretKeyId, new AmazonS3Config() { ServiceURL = opts.ServiceURL });
        }
    }
}
