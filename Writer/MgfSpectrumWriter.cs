using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using log4net;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Util;

namespace ThermoRawFileParser.Writer
{
    public class MgfSpectrumWriter : SpectrumWriter
    {
        private static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string PositivePolarity = "+";
        private const string NegativePolarity = "-";

        // Filter string
        private const string FilterStringIsolationMzPattern = @"ms2 (.*?)@";

        // Precursor scan number for MS2 scans
        private int _precursorMs1ScanNumber;

        // Dictionary with isolation m/z (key) and precursor scan number (value) entries
        // for reference in the precursor element of an MS3 spectrum
        private readonly LimitedSizeDictionary<string, int> _isolationMzToPrecursorScanNumberMapping =
            new LimitedSizeDictionary<string, int>(40);

        public MgfSpectrumWriter(ParseInput parseInput) : base(parseInput)
        {
            ParseInput.MsLevel.Remove(1); // MS1 spectra are not supposed to be in MGF
        }

        /// <inheritdoc />       
        public override void Write(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            ConfigureWriter(".mgf");
            using (Writer)
            {
                Log.Info("Processing " + (lastScanNumber - firstScanNumber + 1) + " scans");

                var lastScanProgress = 0;
                for (var scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
                {
                    if (ParseInput.LogFormat == LogFormat.DEFAULT)
                    {
                        var scanProgress = (int) ((double) scanNumber / (lastScanNumber - firstScanNumber + 1) * 100);
                        if (scanProgress % ProgressPercentageStep == 0)
                        {
                            if (scanProgress != lastScanProgress)
                            {
                                Console.Write("" + scanProgress + "% ");
                                lastScanProgress = scanProgress;
                            }
                        }
                    }

                    // Get the scan from the RAW file
                    var scan = Scan.FromFile(rawFile, scanNumber);

                    // Get the retention time
                    var retentionTime = rawFile.RetentionTimeFromScanNumber(scanNumber);

                    // Get the scan filter for this scan number
                    var scanFilter = rawFile.GetFilterForScanNumber(scanNumber);

                    // Get the scan event for this scan number
                    var scanEvent = rawFile.GetScanEventForScanNumber(scanNumber);

                    // Construct the precursor reference string for the title 
                    var precursorReference = "";
                    if (ParseInput.MgfPrecursor)
                    {
                        if (scanFilter.MSOrder == MSOrderType.Ms)
                        {
                            // Keep track of the MS1 scan number for precursor reference
                            _precursorMs1ScanNumber = scanNumber;
                        }
                        else
                        {
                            precursorReference = ConstructPrecursorReference(scanFilter.MSOrder, scanNumber, scanEvent);
                        }
                    }

                    // Don't include MS1 spectra
                    if (ParseInput.MsLevel.Contains((int) scanFilter.MSOrder))
                    {
                        var reaction = GetReaction(scanEvent, scanNumber);

                        Writer.WriteLine("BEGIN IONS");
                        if (!ParseInput.MgfPrecursor)
                        {
                            Writer.WriteLine($"TITLE={ConstructSpectrumTitle((int) Device.MS, 1, scanNumber)}");
                        }
                        else
                        {
                            Writer.WriteLine(
                                $"TITLE={ConstructSpectrumTitle((int) Device.MS, 1, scanNumber)} [PRECURSOR={precursorReference}]");
                        }

                        Writer.WriteLine($"SCANS={scanNumber}");
                        Writer.WriteLine(
                            $"RTINSECONDS={(retentionTime * 60).ToString(CultureInfo.InvariantCulture)}");

                        // Trailer extra data list
                        var trailerData = new ScanTrailer(rawFile.GetTrailerExtraInformation(scanNumber));
                        int? charge = trailerData.AsPositiveInt("Charge State:");
                        double? monoisotopicMz = trailerData.AsDouble("Monoisotopic M/Z:");
                        double? isolationWidth =
                            trailerData.AsDouble("MS" + (int) scanFilter.MSOrder + " Isolation Width:");

                        if (reaction != null)
                        {
                            var selectedIonMz =
                                CalculateSelectedIonMz(reaction, monoisotopicMz, isolationWidth);

                            Writer.WriteLine("PEPMASS=" +
                                             selectedIonMz.ToString(CultureInfo.InvariantCulture));
                        }

                        // Charge
                        if (charge != null)
                        {
                            // Scan polarity            
                            var polarity = PositivePolarity;
                            if (scanFilter.Polarity == PolarityType.Negative)
                            {
                                polarity = NegativePolarity;
                            }

                            Writer.WriteLine($"CHARGE={charge}{polarity}");
                        }

                        // Write the filter string
                        //Writer.WriteLine($"SCANEVENT={scanEvent.ToString()}");

                        if (!ParseInput.NoPeakPicking.Contains((int)scanFilter.MSOrder))
                        {
                            // Check if the scan has a centroid stream
                            if (scan.HasCentroidStream)
                            {
                                if (scan.CentroidScan.Length > 0)
                                {
                                    for (var i = 0; i < scan.CentroidScan.Length; i++)
                                    {
                                        Writer.WriteLine(
                                            scan.CentroidScan.Masses[i].ToString("0.0000000",
                                                CultureInfo.InvariantCulture)
                                            + " "
                                            + scan.CentroidScan.Intensities[i].ToString("0.0000000000",
                                                CultureInfo.InvariantCulture));
                                    }
                                }
                            }
                            else // Otherwise take segmented (low res) scan data
                            {
                                // If the spectrum is profile perform centroiding
                                var segmentedScan = scanEvent.ScanData == ScanDataType.Profile
                                    ? Scan.ToCentroid(scan).SegmentedScan
                                    : scan.SegmentedScan;

                                for (var i = 0; i < segmentedScan.Positions.Length; i++)
                                {
                                    Writer.WriteLine(
                                        segmentedScan.Positions[i].ToString("0.0000000",
                                            CultureInfo.InvariantCulture)
                                        + " "
                                        + segmentedScan.Intensities[i].ToString("0.0000000000",
                                            CultureInfo.InvariantCulture));
                                }
                            }
                        }
                        else // Use the segmented data as is
                        {
                            for (var i = 0; i < scan.SegmentedScan.Positions.Length; i++)
                            {
                                Writer.WriteLine(
                                    scan.SegmentedScan.Positions[i].ToString("0.0000000",
                                        CultureInfo.InvariantCulture)
                                    + " "
                                    + scan.SegmentedScan.Intensities[i].ToString("0.0000000000",
                                        CultureInfo.InvariantCulture));
                            }
                        }

                        Writer.WriteLine("END IONS");

                        Log.Debug("Spectrum written to file -- SCAN " + scanNumber);
                    }
                }

                if (ParseInput.LogFormat == LogFormat.DEFAULT)
                {
                    Console.WriteLine();
                }
            }
        }

        private string ConstructPrecursorReference(MSOrderType msOrder, int scanNumber, IScanEvent scanEvent)
        {
            // Precursor reference
            var precursorReference = "";

            switch (msOrder)
            {
                case MSOrderType.Ms2:
                    // Keep track of the MS2 scan number and isolation m/z for precursor reference                   
                    var result = Regex.Match(scanEvent.ToString(), FilterStringIsolationMzPattern);
                    if (result.Success)
                    {
                        if (_isolationMzToPrecursorScanNumberMapping.ContainsKey(result.Groups[1].Value))
                        {
                            _isolationMzToPrecursorScanNumberMapping.Remove(result.Groups[1].Value);
                        }

                        _isolationMzToPrecursorScanNumberMapping.Add(result.Groups[1].Value, scanNumber);
                    }

                    precursorReference = ConstructSpectrumTitle((int) Device.MS, 1, _precursorMs1ScanNumber);
                    break;

                case MSOrderType.Ms3:
                    var precursorScanNumber = _isolationMzToPrecursorScanNumberMapping.Keys.FirstOrDefault(
                        isolationMz => scanEvent.ToString().Contains(isolationMz));
                    if (!precursorScanNumber.IsNullOrEmpty())
                    {
                        precursorReference = ConstructSpectrumTitle((int) Device.MS, 1,
                            _isolationMzToPrecursorScanNumberMapping[precursorScanNumber]);
                    }
                    else
                    {
                        throw new InvalidOperationException("Couldn't find a MS2 precursor scan for MS3 scan " +
                                                            scanEvent);
                    }

                    break;
            }

            return precursorReference;
        }
    }
}