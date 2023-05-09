using System.CommandLine;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using dotenv.net;

DotEnv.Load();

new CommandBuilder(args);

class CommandBuilder
{
    private const int ObjectDoesNotExist = 404;
    private const int UnknownError = 500;
    
    private readonly OptionDetails _endpointOption = new(Name: "--endpoint", Description: "The FQDN Endpoint with the protocol.");
    private readonly OptionDetails _accessKeyOption = new(Name: "--access-key", Description: "The AWS access key.");
    private readonly OptionDetails _secretkeyOption = new(Name: "--secret-key", Description: "The AWS secret key.");
    private readonly OptionDetails _bucketOption = new(Name: "--bucket", Description: "The S3 Bucket.");
    private readonly OptionDetails _prefixOption = new(Name: "--prefix", Description: "The prefix of the files.");
    private readonly OptionDetails _objectKeyOption = new(Name: "--object-key", Description: "The object key (this is what a file name is called in an S3 environment).");
    
    public CommandBuilder(string[] args) {
        RootCommand rootCommand = new RootCommand(description: "List and download files from an S3 endpoint.");

        rootCommand.AddCommand(CreateListCommand());
        rootCommand.AddCommand(CreateUploadCommand());
        rootCommand.AddCommand(CreateDownloadCommand());
        rootCommand.AddCommand(CreateDeleteCommand());

        rootCommand.Invoke(args);
    }

    private Command CreateListCommand()
    {
        var command = new Command("list", "List all objects in an S3 storage");

        var endpointOption = AddOption<string>(command, _endpointOption);
        var accesskeyOption = AddOption<string>(command, _accessKeyOption);
        var secretkeyOption = AddOption<string>(command, _secretkeyOption);
        var bucketOption = AddOption<string>(command, _bucketOption);
        var prefixOption = AddOption<string?>(command, _prefixOption);

        command.SetHandler((string serviceUrl, string awsAccessKeyId, string awsSecretKeyId, string bucket, string? prefix) =>
        {
            try {
                var client = GetClient(serviceUrl, awsAccessKeyId, awsSecretKeyId);
                var bucketName = bucket;
                var request = new ListObjectsRequest();
                request.BucketName = bucketName;
                request.Prefix = prefix;
                var objects = client.ListObjectsAsync(request).GetAwaiter().GetResult();

                var filenames = new LinkedList<string>();
                foreach (var o in objects.S3Objects) {
                    if (o.Key.EndsWith("/"))
                    {
                        continue;
                    }

                    string filename = o.Key;
                    if(prefix != null && o.Key.StartsWith(prefix))
                    {
                        filename = filename.Substring(prefix.Length);
                    }

                    filenames.AddLast(filename);
                }
                
                foreach (var filename in filenames)
                {
                    Console.WriteLine(filename);
                }
            } catch (Exception e) {
                Console.WriteLine($"ERROR: {e.Message}\n");
                Environment.Exit(UnknownError);
            }
        }, endpointOption, accesskeyOption, secretkeyOption, bucketOption, prefixOption);

        return command;
    }

    private Command CreateUploadCommand()
    {
        var command = new Command("upload", "Download one object from a S3 storage");

        var pathOption = AddOption<string>(command, new OptionDetails(Name: "--local-path", Description: "The local path to the file to be uploaded."));
        var objectKeyOption = AddOption<string>(command, _objectKeyOption);
        var endpointOption = AddOption<string>(command, this._endpointOption);
        var accesskeyOption = AddOption<string>(command, _accessKeyOption);
        var secretkeyOption = AddOption<string>(command, _secretkeyOption);
        var bucketOption = AddOption<string>(command, _bucketOption);
        var prefixOption = AddOption<string?>(command, _prefixOption);

        command.SetHandler((string serviceUrl, string awsAccessKeyId, string awsSecretKeyId, string bucket, string? prefix, string path, string objectKey) =>
        {
            var client = GetClient(serviceUrl, awsAccessKeyId, awsSecretKeyId);
            var bucketName = bucket;
            var fullObjectPath = ConcatPaths(prefix, objectKey);
            Console.WriteLine($"Upload {path} to {fullObjectPath}");

            if (!ObjectExists(client, bucketName, path)) {
                Environment.Exit(ObjectDoesNotExist);
            }

            try {
                var transfer = new TransferUtility(client);
                transfer.Upload(path, bucketName, objectKey);
            } catch (Exception e) {
                Console.WriteLine($"ERROR: {e.Message}\n");
                Environment.Exit(UnknownError);
            }
        }, endpointOption, accesskeyOption, secretkeyOption, bucketOption, prefixOption, pathOption, objectKeyOption);

        return command;
    }

