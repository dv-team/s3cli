# S3Cli

## Command line interface

### Run

List files

```
dotnet run list --prefix rechnungen/ --endpoint https://<service-url> --bucket <bucket-name> --access-key <access-key> --secret-key <secret-key>
```

Object key exists?

```
dotnet run has --object-key <remotefilename> --prefix rechnungen/ --endpoint https://<service-url> --bucket <bucket-name> --access-key <access-key> --secret-key <secret-key>
```

Download a file

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
dotnet publish -c Release
```
