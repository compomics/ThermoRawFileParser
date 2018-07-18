using System.IO;

namespace ThermoRawFileParser
{
    public class ParseInput
    {
        /// <summary>
        /// The RAW file path.
        /// </summary>
        public string RawFilePath { get; set; }

        /// <summary>
        /// The output directory.
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// The output format.
        /// </summary>
        public OutputFormat OutputFormat { get; set; }

        /// <summary>
        /// Gzip the output file.
        /// </summary>
        public bool Gzip { get; set; }

        /// <summary>
        /// Output the metadata.
        /// </summary>
        public bool OutputMetadata { get; set; }

        /// <summary>
        /// Exclude the MS2 spectra in profile mode.
        /// </summary>
        public bool ExcludeProfileData { get; set; }

        /// <summary>
        /// The data collection identifier.
        /// </summary>  
        public string Collection { get; set; }

        /// <summary>
        /// Mass spectrometry run name.
        /// </summary>
        public string MsRun { get; set; }

        /// <summary>
        /// This property is used disambiguate instances where the same collection
        /// has two or more msRuns with the same name.
        /// </summary>
        public string SubFolder { get; set; }

        /// <summary>
        /// The raw file name.
        /// </summary>
        public string RawFileName { get; set; }

        /// <summary>
        /// The RAW file name without extension.
        /// </summary>
        public string RawFileNameWithoutExtension { get; set; }

        public ParseInput()
        {
        }

        public ParseInput(string rawFilePath, string outputDirectory, OutputFormat outputFormat, bool gzip,
            bool outputMetadata, bool excludeProfileData, string collection, string msRun, string subFolder)
        {
            RawFilePath = rawFilePath;
            string[] splittedPath = RawFilePath.Split('/');
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
        }
    }
}