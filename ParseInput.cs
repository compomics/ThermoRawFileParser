using System;
using System.Collections.Generic;
using System.IO;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParser
{
    public class ParseInput
    {
        //all ms levels
        private readonly HashSet<int> allLevels = new HashSet<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        /// <summary>
        /// The RAW file path.
        /// </summary>
        private string rawFilePath;

        /// <summary>
        /// The RAW folder path.
        /// </summary>
        public string RawDirectoryPath { get; set; }

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
        public string OutputDirectory { get; set; }

        /// <summary>
        /// The output file.
        /// </summary>>
        public string OutputFile { get; set; }

        /// <summary>
        /// The output format.
        /// </summary>
        public OutputFormat OutputFormat { get; set; }

        /// <summary>
        /// The metadata output format.
        /// </summary>
        public MetadataFormat MetadataFormat { get; set; }

        /// <summary>
        /// The metadata output file.
        /// </summary>>
        public string MetadataOutputFile { get; set; }

        /// <summary>
        /// Gzip the output file.
        /// </summary>
        public bool Gzip { get; set; }

        public bool NoPeakPicking { get; set; }

        public bool NoZlibCompression { get; set; }

        public bool AllDetectors { get; set; }

        public LogFormat LogFormat { get; set; }

        public bool IgnoreInstrumentErrors { get; set; }

        public bool ExData { get; set; }

        public HashSet<int> MsLevel { get; set; }

        public bool MGFPrecursor { get; set; }

        public bool StdOut { get; set; }

        private S3Loader S3Loader { get; set; }

        public string S3AccessKeyId { get; set; }

        public string S3SecretAccessKey { get; set; }

        public string S3Url { get; set; }

        public string BucketName { get; set; }

        /// <summary>
        /// The raw file name.
        /// </summary>
        private string rawFileName;

        /// <summary>
        /// The RAW file name without extension.
        /// </summary>
        public string RawFileNameWithoutExtension { get; private set; }

        public ParseInput()
        {
            MetadataFormat = MetadataFormat.NONE;
            OutputFormat = OutputFormat.NONE;
            Gzip = false;
            NoPeakPicking = false;
            NoZlibCompression = false;
            LogFormat = LogFormat.DEFAULT;
            IgnoreInstrumentErrors = false;
            AllDetectors = false;
            MsLevel = allLevels;
            StdOut = false;
        }

        public ParseInput(string rawFilePath, string rawDirectoryPath, string outputDirectory, OutputFormat outputFormat
        ) : this()
        {
            RawFilePath = rawFilePath;
            RawDirectoryPath = rawDirectoryPath;
            OutputDirectory = outputDirectory;
            OutputFormat = outputFormat;
        }

        public void InitializeS3Bucket()
        {
            S3Loader = new S3Loader(S3Url, S3AccessKeyId, S3SecretAccessKey, BucketName);
        }
    }
}