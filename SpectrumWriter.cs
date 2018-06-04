using System;
using System.IO;
using System.Text;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser
{
    public abstract class SpectrumWriter : ISpectrumWriter
    {
        protected readonly string _rawFilePath;
        protected readonly string _outputDirectory;
        protected readonly string _collection;
        protected readonly string _msRun;
        protected readonly string _subFolder;
        protected static string _rawFileName;
        protected static string _rawFileNameWithoutExtension;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rawFilePath">the RAW file path</param>
        /// <param name="outputDirectory">the output directory</param>
        /// <param name="collection">the collection identifier</param>
        /// <param name="msRun">the MS run identifier</param>
        /// <param name="subFolder">the sub folder directory</param>
        protected SpectrumWriter(string rawFilePath, string outputDirectory, string collection, string msRun,
            string subFolder)
        {
            _rawFilePath = rawFilePath;
            string[] splittedPath = _rawFilePath.Split('/');
            _rawFileName = splittedPath[splittedPath.Length - 1];
            _rawFileNameWithoutExtension = Path.GetFileNameWithoutExtension(_rawFileName);
            _outputDirectory = outputDirectory;
            _collection = collection;
            _msRun = msRun;
            _subFolder = subFolder;
        }

        /// <inheritdoc />
        public abstract void WriteSpectra(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber);

        /// <summary>
        /// Construct the spectrum title.
        /// </summary>
        /// <param name="scanNumber">the spectrum scan number</param>
        protected string ConstructSpectrumTitle(int scanNumber)
        {
            StringBuilder spectrumTitle = new StringBuilder("mzspec:");

            if (_collection != null)
            {
                spectrumTitle.Append(_collection).Append(":");
            }

            if (_subFolder != null)
            {
                spectrumTitle.Append(_subFolder).Append(":");
            }

            if (_msRun != null)
            {
                spectrumTitle.Append(_msRun).Append(":");
            }
            else
            {
                spectrumTitle.Append(_rawFileName).Append(":");
            }

            spectrumTitle.Append("scan:");
            spectrumTitle.Append(scanNumber);

            return spectrumTitle.ToString();
        }

        /// <summary>
        /// Get the spectrum intensity.
        /// </summary>
        /// <param name="rawFile">the RAW file object</param>
        /// <param name="scanNumber">the scan number</param>
        protected double GetPrecursorIntensity(IRawDataPlus rawFile, int precursorScanNumber, int scanNumber)
        {
            // Define the settings for getting the Base Peak chromatogram            
            ChromatogramTraceSettings settings = new ChromatogramTraceSettings(TraceType.TIC);

            // Get the chromatogram from the RAW file. 
            var data = rawFile.GetChromatogramData(new IChromatogramSettings[] {settings}, scanNumber -1,
                scanNumber - 1);

            // Split the data into the chromatograms
            var trace = ChromatogramSignal.FromChromatogramData(data);

            return trace[0].Intensities[0];
        }
    }
}