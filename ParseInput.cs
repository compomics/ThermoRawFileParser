using System;
using System.IO;
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
        /// The output file.
        /// </summary>>
        public string OutputFile { get; }

        /// <summary>
        /// Gzip the output file.
        /// </summary>
        public bool Gzip { get; }

        /// <summary>
        /// Output the metadata.
        /// </summary>
        public MetadataFormat OutputMetadata { get; }

        /// <summary>
        /// The metadata output file.
        /// </summary>>
        public string MetadataOutputFile { get; }

        /// <summary>
        /// The raw file name.
        /// </summary>
        public string RawFileName { get; }

        /// <summary>
        /// The RAW file name without extension.
        /// </summary>
        public string RawFileNameWithoutExtension { get; }

        private S3Loader S3Loader { get; set; }

        private string S3AccessKeyId { get; }

        private string S3SecretAccessKey { get; }

        private string S3url { get; }

        public bool IgnoreInstrumentErrors { get; }

        public bool NoPeakPicking { get; }

        public bool NoZlibCompression { get; }

        public LogFormat LogFormat { get; }

        private readonly string bucketName;

        public ParseInput(string rawFilePath, string outputDirectory, string outputFile, OutputFormat outputFormat
        )
        {
            RawFilePath = rawFilePath;
            var splittedPath = RawFilePath.Split('/');
            RawFileName = splittedPath[splittedPath.Length - 1];
            RawFileNameWithoutExtension = Path.GetFileNameWithoutExtension(RawFileName);
            OutputDirectory = outputDirectory;
            OutputFile = outputFile;
            OutputFormat = outputFormat;
            Gzip = false;
            OutputMetadata = MetadataFormat.NONE;
            IgnoreInstrumentErrors = false;
            NoPeakPicking = false;
            NoZlibCompression = false;
            LogFormat = LogFormat.DEFAULT;

            if (S3url != null && S3AccessKeyId != null && S3SecretAccessKey != null && bucketName != null)
                if (Uri.IsWellFormedUriString(S3url, UriKind.Absolute))
                {
                    InitializeS3Bucket();
                }
                else
                {
                    throw new UriFormatException("Invalid S3 url: " + S3url);
                }

            if (OutputDirectory == null && OutputFile != null)
                OutputDirectory = Path.GetDirectoryName(OutputFile);
        }

        public ParseInput(string rawFilePath, string outputDirectory, string outputFile, OutputFormat outputFormat,
            bool gzip,
            MetadataFormat outputMetadata, string metadataOutputFile, string s3url, string s3AccessKeyId,
            string s3SecretAccessKey, string bucketName,
            bool ignoreInstrumentErrors, bool noPeakPicking, bool noZlibCompression, LogFormat logFormat
        )
        {
            RawFilePath = rawFilePath;
            var splittedPath = RawFilePath.Split('/');
            RawFileName = splittedPath[splittedPath.Length - 1];
            RawFileNameWithoutExtension = Path.GetFileNameWithoutExtension(RawFileName);
            OutputDirectory = outputDirectory;
            OutputFile = outputFile;
            OutputFormat = outputFormat;
            Gzip = gzip;
            OutputMetadata = outputMetadata;
            MetadataOutputFile = metadataOutputFile;
            S3url = s3url;
            S3AccessKeyId = s3AccessKeyId;
            S3SecretAccessKey = s3SecretAccessKey;
            this.bucketName = bucketName;
            IgnoreInstrumentErrors = ignoreInstrumentErrors;
            NoPeakPicking = noPeakPicking;
            NoZlibCompression = noZlibCompression;
            LogFormat = logFormat;

            if (S3url != null && S3AccessKeyId != null && S3SecretAccessKey != null && bucketName != null)
                if (Uri.IsWellFormedUriString(S3url, UriKind.Absolute))
                {
                    InitializeS3Bucket();
                }
                else
                {
                    throw new UriFormatException("Invalid S3 url: " + S3url);
                }

            if (OutputDirectory == null && OutputFile != null)
                OutputDirectory = Path.GetDirectoryName(OutputFile);
        }

        private void InitializeS3Bucket()
        {
            S3Loader = new S3Loader(S3url, S3AccessKeyId, S3SecretAccessKey, bucketName);
        }
    }
}