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
        private int _precursorScanNumber;

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

                    IReaction reaction = null;
                    switch (scanFilter.MSOrder)
                    {
                        case MSOrderType.Ms:
                            // Keep track of scan number for precursor reference
                            _precursorScanNumber = scanNumber;
                            break;
                        case MSOrderType.Ms2:
                            try
                            {
                                reaction = scanEvent.GetReaction(0);
                            }
                            catch (ArgumentOutOfRangeException exception)
                            {
                                Log.Warn("No reaction found for scan " + scanNumber);
                            }

                            goto default;
                        case MSOrderType.Ms3:
                        {
                            try
                            {
                                reaction = scanEvent.GetReaction(1);
                            }
                            catch (ArgumentOutOfRangeException exception)
                            {
                                Log.Warn("No reaction found for scan " + scanNumber);
                            }

                            goto default;
                        }
                        default:
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

                                if (!ParseInput.PrecursorIntensity)
                                {
                                    Writer.WriteLine("PEPMASS=" +
                                                     selectedIonMz.ToString(CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    var precursorPeakIntensity = CalculatePrecursorPeakIntensity(rawFile,
                                        _precursorScanNumber, reaction.PrecursorMass);
                                    if (precursorPeakIntensity != null)
                                    {
                                        Writer.WriteLine("PEPMASS=" +
                                                         selectedIonMz.ToString(CultureInfo.InvariantCulture) +
                                                         " " +
                                                         precursorPeakIntensity.Value.ToString(CultureInfo
                                                             .InvariantCulture));
                                    }
                                    else
                                    {
                                        Writer.WriteLine("PEPMASS=" +
                                                         selectedIonMz.ToString(CultureInfo.InvariantCulture));
                                    }
                                }
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

                            // Check if the scan has a centroid stream
                            if (scan.HasCentroidStream && (scanEvent.ScanData == ScanDataType.Centroid ||
                                                           (scanEvent.ScanData == ScanDataType.Profile &&
                                                            !ParseInput.NoPeakPicking)))
                            {
                                var centroidStream = rawFile.GetCentroidStream(scanNumber, false);
                                if (scan.CentroidScan.Length > 0)
                                {
                                    for (var i = 0; i < centroidStream.Length; i++)
                                    {
                                        Writer.WriteLine(
                                            centroidStream.Masses[i].ToString("0.0000000",
                                                CultureInfo.InvariantCulture)
                                            + " "
                                            + centroidStream.Intensities[i].ToString("0.0000000000",
                                                CultureInfo.InvariantCulture));
                                    }
                                }
                            }
                            // Otherwise take the profile data
                            else
                            {
                                // Get the scan statistics from the RAW file for this scan number
                                var scanStatistics = rawFile.GetScanStatsForScanNumber(scanNumber);

                                // Get the segmented (low res and profile) scan data
                                var segmentedScan =
                                    rawFile.GetSegmentedScanFromScanNumber(scanNumber, scanStatistics);
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

                            Writer.WriteLine("END IONS");

                            Log.Debug("Spectrum written to file -- SCAN " + scanNumber);

                            break;
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