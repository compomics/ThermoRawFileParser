using System;
using System.Globalization;
using System.Reflection;
using log4net;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.Writer
{
    public class MgfSpectrumWriter : SpectrumWriter
    {
        private static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string PositivePolarity = "+";
        private const string NegativePolarity = "-";

        // Precursor scan number for reference in the precursor element of an MS2 spectrum

        public MgfSpectrumWriter(ParseInput parseInput) : base(parseInput)
        {
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

                    // Get each scan from the RAW file
                    var scan = Scan.FromFile(rawFile, scanNumber);

                    // Check to see if the RAW file contains label (high-res) data and if it is present
                    // then look for any data that is out of order
                    var time = rawFile.RetentionTimeFromScanNumber(scanNumber);

                    // Get the scan filter for this scan number
                    var scanFilter = rawFile.GetFilterForScanNumber(scanNumber);

                    // Get the scan event for this scan number
                    var scanEvent = rawFile.GetScanEventForScanNumber(scanNumber);

                    // don't include MS1 spectra
                    if (scanFilter.MSOrder != MSOrderType.Ms)
                    {
                        IReaction reaction = GetReaction(scanEvent, scanNumber);

                        Writer.WriteLine("BEGIN IONS");
                        Writer.WriteLine($"TITLE={ConstructSpectrumTitle(scanNumber)}");
                        Writer.WriteLine($"SCANS={scanNumber}");
                        Writer.WriteLine(
                            $"RTINSECONDS={(time * 60).ToString(CultureInfo.InvariantCulture)}");

                        // trailer extra data list
                        var trailerData = rawFile.GetTrailerExtraInformation(scanNumber);
                        int? charge = null;
                        double? monoisotopicMz = null;
                        double? isolationWidth = null;
                        for (var i = 0; i < trailerData.Length; i++)
                        {
                            if (trailerData.Labels[i] == "Charge State:")
                            {
                                if (Convert.ToInt32(trailerData.Values[i]) > 0)
                                {
                                    charge = Convert.ToInt32(trailerData.Values[i]);
                                }
                            }

                            if (trailerData.Labels[i] == "Monoisotopic M/Z:")
                            {
                                monoisotopicMz = double.Parse(trailerData.Values[i], NumberStyles.Any,
                                    CultureInfo.CurrentCulture);
                            }

                            if (trailerData.Labels[i] == "MS" + (int) scanFilter.MSOrder + " Isolation Width:")
                            {
                                isolationWidth = double.Parse(trailerData.Values[i], NumberStyles.Any,
                                    CultureInfo.CurrentCulture);
                            }
                        }

                        if (reaction != null)
                        {
                            var selectedIonMz =
                                CalculateSelectedIonMz(reaction, monoisotopicMz, isolationWidth);

                            Writer.WriteLine("PEPMASS=" +
                                             selectedIonMz.ToString(CultureInfo.InvariantCulture));
                        }

                        // charge
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

                        // write the filter string
                        //Writer.WriteLine($"SCANEVENT={scanEvent.ToString()}");

                        if (!ParseInput.NoPeakPicking)
                        {
                            // check if the scan has a centroid stream
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
                            else // otherwise take segmented (low res) scan data
                            {
                                // if the spectrum is profile perform centroiding
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
                        else // use the segmented data as is
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
    }
}