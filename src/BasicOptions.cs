using CommandLine;

namespace s3cli.src
{
    public abstract class BasicOptions
    {
        #region Direct-Settings
        [Option("service-url", SetName="DirectSettings", Required = true, HelpText = "The Service-URL. Like https://ams3.digitaloceanspaces.com")]
        public string ServiceURL { get; set; }

        [Option("access-key-id", SetName="DirectSettings", Required = true, HelpText = "The AccessKeyId.")]
        public string AccessKeyId { get; set; }

        [Option("secret-key-id", SetName="DirectSettings", Required = true, HelpText = "The SecretKeyId.")]
        public string SecretKeyId { get; set; }

        [Option("bucket-name", SetName="DirectSettings", Required = true, HelpText = "The bucket-name")]
        public string BucketName { get; set; }        
        #endregion

        #region Settings-File
        [Option("settings", SetName="SettingsFile", Required = true, Default = "", HelpText = "The settings-file")]
        public string SettingsFile { get; set; }
        #endregion
    }
}