using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.Writer
{
    public abstract class SpectrumWriter : ISpectrumWriter
    {
        /// <summary>
        /// The parse input object
        /// </summary>
        protected readonly ParseInput ParseInput;
        /// <summary>
        /// The output stream writer
        /// </summary>
        protected StreamWriter writer;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rawFilePath">the RAW file path</param>
        protected SpectrumWriter(ParseInput parseInput)
        {
            ParseInput = parseInput;
        }

        /// <inheritdoc />
        public abstract void Write(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber);
        
        /// <summary>
        /// Configure the output writer
        /// </summary>
        /// <param name="extension">The exenstion of the output file</param>
        protected void ConfigureWriter(String extension)
        {
            String fullExtension = ParseInput.Gzip ? extension + ".gzip" : extension;
            if (!ParseInput.Gzip)
            {
                writer = File.CreateText(ParseInput.OutputDirectory + "//" + ParseInput.RawFileNameWithoutExtension +
                                         extension);
            }
            else
            {
                FileStream fileStream = File.Create(ParseInput.OutputDirectory + "//" +
                                                    ParseInput.RawFileNameWithoutExtension + fullExtension);
                GZipStream compress = new GZipStream(fileStream, CompressionMode.Compress);
                writer = new StreamWriter(compress);
            }
        }

        /// <summary>
        /// Construct the spectrum title.
        /// </summary>
        /// <param name="scanNumber">the spectrum scan number</param>
        protected string ConstructSpectrumTitle(int scanNumber)
        {
            StringBuilder spectrumTitle = new StringBuilder("mzspec:");

            if (ParseInput.Collection != null)
            {
                spectrumTitle.Append(ParseInput.Collection).Append(":");
            }

            if (ParseInput.SubFolder != null)
            {
                spectrumTitle.Append(ParseInput.SubFolder).Append(":");
            }

            if (ParseInput.MsRun != null)
            {
                spectrumTitle.Append(ParseInput.MsRun).Append(":");
            }
            else
            {
                spectrumTitle.Append(ParseInput.RawFileName).Append(":");
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
            var data = rawFile.GetChromatogramData(new IChromatogramSettings[] {settings}, scanNumber - 1,
                scanNumber - 1);

            // Split the data into the chromatograms
            var trace = ChromatogramSignal.FromChromatogramData(data);

            return trace[0].Intensities[0];
        }
    }
}