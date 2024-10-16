using System.Collections.Generic;
using System.IO;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParser
{
    public class ParseInput
    {
        // All MS levels
        public static HashSet<int> AllLevels { get => new HashSet<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }; }

        /// <summary>
        /// The RAW file path.
        /// </summary>
        private string _rawFilePath;

        private string _userProvidedFilePath;

        private int _errors;

        private int _warnings;

        /// <summary>
        /// The RAW folder path.
        /// </summary>
        public string RawDirectoryPath { get; set; }

        public string RawFilePath
        {
            get => _rawFilePath;
            set
            {
                _rawFilePath = value;
                _userProvidedFilePath = value;
                if (value != null)
                {
                    RawFileNameWithoutExtension = Path.GetFileNameWithoutExtension(value);
                }
            }
        }
        public string UserProvidedPath
        {
            get => _userProvidedFilePath;
        }

        public int Errors
        {
            get => _errors;
        }
        public int Warnings
        {
            get => _warnings;
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

        public HashSet<int> NoPeakPicking { get; set; }

        public bool NoZlibCompression { get; set; }

        public bool AllDetectors { get; set; }

        public LogFormat LogFormat { get; set; }

        public bool IgnoreInstrumentErrors { get; set; }

        public bool ExData { get; set; }

        public HashSet<int> MsLevel { get; set; }

        public int MaxLevel { get; set; }

        public bool MgfPrecursor { get; set; }

        public bool NoiseData { get; set; }

        public bool StdOut { get; set; }

        public bool Vigilant { get; set; }

        private S3Loader S3Loader { get; set; }

        public string S3AccessKeyId { get; set; }

        public string S3SecretAccessKey { get; set; }

        public string S3Url { get; set; }

        public string BucketName { get; set; }

        /// <summary>
        /// The RAW file name without extension.
        /// </summary>
        public string RawFileNameWithoutExtension { get; private set; }

        public ParseInput()
        {
            MetadataFormat = MetadataFormat.None;
            OutputFormat = OutputFormat.None;
            Gzip = false;
            NoPeakPicking = new HashSet<int>();
            NoZlibCompression = false;
            LogFormat = LogFormat.DEFAULT;
            IgnoreInstrumentErrors = false;
            AllDetectors = false;
            MsLevel = AllLevels;
            MgfPrecursor = false;
            StdOut = false;
            NoiseData = false;
            Vigilant = false;
            _errors = 0;
            _warnings = 0;
            MaxLevel = 10;
        }

        public ParseInput(string rawFilePath, string rawDirectoryPath, string outputDirectory, OutputFormat outputFormat
        ) : this()
        {
            RawFilePath = rawFilePath;
            RawDirectoryPath = rawDirectoryPath;
            OutputDirectory = outputDirectory;
            OutputFormat = outputFormat;
            MgfPrecursor = true;
        }

        public void InitializeS3Bucket()
        {
            S3Loader = new S3Loader(S3Url, S3AccessKeyId, S3SecretAccessKey, BucketName);
        }

        public void NewError()
        {
            _errors++;
        }

        public void NewWarn()
        {
            _warnings++;
        }

        public void UpdateRealPath(string path)
        {
            if (path != null)
            {
                _userProvidedFilePath = _rawFilePath;
                _rawFilePath = path;
                
            }
        }
    }
}