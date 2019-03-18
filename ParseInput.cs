using System.ComponentModel;
using System.IO;
using NUnit.Framework.Constraints;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParser
{
    public class ParseInput
    {
        private string bucketName;
        private bool ignoreInstrumentErrors;

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
        /// Exclude the MS2 spectra in profile mode.
        /// </summary>
        public bool ExcludeProfileData { get; }

        /// <summary>
        /// The data collection identifier.
        /// </summary>  
        public string Collection { get; }

        /// <summary>
        /// Mass spectrometry run name.
        /// </summary>
        public string MsRun { get; }

        /// <summary>
        /// This property is used disambiguate instances where the same collection
        /// has two or more msRuns with the same name.
        /// </summary>
        public string SubFolder { get; }

        /// <summary>
        /// The raw file name.
        /// </summary>
        public string RawFileName { get; }

        /// <summary>
        /// The RAW file name without extension.
        /// </summary>
        public string RawFileNameWithoutExtension { get; }

        public log4net.ILog Log { get; }
        
        public S3Loader S3loader { get; set; }
        
        public string S3AccessKeyId { get; set; }

        public string S3SecretAccessKey { get; set; }
        
        public string S3url { get; set; }
        
        public bool IgnoreInstrumentErrors
        {
            get => ignoreInstrumentErrors;
            set => ignoreInstrumentErrors = value;
        }


        public ParseInput(string rawFilePath, string outputDirectory, OutputFormat outputFormat, bool gzip,
            MetadataFormat outputMetadata, bool excludeProfileData, string collection, string msRun, string subFolder, 
            log4net.ILog log, string s3url, string s3AccessKeyId, string s3SecretAccessKey, string bucketName, bool ignoreInstrumentErrors
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
            ExcludeProfileData = excludeProfileData;
            Collection = collection;
            MsRun = msRun;
            SubFolder = subFolder;
            Log = log;
            S3url = s3url; 
            S3AccessKeyId = s3AccessKeyId;
            S3SecretAccessKey = s3SecretAccessKey;
            this.bucketName = bucketName;
            this.ignoreInstrumentErrors = ignoreInstrumentErrors; 

            if (S3url != null && S3AccessKeyId != null && S3SecretAccessKey != null)
                initializeS3bucket(s3url, s3AccessKeyId, s3SecretAccessKey, bucketName); 

        }
        private void initializeS3bucket(string s3url, string s3AccessKeyId, string s3SecretAccessKey, string bucketName)
        {
            S3loader = new S3Loader(s3url, s3AccessKeyId, s3SecretAccessKey, bucketName);
        }
    }
}