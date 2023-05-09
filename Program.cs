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
    private readonly OptionDetails _objectKeyOption = new(Name: "--objectKey", Description: "The object key (this is what a file name is called in an S3 environment).");
    
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
        var prefixOption = AddOption<string?>(command, _prefixOption, isRequired: false);

        command.SetHandler((string serviceUrl, string awsAccessKeyId, string awsSecretKeyId, string bucket, string? prefix) =>
        {
            try {
                var client = GetClient(serviceUrl, awsAccessKeyId, awsSecretKeyId);
                var bucketName = bucket;
                var request = new ListObjectsRequest();
                request.BucketName = bucketName;
                request.Prefix = prefix;
                var objects = client.ListObjectsAsync(request).GetAwaiter().GetResult();

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

        var pathOption = AddOption<string>(command, new OptionDetails(Name: "--local-path", Description: "The local path to the file to be uploaded."), isRequired: true);
        var objectKeyOption = AddOption<string?>(command, _objectKeyOption);
        var endpointOption = AddOption<string>(command, this._endpointOption, isRequired: true);
        var accesskeyOption = AddOption<string>(command, _accessKeyOption, isRequired: true);
        var secretkeyOption = AddOption<string>(command, _secretkeyOption, isRequired: true);
        var bucketOption = AddOption<string>(command, _bucketOption, isRequired: true);
        var prefixOption = AddOption<string?>(command, _prefixOption);

        command.SetHandler((string serviceUrl, string awsAccessKeyId, string awsSecretKeyId, string bucket, string? prefix, string path, string? objectKey) =>
        {
            var client = GetClient(serviceUrl, awsAccessKeyId, awsSecretKeyId);
            var bucketName = bucket;
            var request = new ListObjectsRequest();
            request.BucketName = bucketName;
            request.Prefix = prefix;

            if (!ObjectExists(client, bucketName, path)) {
                Environment.Exit(ObjectDoesNotExist);
            }

            try {
                var transfer = new TransferUtility(client);
                var filename = objectKey ?? $"/{path}".Split("/").Last();
                transfer.Upload(filename, bucketName, path);
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

        var pathOption = AddOption<string>(command, new(Name: "--local-path", Description: "The local path to the download target."), isRequired: true);
        var objectKeyOption = AddOption<string?>(command, _objectKeyOption, isRequired: true, defaultValue: null);
        var endpointOption = AddOption<string>(command, _endpointOption, isRequired: true);
        var accesskeyOption = AddOption<string>(command, _accessKeyOption, isRequired: true);
        var secretkeyOption = AddOption<string>(command, _secretkeyOption, isRequired: true);
        var bucketOption = AddOption<string>(command, _bucketOption, isRequired: true);
        var prefixOption = AddOption<string?>(command, _prefixOption, isRequired: false, defaultValue: null);

        command.SetHandler((serviceUrl, awsAccessKeyId, awsSecretKeyId, bucket, prefix, path, objectKey) =>
        {
            var client = GetClient(serviceUrl, awsAccessKeyId, awsSecretKeyId);
            var bucketName = bucket;
            var request = new ListObjectsRequest();
            request.BucketName = bucketName;
            request.Prefix = prefix;

            if (!ObjectExists(client, bucketName, path)) {
                Environment.Exit(ObjectDoesNotExist);
            }

            try {
                var transfer = new TransferUtility(client);
                var filename = objectKey ?? $"/{path}".Split("/").Last();
                transfer.Download(filename, bucketName, path);
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
        
        var objectKeyOption = AddOption<string>(command, _objectKeyOption, isRequired: true);
        var endpointOption = AddOption<string>(command, _endpointOption, isRequired: true);
        var accesskeyOption = AddOption<string>(command, _accessKeyOption, isRequired: true);
        var secretkeyOption = AddOption<string>(command, _secretkeyOption, isRequired: true);
        var bucketOption = AddOption<string>(command, _bucketOption, isRequired: true);
        var prefixOption = AddOption<string?>(command, _prefixOption, isRequired: false);

        command.SetHandler((serviceUrl, awsAccessKeyId, awsSecretKeyId, bucket, prefix, objectKey) =>
        {
            var client = GetClient(serviceUrl, awsAccessKeyId, awsSecretKeyId);
            var bucketName = bucket;
            var request = new ListObjectsRequest();
            request.BucketName = bucketName;
            request.Prefix = prefix;

            if (!ObjectExists(client, bucketName, objectKey)) {
                Environment.Exit(ObjectDoesNotExist);
            }

            try {
                client.DeleteObjectAsync(bucketName, objectKey).GetAwaiter().GetResult();
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

    private Option<T> AddOption<T>(Command command, OptionDetails optionDetails, bool isRequired = true)
    {
        var option = new Option<T>(name: optionDetails.Name, description: optionDetails.Description);
        option.IsRequired = isRequired;
        command.AddOption(option);
        return option;
    }

    private Option<T> AddOption<T>(Command command, OptionDetails optionDetails, bool isRequired, T defaultValue)
    {
        var option = new Option<T>(name: optionDetails.Name, description: optionDetails.Description, getDefaultValue: () => defaultValue);
        option.IsRequired = isRequired;
        command.AddOption(option);
        return option;
    }

    public record OptionDetails(string Name, string Description);
}
