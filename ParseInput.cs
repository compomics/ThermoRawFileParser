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
        private string rawFilePath;

        /// <summary>
        /// The RAW folder path.
        /// </summary>
        public string RawDirectoryPath { get; }

        public string RawFilePath
        {
            get => rawFilePath;
            set
            {
                rawFilePath = value;
                if (value != null)
                {
                    RawFileNameWithoutExtension = Path.GetFileNameWithoutExtension(value);
                    var splittedPath = value.Split('/');
                    rawFileName = splittedPath[splittedPath.Length - 1];
                }
            }
        }

        /// <summary>
        /// The output directory.
        /// </summary>
        public string OutputDirectory { get; }

        /// <summary>
        /// The output file.
        /// </summary>>
        public string OutputFile { get; }

        /// <summary>
        /// The output format.
        /// </summary>
        public OutputFormat OutputFormat { get; }

        /// <summary>
        /// Output the metadata.
        /// </summary>
        public MetadataFormat OutputMetadata { get; }

        /// <summary>
        /// The metadata output file.
        /// </summary>>
        public string MetadataOutputFile { get; }

        /// <summary>
        /// Gzip the output file.
        /// </summary>
        public bool Gzip { get; }

        public bool NoPeakPicking { get; }

        public bool PrecursorIntensity { get; }

        public bool NoZlibCompression { get; }

        public LogFormat LogFormat { get; }

        public bool IgnoreInstrumentErrors { get; }

        private S3Loader S3Loader { get; set; }

        private string S3AccessKeyId { get; }

        private string S3SecretAccessKey { get; }

        private string S3url { get; }

        private readonly string bucketName;

        /// <summary>
        /// The raw file name.
        /// </summary>
        private string rawFileName;

        /// <summary>
        /// The RAW file name without extension.
        /// </summary>
        public string RawFileNameWithoutExtension { get; private set; }

        public ParseInput(string rawFilePath, string rawDirectoryPath, string outputDirectory, string outputFile,
            OutputFormat outputFormat
        )
        {
            RawFilePath = rawFilePath;
            RawDirectoryPath = rawDirectoryPath;
            OutputDirectory = outputDirectory;
            OutputFile = outputFile;
            OutputFormat = outputFormat;
            OutputMetadata = MetadataFormat.NONE;
            Gzip = false;
            NoPeakPicking = false;
            PrecursorIntensity = false;
            NoZlibCompression = false;
            LogFormat = LogFormat.DEFAULT;
            IgnoreInstrumentErrors = false;

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

        public ParseInput(string rawFilePath, string rawDirectoryPath, string outputDirectory, string outputFile,
            OutputFormat outputFormat, MetadataFormat outputMetadata, string metadataOutputFile, bool gzip,
            bool noPeakPicking, bool precursorIntensity, bool noZlibCompression, LogFormat logFormat,
            bool ignoreInstrumentErrors, string s3url, string s3AccessKeyId, string s3SecretAccessKey, string bucketName
        )
        {
            RawFilePath = rawFilePath;
            RawDirectoryPath = rawDirectoryPath;
            OutputDirectory = outputDirectory;
            OutputFile = outputFile;
            OutputFormat = outputFormat;
            OutputMetadata = outputMetadata;
            MetadataOutputFile = metadataOutputFile;
            Gzip = gzip;
            NoPeakPicking = noPeakPicking;
            PrecursorIntensity = precursorIntensity;
            NoZlibCompression = noZlibCompression;
            LogFormat = logFormat;
            IgnoreInstrumentErrors = ignoreInstrumentErrors;
            S3url = s3url;
            S3AccessKeyId = s3AccessKeyId;
            S3SecretAccessKey = s3SecretAccessKey;
            this.bucketName = bucketName;

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