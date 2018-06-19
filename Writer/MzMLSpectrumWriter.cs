using System;
using mzIdentML120.Generated;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Writer.MzML;
using CVParamType = ThermoRawFileParser.Writer.MzML.CVParamType;
using SourceFileType = ThermoRawFileParser.Writer.MzML.SourceFileType;

namespace ThermoRawFileParser.Writer
{
    public class MzMLSpectrumWriter : SpectrumWriter
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MzMLSpectrumWriter(ParseInput parseInput) : base(parseInput)
        {
        }

        public override void WriteSpectra(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            for (int scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
            {
                InitializeMz();

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
//                        mgfFile.WriteLine("BEGIN IONS");
//                        mgfFile.WriteLine($"TITLE={ConstructSpectrumTitle(scanNumber)}");
//                        mgfFile.WriteLine($"SCAN={scanNumber}");
//                        mgfFile.WriteLine($"RTINSECONDS={time * 60}");
//                        // Get the reaction information for the first precursor
//                        var reaction = scanEvent.GetReaction(0);
//                        double precursorMass = reaction.PrecursorMass;
//                        mgfFile.WriteLine($"PEPMASS={precursorMass:F7}");
//                        //mgfFile.WriteLine($"PEPMASS={precursorMass:F2} {GetPrecursorIntensity(rawFile, scanNumber)}");

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

//                            // Get the segmented (low res and profile) scan data
//                            var segmentedScan = rawFile.GetSegmentedScanFromScanNumber(scanNumber, scanStatistics);
//                            mzmlMzSpectrum = new MzmlMzSpectrum(segmentedScan.Positions, segmentedScan.Intensities,
//                                false);
                        }
                    }
                }
            }
        }

        private mzMLType InitializeMz()
        {
            mzMLType mzMl = new mzMLType()
            {
                version = "1.1.0",
                cvList = new CVListType(),
                id = ParseInput.RawFileNameWithoutExtension,
            };

            mzMl.cvList = new CVListType()
            {
                count = "2",
                cv = new CVType[2]
            };

            mzMl.cvList.cv[0] = new CVType()
            {
                URI = @"https://raw.githubusercontent.com/HUPO-PSI/psi-ms-CV/master/psi-ms.obo",
                fullName = "Proteomics Standards Initiative Mass Spectrometry Ontology",
                id = "MS",
                version = "4.0.1"
            };

            mzMl.cvList.cv[1] = new CVType()
            {
                URI = @"http://obo.cvs.sourceforge.net/*checkout*/obo/obo/ontology/phenotype/unit.obo",
                fullName = "Unit Ontology",
                id = "UO",
                version = "12:10:2011"
            };
            
            mzMl.fileDescription = new FileDescriptionType()
            {
                fileContent = new ParamGroupType(),
                sourceFileList = new SourceFileListType()                
            };

            mzMl.fileDescription.sourceFileList = new SourceFileListType()
            {
                count = "1",
                sourceFile = new SourceFileType[1]
            };

            mzMl.fileDescription.sourceFileList.sourceFile[0].cvParam = new CVParamType[3];
            mzMl.fileDescription.sourceFileList.sourceFile[0].cvParam[0] = new CVParamType
            {
                accession = "MS:1000768",
                name = "Thermo nativeID format",
                cvRef = "MS",
                value = ""
            };
            mzMl.fileDescription.sourceFileList.sourceFile[0].cvParam[1] = new CVParamType
            {
                accession = "MS:1000568",
                name = "Thermo RAW format",
                cvRef = "MS",
                value = ""
            };
            mzMl.fileDescription.sourceFileList.sourceFile[0].cvParam[2] = new CVParamType
            {
                accession = FileChecksumAccessions[myMsDataFile.SourceFile.FileChecksumType],
                name = myMsDataFile.SourceFile.FileChecksumType,
                cvRef = "MS",
                value = myMsDataFile.SourceFile.CheckSum ?? "",
            };
            
            
            if (myMsDataFile.SourceFile.NativeIdFormat != null && myMsDataFile.SourceFile.MassSpectrometerFileFormat != null && myMsDataFile.SourceFile.FileChecksumType != null)
            {
                mzML.fileDescription.sourceFileList = new Generated.SourceFileListType()
                {
                    count = "1",
                    sourceFile = new Generated.SourceFileType[1]
                };

                string idName = char.IsNumber(myMsDataFile.SourceFile.FileName[0]) ?
                    "id:" + myMsDataFile.SourceFile.FileName[0] :
                    myMsDataFile.SourceFile.FileName;
                mzML.fileDescription.sourceFileList.sourceFile[0] = new Generated.SourceFileType
                {
                    id = idName,
                    name = myMsDataFile.SourceFile.FileName,
                    location = myMsDataFile.SourceFile.Uri.ToString(),
                };

                
                mzML.fileDescription.sourceFileList.sourceFile[0].cvParam[1] = new Generated.CVParamType
                {
                    accession = MassSpectrometerFileFormatAccessions[myMsDataFile.SourceFile.MassSpectrometerFileFormat],
                    name = myMsDataFile.SourceFile.MassSpectrometerFileFormat,
                    cvRef = "MS",
                    value = ""
                };
                mzML.fileDescription.sourceFileList.sourceFile[0].cvParam[2] = new Generated.CVParamType
                {
                    accession = FileChecksumAccessions[myMsDataFile.SourceFile.FileChecksumType],
                    name = myMsDataFile.SourceFile.FileChecksumType,
                    cvRef = "MS",
                    value = myMsDataFile.SourceFile.CheckSum ?? "",
                };
            }

            mzML.fileDescription.fileContent.cvParam = new Generated.CVParamType[2];
            mzML.fileDescription.fileContent.cvParam[0] = new Generated.CVParamType
            {
                accession = "MS:1000579", // MS1 Data
                name = "MS1 spectrum",
                cvRef = "MS",
                value = ""
            };
            mzML.fileDescription.fileContent.cvParam[1] = new Generated.CVParamType
            {
                accession = "MS:1000580", // MSn Data
                name = "MSn spectrum",
                cvRef = "MS",
                value = ""
            };

            mzML.softwareList = new Generated.SoftwareListType
            {
                count = "2",
                software = new Generated.SoftwareType[2]
            };

            // TODO: add the raw file fields
            mzML.softwareList.software[0] = new Generated.SoftwareType
            {
                id = "mzLib",
                version = "1",
                cvParam = new Generated.CVParamType[1]
            };

            mzML.softwareList.software[0].cvParam[0] = new Generated.CVParamType
            {
                accession = "MS:1000799",
                value = "mzLib",
                name = "custom unreleased software tool",
                cvRef = "MS"
            };

            return mzMl;
        }
    }
}