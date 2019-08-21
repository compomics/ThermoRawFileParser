using System;
using System.IO;
using System.IO.Compression;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.Writer
{
    public abstract class SpectrumWriter : ISpectrumWriter
    {
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
        protected static string ConstructSpectrumTitle(int scanNumber)
        {
            return "controllerType=0 controllerNumber=1 scan=" + scanNumber;
        }

        /// <summary>
        /// Calculate the selected ion m/z value. This is necessary because the precursor mass found in the reaction
        /// isn't always the monoisotopic mass.
        /// https://github.com/ProteoWizard/pwiz/blob/master/pwiz/data/vendor_readers/Thermo/SpectrumList_Thermo.cpp#L564-L574
        /// </summary>
        /// <param name="reaction">the scan event reaction</param>
        /// <param name="monoisotopicMz">the monoisotopic m/z value</param>
        /// <param name="isolationWidth">the scan event reaction</param>
        protected static double CalculateSelectedIonMz(IReaction reaction, double? monoisotopicMz,
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

            // Get the scan from the RAW file
            var scan = Scan.FromFile(rawFile, precursorScanNumber);

            // Check if the scan has a centroid stream
            if (scan.HasCentroidStream)
            {
                var centroidStream = rawFile.GetCentroidStream(precursorScanNumber, false);
                if (scan.CentroidScan.Length > 0)
                {
                    for (var i = 0; i < centroidStream.Length; i++)
                    {
                        if (Math.Abs(precursorMass - centroidStream.Masses[i]) < Tolerance)
                        {
                            //Console.WriteLine(Math.Abs(precursorMass - centroidStream.Masses[i]));
                            //Console.WriteLine(precursorMass + " - " + centroidStream.Masses[i] + " - " +
                            //                  centroidStream.Intensities[i]);
                            precursorIntensity = centroidStream.Intensities[i];
                            break;
                        }
                    }
                }
            }
            else
            {
                rawFile.SelectInstrument(Device.MS, 1);

                IChromatogramSettings[] allSettings =
                {
                    new ChromatogramTraceSettings(TraceType.BasePeak)
                    {
                        Filter = MsFilter,
                        MassRanges = new[]
                        {
                            new Range(precursorMass, precursorMass)
                        }
                    }
                };

                var data = rawFile.GetChromatogramData(allSettings, precursorScanNumber,
                    precursorScanNumber);
                var chromatogramTrace = ChromatogramSignal.FromChromatogramData(data);
                if (!chromatogramTrace.IsNullOrEmpty())
                {
                    precursorIntensity = chromatogramTrace[0].Intensities[0];
                }
            }

            return precursorIntensity;
        }
    }
}