using CommandLine;

namespace s3cli.src
{
    public class ListObjectsOptions : BasicOptions
    {
        [Option("prefix", Required = false, Default = "", HelpText = "Prefix for all objects to be listed")]
        public string Prefix { get; set; }
    }

    public class HasObjectOptions : BasicOptions
    {
        [Option("object-key", Required = true, HelpText = "The object key to look for")]
        public string ObjectKey { get; set; }
    }

    public class DownloadObjectOptions : BasicOptions 
    {
        [Option("object-key", Required = true, HelpText = "The (full) object key on the remove server")]
        public string ObjectKey { get; set; }

        [Option("local-filename", Required = true, HelpText = "The target file path in the local environment")]
        public string Filename { get; set; }
    }

    public class UploadObjectOptions : BasicOptions
    {
        [Option("object-key", Required = true, HelpText = "The (full) object key on the remove server")]
        public string ObjectKey { get; set; }

        [Option("local-filename", Required = true, HelpText = "Local filename to upload")]
        public string Filename { get; set; }
    }
}