    private Command CreateDownloadCommand()
    {
        var command = new Command("download", "Download one object (referenced by a fully qualified object key or a prefix plus object-key) from a S3 storage");

        var pathOption = AddOption<string>(command, new(Name: "--local-path", Description: "The local path to the download target."));
        var objectKeyOption = AddOption<string>(command, _objectKeyOption);
        var endpointOption = AddOption<string>(command, _endpointOption);
        var accesskeyOption = AddOption<string>(command, _accessKeyOption);
        var secretkeyOption = AddOption<string>(command, _secretkeyOption);
        var bucketOption = AddOption<string>(command, _bucketOption);
        var prefixOption = AddOption<string?>(command, _prefixOption, defaultValue: null);

        command.SetHandler((serviceUrl, awsAccessKeyId, awsSecretKeyId, bucket, prefix, path, objectKey) =>
        {
            var client = GetClient(serviceUrl, awsAccessKeyId, awsSecretKeyId);
            var bucketName = bucket;
            var fullObjectPath = ConcatPaths(prefix, objectKey);
            Console.WriteLine($"Download {fullObjectPath} to {path}");

            if (!ObjectExists(client, bucketName, fullObjectPath)) {
                Environment.Exit(ObjectDoesNotExist);
            }

            try {
                var transfer = new TransferUtility(client);
                transfer.Download(path, bucketName, fullObjectPath);
            } catch (Exception e) {
                Console.WriteLine($"ERROR: {e.Message}\n");
                Environment.Exit(UnknownError);
            }
        }, endpointOption, accesskeyOption, secretkeyOption, bucketOption, prefixOption, pathOption, objectKeyOption);

        return command;
    }
    
    private Command CreateDeleteCommand()
    {
        var command = new Command("delete", "Remove one object from a S3 storage");
        
        var objectKeyOption = AddOption<string>(command, _objectKeyOption);
        var endpointOption = AddOption<string>(command, _endpointOption);
        var accesskeyOption = AddOption<string>(command, _accessKeyOption);
        var secretkeyOption = AddOption<string>(command, _secretkeyOption);
        var bucketOption = AddOption<string>(command, _bucketOption);
        var prefixOption = AddOption<string?>(command, _prefixOption);

        command.SetHandler((serviceUrl, awsAccessKeyId, awsSecretKeyId, bucket, prefix, objectKey) =>
        {
            var client = GetClient(serviceUrl, awsAccessKeyId, awsSecretKeyId);
            var bucketName = bucket;
            var fullObjectPath = ConcatPaths(prefix, objectKey);
            Console.WriteLine($"Delete {fullObjectPath}");

            if (!ObjectExists(client, bucketName, fullObjectPath)) {
                Environment.Exit(ObjectDoesNotExist);
            }

            try {
                client.DeleteObjectAsync(bucketName, fullObjectPath).GetAwaiter().GetResult();
            } catch (Exception e) {
                Console.WriteLine($"ERROR: {e.Message}\n");
                Environment.Exit(UnknownError);
            }
        }, endpointOption, accesskeyOption, secretkeyOption, bucketOption, prefixOption, objectKeyOption);

        return command;
    }

    private bool ObjectExists(AmazonS3Client client, string bucketName, string path) {
        try {
            client.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = bucketName, Key = path }).GetAwaiter().GetResult();
            return true;
        } catch (Amazon.S3.AmazonS3Exception ex) {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                return false;

            throw;
        }
    }

    private static AmazonS3Client GetClient(string serviceUrl, string awsAccessKeyId, string awsSecretKeyId)
    {
        var config = new AmazonS3Config { ServiceURL = serviceUrl };
        return new AmazonS3Client(awsAccessKeyId, awsSecretKeyId, config);
    }

    private Option<T> AddOption<T>(Command command, OptionDetails optionDetails)
    {
        var option = new Option<T>(name: optionDetails.Name, description: optionDetails.Description);
        command.AddOption(option);
        return option;
    }

    private Option<T> AddOption<T>(Command command, OptionDetails optionDetails, T defaultValue)
    {
        var option = new Option<T>(name: optionDetails.Name, description: optionDetails.Description, getDefaultValue: () => defaultValue);
        command.AddOption(option);
        return option;
    }

    private string ConcatPaths(string? prefix, string objectKey)
    {
        var normalizedPrefix = (prefix ?? "").TrimEnd('/');
        if (normalizedPrefix == "")
        {
            return objectKey;
        }

        return $"{normalizedPrefix}/{objectKey.TrimStart('/')}";
    }

    public record OptionDetails(string Name, string Description);
}
