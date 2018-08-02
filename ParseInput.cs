using System.IO;

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

        public ParseInput(string rawFilePath, string outputDirectory, OutputFormat outputFormat, bool gzip,
            MetadataFormat outputMetadata, bool excludeProfileData, string collection, string msRun, string subFolder)
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
        }
    }
}