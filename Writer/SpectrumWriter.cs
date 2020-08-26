using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using log4net;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.Writer
{
    public abstract class SpectrumWriter : ISpectrumWriter
    {
        private static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string MsFilter = "ms";
        private const double Tolerance = 0.01;
        protected const double ZeroDelta = 0.0001;

        /// <summary>
        /// The progress step size in percentage.
        /// </summary>
        protected const int ProgressPercentageStep = 10;

        private const double PrecursorMzDelta = 0.0001;
        private const double DefaultIsolationWindowLowerOffset = 1.5;
        private const double DefaultIsolationWindowUpperOffset = 2.5;

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
        /// <param name="extension">The extension of the output file</param>
        protected void ConfigureWriter(string extension)
        {
            if (ParseInput.OutputFile == null)
            {
                var fullExtension = ParseInput.Gzip ? extension + ".gzip" : extension;
                if (!ParseInput.Gzip || ParseInput.OutputFormat == OutputFormat.IndexMzML)
                {
                    Writer = File.CreateText(ParseInput.OutputDirectory + "//" +
                                             ParseInput.RawFileNameWithoutExtension +
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
            else
            {
                if (!ParseInput.Gzip || ParseInput.OutputFormat == OutputFormat.IndexMzML)
                {
                    Writer = File.CreateText(ParseInput.OutputFile);
                }
                else
                {
                    var fileName = ParseInput.OutputFile;
                    if (ParseInput.Gzip && !Path.GetExtension(fileName).Equals(".gzip"))
                    {
                        fileName = ParseInput.OutputFile + ".gzip";
                    }

                    var fileStream = File.Create(fileName);
                    var compress = new GZipStream(fileStream, CompressionMode.Compress);
                    Writer = new StreamWriter(compress);
                }
            }
        }

        /// <summary>
        /// Construct the spectrum title.
        /// </summary>
        /// <param name="scanNumber">the spectrum scan number</param>
        protected static string ConstructSpectrumTitle(int instrumentType, int instrumentNumber, int scanNumber)
        {
            return String.Format("controllerType={0} controllerNumber={1} scan={2}", instrumentType, instrumentNumber, scanNumber);
        }

        /// <summary>
        /// Calculate the selected ion m/z value. This is necessary because the precursor mass found in the reaction
        /// isn't always the monoisotopic mass.
        /// https://github.com/ProteoWizard/pwiz/blob/master/pwiz/data/vendor_readers/Thermo/SpectrumList_Thermo.cpp#L564-L574
        /// </summary>
        /// <param name="reaction">the scan event reaction</param>
        /// <param name="monoisotopicMz">the monoisotopic m/z value</param>
        /// <param name="isolationWidth">the scan event reaction</param>
        public static double CalculateSelectedIonMz(IReaction reaction, double? monoisotopicMz,
            double? isolationWidth)
        {
            var selectedIonMz = reaction.PrecursorMass;

            // take the isolation width from the reaction if no value was found in the trailer data
            if (isolationWidth == null || isolationWidth < ZeroDelta)
            {
                isolationWidth = reaction.IsolationWidth;
            }

            isolationWidth /= 2;

            if (monoisotopicMz != null && monoisotopicMz > ZeroDelta
                                       && Math.Abs(
                                           reaction.PrecursorMass - monoisotopicMz.Value) >
                                       PrecursorMzDelta)
            {
                selectedIonMz = monoisotopicMz.Value;

                // check if the monoisotopic mass lies in the precursor mass isolation window
                // otherwise take the precursor mass                                    
                if (isolationWidth <= 2.0)
                {
                    if ((selectedIonMz <
                         (reaction.PrecursorMass - DefaultIsolationWindowLowerOffset * 2)) ||
                        (selectedIonMz >
                         (reaction.PrecursorMass + DefaultIsolationWindowUpperOffset)))
                    {
                        selectedIonMz = reaction.PrecursorMass;
                    }
                }
                else if ((selectedIonMz < (reaction.PrecursorMass - isolationWidth)) ||
                         (selectedIonMz > (reaction.PrecursorMass + isolationWidth)))
                {
                    selectedIonMz = reaction.PrecursorMass;
                }
            }

            return selectedIonMz;
        }

        public static IReaction GetReaction(IScanEvent scanEvent, int scanNumber)
        {
            IReaction reaction = null;
            try
            {
                var order = (int) scanEvent.MSOrder;
                reaction = scanEvent.GetReaction(order - 2);
            }
            catch (ArgumentOutOfRangeException)
            {
                Log.Warn("No reaction found for scan " + scanNumber);
            }

            return reaction;
        }

        /// <summary>
        /// Calculate the precursor peak intensity.
        /// </summary>
        /// <param name="rawFile">the RAW file object</param>
        /// <param name="precursorScanNumber">the precursor scan number</param>
        /// <param name="precursorMass">the precursor mass</param>
        protected static double? CalculatePrecursorPeakIntensity(IRawDataPlus rawFile, int precursorScanNumber,
            double precursorMass)
        {
            double? precursorIntensity = null;
            double tolerance;

            // Get the precursor scan from the RAW file
            var scan = Scan.FromFile(rawFile, precursorScanNumber);

            //Select centroid stream if it exists, otherwise use profile one
            double[] masses;
            double[] intensities;

            if (scan.HasCentroidStream)
            {
                masses = scan.CentroidScan.Masses;
                intensities = scan.CentroidScan.Intensities;
                tolerance = 0.01; //high resolution scan
            }
            else
            {
                masses = scan.SegmentedScan.Positions;
                intensities = scan.SegmentedScan.Intensities;
                tolerance = 0.5; //low resolution scan
            }

            //find closest peak in a stream
            var bestDelta = tolerance;
            for (var i = 0; i < masses.Length; i++)
            {
                var delta = precursorMass - masses[i];
                if (Math.Abs(delta) < bestDelta)
                {
                    bestDelta = delta;
                    precursorIntensity = intensities[i];
                }

                if (delta < -1 * tolerance) break;
            }

            return precursorIntensity;
        }
    }
}