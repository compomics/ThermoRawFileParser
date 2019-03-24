using System.ComponentModel;
using System.IO;
using NUnit.Framework.Constraints;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParser
{
    public class ParseInput
    {
        /// <summary>
        /// The RAW file path.
        /// </summary>
        public string RawFilePath { get; }

        /// <summary>
        /// The output directory.
        /// </summary>
        public string OutputDirectory { get; }

        /// <summary>
        /// The output format.
        /// </summary>
        public OutputFormat OutputFormat { get; }

        /// <summary>
        /// Gzip the output file.
        /// </summary>
        public bool Gzip { get; }

        /// <summary>
        /// Output the metadata.
        /// </summary>
        public MetadataFormat OutputMetadata { get; }

        /// <summary>
        /// The raw file name.
        /// </summary>
        public string RawFileName { get; }

        /// <summary>
        /// The RAW file name without extension.
        /// </summary>
        public string RawFileNameWithoutExtension { get; }

        public S3Loader S3loader { get; set; }

        public string S3AccessKeyId { get; set; }

        public string S3SecretAccessKey { get; set; }

        public string S3url { get; set; }

        private string bucketName;
        private bool ignoreInstrumentErrors;

        public bool IgnoreInstrumentErrors
        {
            get => ignoreInstrumentErrors;
            set => ignoreInstrumentErrors = value;
        }


        public ParseInput(string rawFilePath, string outputDirectory, OutputFormat outputFormat, bool gzip,
            MetadataFormat outputMetadata, string s3url, string s3AccessKeyId,
            string s3SecretAccessKey, string bucketName,
            bool ignoreInstrumentErrors
        )
        {
            RawFilePath = rawFilePath;
            var splittedPath = RawFilePath.Split('/');
            RawFileName = splittedPath[splittedPath.Length - 1];
            RawFileNameWithoutExtension = Path.GetFileNameWithoutExtension(RawFileName);
            OutputDirectory = outputDirectory;
            OutputFormat = outputFormat;
            Gzip = gzip;
            OutputMetadata = outputMetadata;            
            S3url = s3url;
            S3AccessKeyId = s3AccessKeyId;
            S3SecretAccessKey = s3SecretAccessKey;
            this.bucketName = bucketName;
            this.ignoreInstrumentErrors = ignoreInstrumentErrors;

            if (S3url != null && S3AccessKeyId != null && S3SecretAccessKey != null)
                InitializeS3Bucket(s3url, s3AccessKeyId, s3SecretAccessKey, bucketName);
        }

        private void InitializeS3Bucket(string s3url, string s3AccessKeyId, string s3SecretAccessKey, string bucketName)
        {
            S3loader = new S3Loader(s3url, s3AccessKeyId, s3SecretAccessKey, bucketName);
        }
    }
}