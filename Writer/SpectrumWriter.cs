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
        private const double Tolerance = 0.01;  
        protected const double ZeroDelta = 0.0001;

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
            var fullExtension = ParseInput.Gzip ? extension + ".gzip" : extension;
            if (!ParseInput.Gzip || ParseInput.OutputFormat == OutputFormat.IndexMzML)
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

        protected string GetFullPath()
        {
            var fs = (FileStream) Writer.BaseStream;
            return fs.Name;
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
        /// Get the spectrum intensity.
        /// </summary>
        /// <param name="rawFile">the RAW file object</param>
        /// <param name="precursorScanNumber">the precursor scan number</param>
        protected static double? GetPrecursorIntensity(IRawDataPlus rawFile, int precursorScanNumber,
            double precursorMass, double retentionTime, double? isolationWidth)
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

                var component = new Component
                {
                    MassRange = new Limit
                    {
                        Low = (double) (precursorMass - isolationWidth / 2),
                        High = (double) (precursorMass + isolationWidth / 2)
                    },
                    RtRange = new Limit
                    {
                        Low = rawFile.RetentionTimeFromScanNumber(precursorScanNumber),
                        High = rawFile.RetentionTimeFromScanNumber(precursorScanNumber)
                    }
                };
                ;

                IChromatogramSettings[] allSettings =
                {
                    new ChromatogramTraceSettings(TraceType.MassRange)
                    {
                        Filter = Component.Filter,
                        MassRanges = new[]
                        {
                            new Range(component.MassRange.Low, component.MassRange.High)
                        }
                    }
                };

                var rtFilteredScans = rawFile.GetFilteredScansListByTimeRange("",
                    component.RtRange.Low,
                    component.RtRange.High);
                var data = rawFile.GetChromatogramData(allSettings, rtFilteredScans[0],
                    rtFilteredScans[rtFilteredScans.Count - 1]);

                var chromatogramTrace = ChromatogramSignal.FromChromatogramData(data);
            }

            return precursorIntensity;
        }
    }

    public class Limit
    {
        public double Low { get; set; }
        public double High { get; set; }
    }

    public class Component
    {
        public Limit RtRange { get; set; }
        public Limit MassRange { get; set; }
        public static string Filter { get; set; }
        public string Name { get; set; }
    }
}