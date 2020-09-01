# S3Cli

## Command line interface

### Run

List files

```
dotnet run list --prefix rechnungen/ --service-url https://<service-url> --access-key-id <access-key> --secret-key-id <secret-key> --bucket-name <bucket-name>
```

Object key exists?

```
dotnet run has --object-key <remotefile> --service-url https://<service-url> --access-key-id <access-key> --secret-key-id <secret-key> --bucket-name <bucket-name>
```

Download a file

```
dotnet run upload --local-filename <localfile> --object-key <remotefile> --service-url https://<service-url> --access-key-id <access-key> --secret-key-id <secret-key> --bucket-name <bucket-name>
```

Download a file

```
dotnet run download --object-key <remotefile> --local-filename <localfile> --service-url https://<service-url> --access-key-id <access-key> --secret-key-id <secret-key> --bucket-name <bucket-name>
```

### Build

Build a self-containing exe:

```
dotnet publish -c Release
```