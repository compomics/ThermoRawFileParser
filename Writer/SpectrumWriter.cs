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
        protected StreamWriter Writer;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parseInput">the parse input object</param>
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
        protected void ConfigureWriter(string extension)
        {
            var fullExtension = ParseInput.Gzip ? extension + ".gzip" : extension;
            if (!ParseInput.Gzip)
            {
                Writer = File.CreateText(ParseInput.OutputDirectory + "//" + ParseInput.RawFileNameWithoutExtension +
                                         extension);
            }
            else
            {
                var fileStream = File.Create(ParseInput.OutputDirectory + "//" +
                                                    ParseInput.RawFileNameWithoutExtension + fullExtension);
                var compress = new GZipStream(fileStream, CompressionMode.Compress);
                Writer = new StreamWriter(compress);
            }
        }

        /// <summary>
        /// Construct the spectrum title.
        /// </summary>
        /// <param name="scanNumber">the spectrum scan number</param>
        protected string ConstructSpectrumTitle(int scanNumber)
        {
            var spectrumTitle = new StringBuilder("mzspec=");

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

            spectrumTiple.Append(" ");
            spectrumTitle.Append("scan=");
            spectrumTitle.Append(scanNumber);

            return spectrumTitle.ToString();
        }

        /// <summary>
        /// Get the spectrum intensity.
        /// </summary>
        /// <param name="rawFile">the RAW file object</param>
        /// <param name="precursorScanNumber">the precursor scan number</param>
        /// <param name="scanNumber">the scan number</param>
        protected double GetPrecursorIntensity(IRawDataPlus rawFile, int precursorScanNumber, int scanNumber)
        {
            // Define the settings for getting the Base Peak chromatogram            
            var settings = new ChromatogramTraceSettings(TraceType.TIC);

            // Get the chromatogram from the RAW file. 
            var data = rawFile.GetChromatogramData(new IChromatogramSettings[] {settings}, scanNumber - 1,
                scanNumber - 1);

            // Split the data into the chromatograms
            var trace = ChromatogramSignal.FromChromatogramData(data);

            return trace[0].Intensities[0];
        }
    }
}