using System;
using System.Collections.Generic;
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
        private readonly Regex _filterStringIsolationMzPattern = new Regex(@"ms\d+ (.+?) \[");

        // Precursor scan number for MSn scans
        private int _precursorScanNumber;

        // Precursor scan number (value) and isolation m/z (key) for reference in the precursor element of an MSn spectrum
        private readonly Dictionary<string, int> _precursorScanNumbers = new Dictionary<string, int>();

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
                if (rawFile.SelectMsData())
                {
                    Log.Info("Processing " + (lastScanNumber - firstScanNumber + 1) + " scans");

                    var lastScanProgress = 0;
                    for (var scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
                    {
                        if (ParseInput.LogFormat == LogFormat.DEFAULT)
                        {
                            var scanProgress = (int)((double)scanNumber / (lastScanNumber - firstScanNumber + 1) * 100);
                            if (scanProgress % ProgressPercentageStep == 0)
                            {
                                if (scanProgress != lastScanProgress)
                                {
                                    Console.Write("" + scanProgress + "% ");
                                    lastScanProgress = scanProgress;
                                }
                            }
                        }

                        _precursorScanNumber = 0;

                        // Get the scan from the RAW file
                        var scan = Scan.FromFile(rawFile, scanNumber);

                        // Get the retention time
                        var retentionTime = rawFile.RetentionTimeFromScanNumber(scanNumber);

                        // Get the scan filter for this scan number
                        var scanFilter = rawFile.GetFilterForScanNumber(scanNumber);

                        // Get the scan event for this scan number
                        var scanEvent = rawFile.GetScanEventForScanNumber(scanNumber);

                        // Trailer extra data list
                        ScanTrailer trailerData;

                        try
                        {
                            trailerData = new ScanTrailer(rawFile.GetTrailerExtraInformation(scanNumber));
                        }
                        catch (Exception ex)
                        {
                            Log.WarnFormat("Cannot load trailer infromation for scan {0} due to following exception\n{1}", scanNumber, ex.Message);
                            ParseInput.NewWarn();
                            trailerData = new ScanTrailer();
                        }

                        // Get scan ms level
                        var msLevel = (int)scanFilter.MSOrder;

                        // Construct the precursor reference string for the title 
                        var precursorReference = "";

                        if (ParseInput.MgfPrecursor)
                        {
                            if (msLevel == 1)
                            {
                                // Keep track of the MS1 scan number for precursor reference
                                _precursorScanNumbers[""] = scanNumber;
                            }
                            else
                            {
                                // Keep track of scan number and isolation m/z for precursor reference                   
                                var result = _filterStringIsolationMzPattern.Match(scanEvent.ToString());
                                if (result.Success)
                                {
                                    if (_precursorScanNumbers.ContainsKey(result.Groups[1].Value))
                                    {
                                        _precursorScanNumbers.Remove(result.Groups[1].Value);
                                    }

                                    _precursorScanNumbers.Add(result.Groups[1].Value, scanNumber);
                                }

                                //update precursor scan if it is provided in trailer data
                                var trailerMasterScan = trailerData.AsPositiveInt("Master Scan Number:");
                                if (trailerMasterScan.HasValue)
                                {
                                    _precursorScanNumber = trailerMasterScan.Value;
                                }
                                else //try getting it from the scan filter
                                {
                                    var parts = Regex.Split(result.Groups[1].Value, " ");

                                    //find the position of the first (from the end) precursor with a different mass 
                                    //to account for possible supplementary activations written in the filter
                                    var lastIonMass = parts.Last().Split('@').First();
                                    int last = parts.Length;
                                    while (last > 0 &&
                                           parts[last - 1].Split('@').First() == lastIonMass)
                                    {
                                        last--;
                                    }

                                    string parentFilter = String.Join(" ", parts.Take(last));
                                    if (_precursorScanNumbers.ContainsKey(parentFilter))
                                    {
                                        _precursorScanNumber = _precursorScanNumbers[parentFilter];
                                    }
                                }

                                if (_precursorScanNumber > 0)
                                {
                                    precursorReference = ConstructSpectrumTitle((int)Device.MS, 1, _precursorScanNumber);
                                }
                                else
                                {
                                    Log.Error($"Failed finding precursor for {scanNumber}");
                                    ParseInput.NewError();
                                }
                            }
                        }

                        if (ParseInput.MsLevel.Contains(msLevel))
                        {
                            var reaction = GetReaction(scanEvent, scanNumber);

                            Writer.WriteLine("BEGIN IONS");
                            if (!ParseInput.MgfPrecursor)
                            {
                                Writer.WriteLine($"TITLE={ConstructSpectrumTitle((int)Device.MS, 1, scanNumber)}");
                            }
                            else
                            {
                                Writer.WriteLine(
                                    $"TITLE={ConstructSpectrumTitle((int)Device.MS, 1, scanNumber)} [PRECURSOR={precursorReference}]");
                            }

                            Writer.WriteLine($"SCANS={scanNumber}");
                            Writer.WriteLine(
                                $"RTINSECONDS={(retentionTime * 60).ToString(CultureInfo.InvariantCulture)}");

                            int? charge = trailerData.AsPositiveInt("Charge State:");
                            double? monoisotopicMz = trailerData.AsDouble("Monoisotopic M/Z:");
                            double? isolationWidth =
                                trailerData.AsDouble("MS" + msLevel + " Isolation Width:");

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

                            double[] masses;
                            double[] intensities;

                            if (!ParseInput.NoPeakPicking.Contains(msLevel))
                            {
                                // Check if the scan has a centroid stream
                                if (scan.HasCentroidStream)
                                {
                                    masses = scan.CentroidScan.Masses;
                                    intensities = scan.CentroidScan.Intensities;
                                }
                                else // Otherwise take segmented (low res) scan data
                                {
                                    // If the spectrum is profile perform centroiding
                                    var segmentedScan = scanEvent.ScanData == ScanDataType.Profile
                                        ? Scan.ToCentroid(scan).SegmentedScan
                                        : scan.SegmentedScan;

                                    masses = segmentedScan.Positions;
                                    intensities = segmentedScan.Intensities;
                                }
                            }
                            else // Use the segmented data as is
                            {
                                masses = scan.SegmentedScan.Positions;
                                intensities = scan.SegmentedScan.Intensities;
                            }

                            if (!(masses is null) && masses.Length > 0)
                            {
                                Array.Sort(masses, intensities);

                                for (var i = 0; i < masses.Length; i++)
                                {
                                    Writer.WriteLine(String.Format("{0:f5} {1:f3}", masses[i], intensities[i]));
                                }
                            }

                            Writer.WriteLine("END IONS");

                            Log.Debug("Spectrum written to file -- SCAN# " + scanNumber);
                        }
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