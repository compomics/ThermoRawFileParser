using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using IO.MzML;
using MassSpectrometry;
using MzLibUtil;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using Polarity = MassSpectrometry.Polarity;

namespace ThermoRawFileParser.Writer
{
    public class MzMlSpectrumWriter : SpectrumWriter
    {
        private static readonly Regex PolarityRegex = new Regex(@"\+ ", RegexOptions.Compiled);

        public MzMlSpectrumWriter(ParseInput parseInput) : base(parseInput)
        {
            //create temp file for loading the periodic table elements from the mzLib library
            String tempElements = Path.GetTempPath() + "elements.dat";
            UsefulProteomicsDatabases.Loaders.LoadElements(tempElements);
        }

        public override void WriteSpectra(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            List<IMzmlScan> scans = new List<IMzmlScan>();
            for (int scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
            {
                // Get each scan from the RAW file
                var scan = Scan.FromFile(rawFile, scanNumber);

                // Check to see if the RAW file contains label (high-res) data and if it is present
                // then look for any data that is out of order
                double retentionTime = rawFile.RetentionTimeFromScanNumber(scanNumber);

                // Get the scan filter for this scan number
                var scanFilter = rawFile.GetFilterForScanNumber(scanNumber);

                // Get the scan event for this scan number
                var scanEvent = rawFile.GetScanEventForScanNumber(scanNumber);
                
                // Get the ionizationMode, MS2 precursor mass, collision energy, and isolation width for each scan
                //if (scanFilter.MSOrder == ThermoFisher.CommonCore.Data.FilterEnums.MSOrderType.Ms2)
                //{
//                        mgfFile.WriteLine("BEGIN IONS");
//                        mgfFile.WriteLine($"TITLE={ConstructSpectrumTitle(scanNumber)}");
//                        mgfFile.WriteLine($"SCAN={scanNumber}");
//                        mgfFile.WriteLine($"RTINSECONDS={time * 60}");
                // Get the reaction information for the first precursor
                //var reaction = scanEvent.GetReaction(0);
                //double precursorMass = reaction.PrecursorMass;
                //mgfFile.WriteLine($"PEPMASS={precursorMass:F7}");

                // trailer extra data list
                var trailerData = rawFile.GetTrailerExtraInformation(scanNumber);
                int? charge = null;
                double? monoisotopicMass = null;
                double? ionInjectionTime = null;
                double? ms2IsolationWidth = null;
                int? masterScanIndex = null;
                for (int i = 0; i < trailerData.Length; i++)
                {
                    if ((trailerData.Labels[i] == "Charge State:"))
                    {
                        if (Convert.ToInt32(trailerData.Values[i]) > 0)
                        {
                            charge = Convert.ToInt32(trailerData.Values[i]);
                        }
                    }

                    if ((trailerData.Labels[i] == "Monoisotopic M/Z:"))
                    {
                        monoisotopicMass = double.Parse(trailerData.Values[i]);
                    }

                    if ((trailerData.Labels[i] == "Ion Injection Time (ms):"))
                    {
                        ionInjectionTime = double.Parse(trailerData.Values[i]);
                    }

                    if ((trailerData.Labels[i] == "MS2 Isolation Width:"))
                    {
                        ms2IsolationWidth = double.Parse(trailerData.Values[i]);
                    }

                    if ((trailerData.Labels[i] == "Master Index:"))
                    {
                        if (Convert.ToInt32(trailerData.Values[i]) > 0)
                        {
                            masterScanIndex = Convert.ToInt32(trailerData.Values[i]);
                        }
                    }
                }

                //$"PEPMASS={precursorMass:F2} {GetPrecursorIntensity(rawFile, scanNumber)}");
                //double collisionEnergy = reaction.CollisionEnergy;
                //mgfFile.WriteLine($"COLLISIONENERGY={collisionEnergy}");
                //var ionizationMode = scanFilter.IonizationMode;
                //mgfFile.WriteLine($"IONMODE={ionizationMode}");


                MzmlMzSpectrum mzmlMzSpectrum = null;
                if (scan.HasCentroidStream)
                {
                    var centroidStream = rawFile.GetCentroidStream(scanNumber, false);
                    if (scan.CentroidScan.Length > 0)
                    {
                        mzmlMzSpectrum = new MzmlMzSpectrum(centroidStream.Masses, centroidStream.Intensities,
                            false);
                    }
                }
                else
                {
                    // Get the scan statistics from the RAW file for this scan number
                    var scanStatistics = rawFile.GetScanStatsForScanNumber(scanNumber);

                    // Get the segmented (low res and profile) scan data
                    var segmentedScan = rawFile.GetSegmentedScanFromScanNumber(scanNumber, scanStatistics);
                    mzmlMzSpectrum = new MzmlMzSpectrum(segmentedScan.Positions, segmentedScan.Intensities,
                        false);
                }

                if (mzmlMzSpectrum != null)
                {
//                            scans.Add(new MzmlScan(scanNumber, mzmlMzSpectrum, 2, true,
//                                (MassSpectrometry.Polarity) Polarity.Positive,
//                                time, new MzRange(scan.ScanStatistics.LowMass, scan.ScanStatistics.HighMass), scanEvent.ToString(),
//                                MZAnalyzerType.Orbitrap, mzmlMzSpectrum.SumOfAllY, null, ConstructSpectrumTitle(scanNumber)));
                    DissociationType dissociationType = DissociationType.AnyActivationType;
                    Enum.TryParse(scanFilter.IonizationMode.ToString(), out dissociationType);
                    double precursorMass = 0.0;
                    double? isolationWidth = null;
                    try
                    {
                        var reaction = scanEvent.GetReaction(0);
                        precursorMass = reaction.PrecursorMass;
                        isolationWidth = reaction.IsolationWidth;
                    }
                    catch (ArgumentOutOfRangeException exception)
                    {
                        //do nothing
                    }


//                    if (scanFilter.MSOrder == MSOrderType.Ms)
//                    {
//                        precursorScanNumber = scanNumber;
//                    }
//                    else
//                    {
                    //Console.WriteLine(scanNumber + "----" + GetPrecursorIntensity(rawFile, precursorScanNumber, scanNumber));
                    // Define the settings for getting the Base Peak chromatogram            
//                            ChromatogramTraceSettings settings = new ChromatogramTraceSettings(TraceType.TIC);
//                            ChromatogramTraceSettings settings = new ChromatogramTraceSettings(TraceType.BasePeak);

//                            Range range = new Range(scan.ScanStatistics.LowMass, scan.ScanStatistics.HighMass);
//                            Range range = new Range((double) (precursorMass - isolationWidth), (double)(precursorMass + isolationWidth));

//                            settings.MassRanges = new Range[]{range};

                    // Get the chromatogram from the RAW file. 
//                            var data = rawFile.GetChromatogramData(new IChromatogramSettings[] {settings}, scanNumber,
//                                scanNumber);

                    // Split the data into the chromatograms
//                            var trace = ChromatogramSignal.FromChromatogramData(data);
//                            Console.WriteLine(scanNumber + "--------------------------" + (trace[0].Intensities[0]));

                    //Console.WriteLine(scanNumber + "------------" + GetPrecursorIntensity(rawFile, precursorScanNumber, scanNumber));
//                    }

                    PolarityType polarityType = scanFilter.Polarity;
                    Polarity polarity;
                    switch (polarityType)
                    {
                        case PolarityType.Positive:
                            polarity = Polarity.Positive;
                            break;
                        case PolarityType.Negative:
                            polarity = Polarity.Negative;
                            break;
                        default:
                            polarity = Polarity.Unknown;
                            break;
                    }

                    MZAnalyzerType mzAnalyzerType;
                    switch (scanEvent.MassAnalyzer)
                    {
                        case MassAnalyzerType.MassAnalyzerFTMS:
                            mzAnalyzerType = MZAnalyzerType.Orbitrap;
                            break;

                        default:
                            mzAnalyzerType = MZAnalyzerType.Unknown;
                            break;
                    }

                    IMzmlScan mzmlScan = null;
                    switch (scanFilter.MSOrder)
                    {
                        case MSOrderType.Ms:
                            mzmlScan = new MzmlScan(
                                scanNumber,
                                mzmlMzSpectrum,
                                (int) scanFilter.MSOrder,
                                scan.HasCentroidStream,
                                polarity,
                                retentionTime,
                                new MzRange(scan.ScanStatistics.LowMass, scan.ScanStatistics.HighMass),
                                scanEvent.ToString(),
                                mzAnalyzerType,
                                scan.ScanStatistics.TIC,
                                ionInjectionTime, //injection time
                                nativeId: ConstructSpectrumTitle(scanNumber)
                            );
                            break;
                        case MSOrderType.Ms2:
                            var scandnd = scan.ScanStatistics;
                            mzmlScan = new MzmlScanWithPrecursor(
                                scanNumber,
                                mzmlMzSpectrum,
                                (int) scanFilter.MSOrder,
                                scan.HasCentroidStream,
                                polarity,
                                retentionTime,
                                new MzRange(scan.ScanStatistics.LowMass, scan.ScanStatistics.HighMass),
                                scanEvent.ToString(),
                                mzAnalyzerType,
                                scan.ScanStatistics.TIC,
                                precursorMass, //selected ion mz
                                charge, //selected ion charge state guess
                                null, //selected ion intensity
                                precursorMass, //isolation mz
                                ms2IsolationWidth, //isolation width
                                dissociationType,
                                masterScanIndex, //precursor scan number
                                monoisotopicMass, //monoisotopic mass
                                ionInjectionTime, //injection time
                                nativeId: ConstructSpectrumTitle(scanNumber)
                            );
                            break;
                        default:
                            break;
                    }

                    if (mzmlScan != null)
                    {
                        scans.Add(mzmlScan);
                    }
                }
            }

            FakeMsDataFile f = new FakeMsDataFile(ParseInput.RawFilePath, ParseInput.RawFileNameWithoutExtension,
                scans.ToArray());
            try
            {
                MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(f,
                    ParseInput.OutputDirectory + "/" + ParseInput.RawFileNameWithoutExtension + ".mzml", false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            //var mzML = new global::ThermoRawFileParser.mzMLType();
        }
    }
}