using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using Parquet;
using Parquet.Data;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParser.Writer
{
    public class ParquetSpectrumWriter : SpectrumWriter
    {
        private static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IRawDataPlus _rawFile;

        // Dictionary to keep track of the different mass analyzers (key: Thermo MassAnalyzerType; value: the reference string)       
        private readonly Dictionary<MassAnalyzerType, string> _massAnalyzers =
            new Dictionary<MassAnalyzerType, string>();

        private readonly Dictionary<IonizationModeType, CVParamType> _ionizationTypes =
            new Dictionary<IonizationModeType, CVParamType>();

        public ParquetSpectrumWriter(ParseInput parseInput) : base(parseInput)
        {
        }

        public override void Write(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            _rawFile = rawFile;
            List<PScan> pScans = new List<PScan>();
            WritePScans(ParseInput.OutputDirectory, rawFile.FileName, rawFile, pScans);
        }

        private static void WritePScans(string outputDirectory, string fileName,
            IRawDataPlus raw,
            List<PScan> scans)
        {
            var enumerator = raw.GetFilteredScanEnumerator(" ");

            foreach (var scanNumber in enumerator
            ) // note in my tests serial is faster than Parallel.Foreach() (this involves disk access, so it makes sense)
            {
                //trailer information is extracted via index
                var trailers = raw.GetTrailerExtraValues(scanNumber);
                var trailerLabels = raw.GetTrailerExtraInformation(scanNumber);
                object chargeState = 0;
                for (int i = 0; i < trailerLabels.Labels.Length; i++)
                {
                    if (trailerLabels.Labels[i] == "Charge State:")
                    {
                        chargeState = raw.GetTrailerExtraValue(scanNumber, i);
                        break;
                    }
                }

                var scanFilter = raw.GetFilterForScanNumber(scanNumber);
                var scanStats = raw.GetScanStatsForScanNumber(scanNumber);

                CentroidStream centroidStream = new CentroidStream();

                //check for FT mass analyzer data
                if (scanFilter.MassAnalyzer == MassAnalyzerType.MassAnalyzerFTMS)
                {
                    centroidStream = raw.GetCentroidStream(scanNumber, false);
                }

                //check for IT mass analyzer data
                if (scanFilter.MassAnalyzer == MassAnalyzerType.MassAnalyzerITMS)
                {
                    var scanData = raw.GetSimplifiedScan(scanNumber);
                    centroidStream.Masses = scanData.Masses;
                    centroidStream.Intensities = scanData.Intensities;
                }

                var msOrder = raw.GetScanEventForScanNumber(scanNumber).MSOrder;

                if (msOrder == MSOrderType.Ms)
                {
                    var pscan = GetPScan(scanStats, centroidStream, fileName, Convert.ToInt32(chargeState));
                    scans.Add(pscan);
                }

                if (msOrder == MSOrderType.Ms2)
                {
                    var precursorMz = raw.GetScanEventForScanNumber(scanNumber).GetReaction(0).PrecursorMass;
                    var pscan = GetPScan(scanStats, centroidStream, fileName, precursorMz,
                        Convert.ToInt32(chargeState));
                    scans.Add(pscan);
                }

                var t = raw.GetTrailerExtraValues(scanNumber);
            }

            WriteScans(outputDirectory, scans, fileName);
        }

        private static PScan GetPScan(ScanStatistics scanStats, CentroidStream centroidStream,
            string fileName, double? precursorMz = null, int? precursorCharge = null)
        {
            var scan = new PScan
            {
                FileName = fileName,
                BasePeakMass = scanStats.BasePeakMass,
                ScanType = scanStats.ScanType,
                BasePeakIntensity = scanStats.BasePeakIntensity,
                PacketType = scanStats.PacketType,
                ScanNumber = scanStats.ScanNumber,
                RetentionTime = scanStats.StartTime,
                Masses = centroidStream.Masses.ToArray(),
                Intensities = centroidStream.Intensities.ToArray(),
                LowMass = scanStats.LowMass,
                HighMass = scanStats.HighMass,
                TIC = scanStats.TIC,
                FileId = fileName,
                PrecursorMz = precursorMz,
                PrecursorCharge = precursorCharge,
                MsOrder = 1
            };
            return scan;
        }

        public static void WriteScans(string outputDirectory, List<PScan> scans, string sourceRawFileName)
        {
            var output = outputDirectory + "//" + Path.GetFileNameWithoutExtension(sourceRawFileName);

            var ds = new DataSet(new DataField<double>("BasePeakIntensity"),
                new DataField<double>("BasePeakMass"),
                new DataField<double[]>("Baselines"),
                new DataField<double[]>("Charges"),
                new DataField<string>("FileId"),
                new DataField<string>("FileName"),
                new DataField<double>("HighMass"),
                new DataField<double[]>("Intensities"),
                new DataField<double>("LowMass"),
                new DataField<double[]>("Masses"),
                new DataField<int>("MsOrder"),
                new DataField<double[]>("Noises"),
                new DataField<int>("PacketType"),
                new DataField<int?>("PrecursorCharge"),
                new DataField<double?>("PrecursorMass"),
                new DataField<double[]>("Resolutions"),
                new DataField<double>("RetentionTime"),
                new DataField<int>("ScanNumber"),
                new DataField<string>("ScanType"),
                new DataField<double>("TIC"));

            foreach (var scan in scans)
            {
                //we can't store null values?
                double[] dummyVal = new double[1];
                if (scan.Noises == null)
                {
                    scan.Noises = dummyVal;
                }

                if (scan.Charges == null)
                {
                    scan.Charges = dummyVal;
                }

                if (scan.Baselines == null)
                {
                    scan.Baselines = dummyVal;
                }

                if (scan.Resolutions == null)
                {
                    scan.Resolutions = dummyVal;
                }

                if (scan.PrecursorMz == null)
                {
                    scan.PrecursorMz = 0;
                    scan.PrecursorCharge = 0;
                }

                ds.Add(scan.BasePeakIntensity,
                    scan.BasePeakMass,
                    scan.Baselines,
                    scan.Charges,
                    scan.FileId,
                    scan.FileName,
                    scan.HighMass,
                    scan.Intensities,
                    scan.LowMass,
                    scan.Masses,
                    scan.MsOrder,
                    scan.Noises,
                    scan.PacketType,
                    scan.PrecursorCharge,
                    scan.PrecursorMz,
                    scan.Resolutions,
                    scan.RetentionTime,
                    scan.ScanNumber,
                    scan.ScanType,
                    scan.TIC);
            }

            using (Stream fileStream = File.OpenWrite(output + ".parquet"))
            {
                using (var writer = new ParquetWriter(fileStream))
                {
                    writer.Write(ds);
                }
            }
        }
    }

    /// PSCan meaing Parsec Scan (because Commoncore has a Scan class)
    /// </summary>
    public class PScan
    {
        /// <summary>
        /// Unique ID per file (foreign key in data store)
        /// </summary>
        public string FileId { get; set; }

        public string FileName { get; set; }
        public double BasePeakIntensity { get; set; }
        public double BasePeakMass { get; set; }
        public double[] Baselines { get; set; }
        public double[] Charges { get; set; }
        public double HighMass { get; set; }
        public double[] Intensities { get; set; }
        public double LowMass { get; set; }
        public double[] Masses { get; set; }
        public double[] Noises { get; set; }
        public int PacketType { get; set; } // : 20,
        public double RetentionTime { get; set; }
        public double[] Resolutions { get; set; }
        public int ScanNumber { get; set; }
        public string ScanType { get; set; } // : FTMS + c ESI d Full ms2 335.9267@hcd30.00 [130.0000-346.0000],
        public double TIC { get; set; }
        public int MsOrder { get; set; }
        public double? PrecursorMz { get; set; }
        public int? PrecursorCharge { get; set; }
    }
}