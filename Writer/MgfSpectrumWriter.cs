using System;
using System.IO;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.Writer
{
    internal class MgfSpectrumWriter : SpectrumWriter
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public MgfSpectrumWriter(ParseInput parseInput) : base(parseInput)
        {
        }

        /// <inheritdoc />       
        public override void WriteSpectra(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            using (var mgfFile =
                File.CreateText(ParseInput.OutputDirectory + "//" + ParseInput.RawFileNameWithoutExtension + ".mgf"))
            {
                for (int scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
                {
                    // Get each scan from the RAW file
                    var scan = Scan.FromFile(rawFile, scanNumber);

                    // Check to see if the RAW file contains label (high-res) data and if it is present
                    // then look for any data that is out of order
                    double time = rawFile.RetentionTimeFromScanNumber(scanNumber);

                    // Get the scan filter for this scan number
                    var scanFilter = rawFile.GetFilterForScanNumber(scanNumber);

                    // Get the scan event for this scan number
                    var scanEvent = rawFile.GetScanEventForScanNumber(scanNumber);

                    // Get the ionizationMode, MS2 precursor mass, collision energy, and isolation width for each scan
                    if (scanFilter.MSOrder == ThermoFisher.CommonCore.Data.FilterEnums.MSOrderType.Ms2)
                    {
                        if (scanEvent.ScanData == ScanDataType.Centroid ||
                            (scanEvent.ScanData == ScanDataType.Profile && ParseInput.IncludeProfileData))
                        {
                            mgfFile.WriteLine("BEGIN IONS");
                            mgfFile.WriteLine($"TITLE={ConstructSpectrumTitle(scanNumber)}");
                            mgfFile.WriteLine($"SCAN={scanNumber}");
                            mgfFile.WriteLine($"RTINSECONDS={time * 60}");
                            // Get the reaction information for the first precursor
                            var reaction = scanEvent.GetReaction(0);
                            double precursorMass = reaction.PrecursorMass;
                            mgfFile.WriteLine($"PEPMASS={precursorMass:F7}");
                            //mgfFile.WriteLine($"PEPMASS={precursorMass:F2} {GetPrecursorIntensity(rawFile, scanNumber)}");

                            // trailer extra data list
                            var trailerData = rawFile.GetTrailerExtraInformation(scanNumber);

                            for (int i = 0; i < trailerData.Length; i++)
                            {
                                if ((trailerData.Labels[i] == "Charge State:"))
                                {
                                    if (Convert.ToInt32(trailerData.Values[i]) > 0)
                                    {
                                        mgfFile.WriteLine($"CHARGE={trailerData.Values[i]}+");
                                    }
                                }
                            }

                            //double collisionEnergy = reaction.CollisionEnergy;
                            //mgfFile.WriteLine($"COLLISIONENERGY={collisionEnergy}");
                            //var ionizationMode = scanFilter.IonizationMode;
                            //mgfFile.WriteLine($"IONMODE={ionizationMode}");

                            if (scan.HasCentroidStream)
                            {
                                var centroidStream = rawFile.GetCentroidStream(scanNumber, false);
                                if (scan.CentroidScan.Length > 0)
                                {
                                    for (int i = 0; i < centroidStream.Length; i++)
                                    {
                                        mgfFile.WriteLine(
                                            $"{centroidStream.Masses[i]:F7} {centroidStream.Intensities[i]:F10}");
                                    }
                                }
                            }
                            else
                            {
                                // Get the scan statistics from the RAW file for this scan number
                                var scanStatistics = rawFile.GetScanStatsForScanNumber(scanNumber);

                                // Get the segmented (low res and profile) scan data
                                var segmentedScan = rawFile.GetSegmentedScanFromScanNumber(scanNumber, scanStatistics);
                                for (int i = 0; i < segmentedScan.Positions.Length; i++)
                                {
                                    mgfFile.WriteLine(
                                        $"{segmentedScan.Positions[i]:F7} {segmentedScan.Intensities[i]:F10}");
                                }
                            }

                            mgfFile.WriteLine("END IONS");
                        }
                    }
                }
            }
        }
    }
}