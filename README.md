# S3Cli

## Command line interface

### Run

```
Description:
  List and download files from an S3 endpoint.

Usage:
  s3-download-client [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  list      List all objects in an S3 storage
  upload    Download one object from a S3 storage
  download  Download one object (referenced by a fully qualified object key or a prefix plus object-key) from a S3 storage
  delete    Remove one object from a S3 storage
```

List files

```
dotnet run list --prefix rechnungen/ --endpoint https://<service-url> --bucket <bucket-name> --access-key <access-key> --secret-key <secret-key>
```

Object key exists?

```
dotnet run has --object-key <remotefilename> --prefix rechnungen/ --endpoint https://<service-url> --bucket <bucket-name> --access-key <access-key> --secret-key <secret-key>
```

Upload a file

```
dotnet run upload --local-path <localfilepath> --prefix rechnungen/ --object-key <remotefile> --endpoint https://<service-url> --bucket <bucket-name> --access-key <access-key> --secret-key <secret-key>
```

Download a file

```
dotnet run download --local-path <localfile> --prefix rechnungen/ --object-key <remotefile> --endpoint https://<service-url> --bucket <bucket-name> --access-key <access-key> --secret-key <secret-key>
```

### Build

Build a self-containing exe:

```
dotnet publish -c Release -r win10-x64 -p:PublishSingleFile=true --self-contained false
```
