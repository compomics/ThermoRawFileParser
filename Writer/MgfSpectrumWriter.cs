using System;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.Writer
{
    public class MgfSpectrumWriter : SpectrumWriter
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MgfSpectrumWriter(ParseInput parseInput) : base(parseInput)
        {
        }

        /// <inheritdoc />       
        public override void Write(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            ConfigureWriter(".mgf");
            using (Writer)
            {
                for (var scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
                {
                    // Get each scan from the RAW file
                    var scan = Scan.FromFile(rawFile, scanNumber);

                    // Check to see if the RAW file contains label (high-res) data and if it is present
                    // then look for any data that is out of order
                    var time = rawFile.RetentionTimeFromScanNumber(scanNumber);

                    // Get the scan filter for this scan number
                    var scanFilter = rawFile.GetFilterForScanNumber(scanNumber);

                    // Get the scan event for this scan number
                    var scanEvent = rawFile.GetScanEventForScanNumber(scanNumber);

                    // Only consider MS2 spectra
                    if (scanFilter.MSOrder == MSOrderType.Ms2)
                    {
                        if (scanEvent.ScanData == ScanDataType.Centroid ||
                            (scanEvent.ScanData == ScanDataType.Profile && (scan.HasCentroidStream || !ParseInput.ExcludeProfileData)))
                        {
                            Writer.WriteLine("BEGIN IONS");
                            Writer.WriteLine($"TITLE={ConstructSpectrumTitle(scanNumber)}");
                            Writer.WriteLine($"SCAN={scanNumber}");
                            Writer.WriteLine($"RTINSECONDS={time * 60}");
                            // Get the reaction information for the first precursor
                            try
                            {
                                var reaction = scanEvent.GetReaction(0);
                                var precursorMass = reaction.PrecursorMass;
                                Writer.WriteLine($"PEPMASS={precursorMass:F7}");
                            }
                            catch (ArgumentOutOfRangeException exception)
                            {
                                Log.Warn("No reaction found for scan " + scanNumber);
                            }

                            // trailer extra data list
                            var trailerData = rawFile.GetTrailerExtraInformation(scanNumber);
                            for (var i = 0; i < trailerData.Length; i++)
                            {
                                if (trailerData.Labels[i] == "Charge State:")
                                {
                                    if (Convert.ToInt32(trailerData.Values[i]) > 0)
                                    {
                                        Writer.WriteLine($"CHARGE={trailerData.Values[i]}+");
                                    }
                                }
                            }

                            // Check if the scan has a centroid stream
                            if (scan.HasCentroidStream)
                            {
                                var centroidStream = rawFile.GetCentroidStream(scanNumber, false);
                                if (scan.CentroidScan.Length > 0)
                                {
                                    for (var i = 0; i < centroidStream.Length; i++)
                                    {
                                        Writer.WriteLine(
                                            $"{centroidStream.Masses[i]:F7} {centroidStream.Intensities[i]:F10}");
                                    }
                                }
                            }
                            // Otherwise take the profile data
                            else
                            {
                                // Get the scan statistics from the RAW file for this scan number
                                var scanStatistics = rawFile.GetScanStatsForScanNumber(scanNumber);

                                // Get the segmented (low res and profile) scan data
                                var segmentedScan = rawFile.GetSegmentedScanFromScanNumber(scanNumber, scanStatistics);
                                for (var i = 0; i < segmentedScan.Positions.Length; i++)
                                {
                                    Writer.WriteLine(
                                        $"{segmentedScan.Positions[i]:F7} {segmentedScan.Intensities[i]:F10}");
                                }
                            }

                            Writer.WriteLine("END IONS");
                        }
                    }
                }
            }
        }
    }
}