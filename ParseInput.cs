using System.IO;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParser
{
    public class ParseInput
    {
        /// <summary>
        /// The RAW folder path.
        /// </summary>
        public string RawFolderPath { get; }

        /// <summary>
        /// The RAW file path.
        /// </summary>
        private string rawFilePath;
        public string RawFilePath { 
            get { return rawFilePath; }
            set
            {
                rawFilePath = value;
                if (value != null)
                {
                    rawFileNameWithoutExtension = Path.GetFileNameWithoutExtension(value);
                    string[] splittedPath = value.Split('/');
                    rawFileName = splittedPath[splittedPath.Length - 1];
                }
            }
        }

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
        /// The raw file name.
        /// </summary>
        private string rawFileName;
        public string RawFileName
        {
            get { return rawFileName; }
            set
            {
                rawFileName = value;
            }
        }

        /// <summary>
        /// The RAW file name without extension.
        /// </summary>
        private string rawFileNameWithoutExtension;
        public string RawFileNameWithoutExtension {
            get { return rawFileNameWithoutExtension; }
        }

        private S3Loader S3Loader { get; set; }

        private string S3AccessKeyId { get; }

        private string S3SecretAccessKey { get; }

        private string S3url { get; }

        public bool IgnoreInstrumentErrors { get; }

        public bool NoPeakPicking { get; }

        public bool NoZlibCompression { get; }

        public bool Verbose { get; }

        private readonly string bucketName;

        public ParseInput(string rawFolderPath, string rawFilePath, string outputDirectory, string outputFile, OutputFormat outputFormat
        )
        {
            RawFolderPath = rawFolderPath;
            RawFilePath = rawFilePath;
            OutputDirectory = outputDirectory;
            OutputFile = outputFile;
            OutputFormat = outputFormat;
            Gzip = false;
            OutputMetadata = MetadataFormat.NONE;
            IgnoreInstrumentErrors = false;
            NoPeakPicking = false;
            NoZlibCompression = false;
            Verbose = false;

            if (S3url != null && S3AccessKeyId != null && S3SecretAccessKey != null && bucketName != null)
                InitializeS3Bucket();

            if (OutputDirectory == null && OutputFile != null)
                OutputDirectory = Path.GetDirectoryName(OutputFile);
        }

        public ParseInput(string rawFolderPath, string rawFilePath, string outputDirectory, string outputFile, OutputFormat outputFormat,
            bool gzip,
            MetadataFormat outputMetadata, string s3url, string s3AccessKeyId,
            string s3SecretAccessKey, string bucketName,
            bool ignoreInstrumentErrors, bool noPeakPicking, bool noZlibCompression, bool verbose
        )
        {
            RawFolderPath = rawFolderPath;
            RawFilePath = rawFilePath;
            OutputDirectory = outputDirectory;
            OutputFile = outputFile;
            OutputFormat = outputFormat;
            Gzip = gzip;
            OutputMetadata = outputMetadata;
            S3url = s3url;
            S3AccessKeyId = s3AccessKeyId;
            S3SecretAccessKey = s3SecretAccessKey;
            this.bucketName = bucketName;
            IgnoreInstrumentErrors = ignoreInstrumentErrors;
            NoPeakPicking = noPeakPicking;
            NoZlibCompression = noZlibCompression;
            Verbose = verbose;

            if (S3url != null && S3AccessKeyId != null && S3SecretAccessKey != null && bucketName != null)
                InitializeS3Bucket();

            if (OutputDirectory == null && OutputFile != null)
                OutputDirectory = Path.GetDirectoryName(OutputFile);
        }

        private void InitializeS3Bucket()
        {
            S3Loader = new S3Loader(S3url, S3AccessKeyId, S3SecretAccessKey, bucketName);
        }
    }
}