using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using IO.MzML;
using mzIdentML120.Generated;
using MassSpectrometry;
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

        private static readonly Dictionary<IonizationModeType, string> IonizationTypeAccessions =
            new Dictionary<IonizationModeType, string>
            {
                {IonizationModeType.ElectroSpray, "MS:1000073"},
                {IonizationModeType.GlowDischarge, "MS:1000259"},
                {IonizationModeType.ChemicalIonization, "MS:1000071"},
                {IonizationModeType.AtmosphericPressureChemicalIonization, "MS:1000070"},
                {IonizationModeType.ChemicalIonization, "MS:1000071"},
                {IonizationModeType.MatrixAssistedLaserDesorptionIonization, "MS:1000239"},
            };

        private static readonly Dictionary<IonizationModeType, string> IonizationTypeNames =
            new Dictionary<IonizationModeType, string>
            {
                {IonizationModeType.ElectroSpray, "electrospray ionization"},
                {IonizationModeType.GlowDischarge,"glow discharge ionization"},
                {IonizationModeType.ChemicalIonization, "chemical ionization"},
                {IonizationModeType.AtmosphericPressureChemicalIonization, "atmospheric pressure chemical ionization"},
                {IonizationModeType.ChemicalIonization, "chemical ionization"},
                {IonizationModeType.MatrixAssistedLaserDesorptionIonization, "atmospheric pressure matrix-assisted laser desorption ionization"},
            };
        
        private static readonly Dictionary<ActivationType, string> DissociationTypeAccessions =
            new Dictionary<ActivationType, string>
            {
                {ActivationType.CollisionInducedDissociation, "MS:1000133"},
                {ActivationType.HigherEnergyCollisionalDissociation, "MS:1002481"},
                {ActivationType.ElectronTransferDissociation, "MS:1000598"},
                {ActivationType.MultiPhotonDissociation, "MS:1000435"},
            };

        private static readonly Dictionary<ActivationType, string> DissociationTypeNames =
            new Dictionary<ActivationType, string>
            {
                {ActivationType.CollisionInducedDissociation, "collision-induced dissociation"},
                {
                    ActivationType.HigherEnergyCollisionalDissociation,
                    "higher energy beam-type collision-induced dissociation"
                },
                {ActivationType.ElectronTransferDissociation, "electron transfer dissociation"},
                {ActivationType.MultiPhotonDissociation, "photodissociation"},
            };

        private static readonly Dictionary<string, string> InstrumentModelAccessions =
            new Dictionary<string, string>
            {
                {"LTQ FT", "MS:1000448"},
                {"LTQ Orbitrap Velos", "MS:1001742"},
                {"LTQ Orbitrap", "MS:1000449"},
                {"LTQ Velos", "MS:1000855"},
                {"Orbitrap Fusion", "MS:1002416"},
                {"Q Exactive", "MS:1001911"},
            };

        private static readonly Dictionary<string, string> InstrumentModelNames =
            new Dictionary<string, string>
            {
                {"LTQ FT", "LTQ FT"},
                {"LTQ Orbitrap Velos", "LTQ Orbitrap Velos"},
                {"LTQ Orbitrap", "LTQ Orbitrap"},
                {"LTQ Velos", "LTQ Velos"},
                {"Orbitrap Fusion", "Orbitrap Fusion"},
                {"Q Exactive", "Q Exactive"},
            };

        private IRawDataPlus rawFile;
        private Dictionary<MZAnalyzerType, string> analyzersInThisFileDict = new Dictionary<MZAnalyzerType, string>();

        public MzMLSpectrumWriter(ParseInput parseInput) : base(parseInput)
        {
        }

        public override void WriteSpectra(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            this.rawFile = rawFile;
            var mzMl = InitializeMz();

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

                if (scan.HasCentroidStream)
                {
                    var centroidStream = rawFile.GetCentroidStream(scanNumber, false);
                    if (scan.CentroidScan.Length > 0)
                    {
                        SpectrumType spectrum = new SpectrumType()
                        {
//                            binaryDataArrayList = new BinaryDataArrayListType()
//                            {
//                                binaryDataArray = 
//                            };
                        };
                    }
                }
                else
                {
                    // Get the scan statistics from the RAW file for this scan number
                    var scanStatistics = rawFile.GetScanStatsForScanNumber(scanNumber);

                    // Get the segmented (low res and profile) scan data
                    var segmentedScan = rawFile.GetSegmentedScanFromScanNumber(scanNumber, scanStatistics);
                }

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

                            if ((trailerData.Labels[i] == "Master Scan Number:") ||
                                (trailerData.Labels[i] == "Master Scan Number") ||
                                (trailerData.Labels[i] == "Master Index:"))
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

            mzMl.cvList.cv[2] = new CVType()
            {
                URI = @"http://purl.obolibrary.org/obo/ncit/releases/2018-06-08/ncit.owl",
                fullName = "NCI Thesaurus OBO Edition",
                id = "NCIT",
                version = "18.05d"
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

            mzMl.fileDescription.sourceFileList.sourceFile[0] = new SourceFileType
            {
                id = ParseInput.RawFileName,
                name = ParseInput.RawFileNameWithoutExtension,
                location = ParseInput.RawFilePath,
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
//            mzMl.fileDescription.sourceFileList.sourceFile[0].cvParam[2] = new CVParamType
//            {
//                accession = FileChecksumAccessions[myMsDataFile.SourceFile.FileChecksumType],
//                name = myMsDataFile.SourceFile.FileChecksumType,
//                cvRef = "MS",
//                value = myMsDataFile.SourceFile.CheckSum ?? "",
//            };                                                          

            mzMl.fileDescription.fileContent.cvParam = new CVParamType[2];
            // MS1
            mzMl.fileDescription.fileContent.cvParam[0] = new CVParamType
            {
                accession = "MS:1000579", // MS1 Data
                name = "MS1 spectrum",
                cvRef = "MS",
                value = ""
            };
            // MS2
            mzMl.fileDescription.fileContent.cvParam[1] = new CVParamType
            {
                accession = "MS:1000580", // MSn Data
                name = "MSn spectrum",
                cvRef = "MS",
                value = ""
            };

            mzMl.softwareList = new SoftwareListType
            {
                count = "2",
                software = new SoftwareType[2]
            };

            mzMl.softwareList.software[0] = new SoftwareType
            {
                id = "ThermoRawFileParser",
                version = "1",
                cvParam = new CVParamType[1]
            };

            mzMl.softwareList.software[0].cvParam[0] = new CVParamType
            {
                accession = "MS:1000799",
                value = "ThermoRawFileParser",
                name = "custom unreleased software tool",
                cvRef = "MS"
            };

            mzMl.run = new RunType()
            {
                spectrumList = new SpectrumListType()
            };

            return mzMl;
        }

        private void constructInstrumentConfigurationList()
        {
            var instrumentData = rawFile.GetInstrumentData();

            InstrumentConfigurationListType instrumentConfigurationList = new InstrumentConfigurationListType()
            {
                count = rawFile.InstrumentCount.ToString(),
                instrumentConfiguration = new InstrumentConfigurationType[rawFile.InstrumentCount]
            };
            for (int i = 0; i < rawFile.InstrumentCount; i++)
            {
                instrumentConfigurationList.instrumentConfiguration[i] = new InstrumentConfigurationType
                {
                    id = instrumentData.Name,
                    componentList = new ComponentListType(),
                    cvParam = new CVParamType[1]
                };

                string instrumentModelAccession;
                if (InstrumentModelNames.TryGetValue(instrumentData.Name, out instrumentModelAccession))
                {
                    instrumentModelAccession = "MS:1000483";
                }

                string instrumentModelName;
                if (InstrumentModelNames.TryGetValue(instrumentData.Name, out instrumentModelName))
                {
                    instrumentModelName = "Thermo Fisher Scientific instrument model";
                }

                instrumentConfigurationList.instrumentConfiguration[i].cvParam[0] = new CVParamType
                {
                    cvRef = "MS",
                    accession = instrumentModelAccession,
                    name = instrumentModelName,
                    value = ""
                };

                instrumentConfigurationList.instrumentConfiguration[i].cvParam[1] = new CVParamType
                {
                    cvRef = "MS",
                    accession = "MS:1000529",
                    name = "instrument serial number",
                    value = instrumentData.SerialNumber
                };

                instrumentConfigurationList.instrumentConfiguration[i].cvParam[2] = new CVParamType
                {
                    cvRef = "NCIT",
                    accession = "NCIT:C111093",
                    name = "Software Version",
                    value = instrumentData.SoftwareVersion
                };

                instrumentConfigurationList.instrumentConfiguration[i].componentList =
                    new ComponentListType
                    {
                        count = 3.ToString(),
                        source = new SourceComponentType[1],
                        analyzer = new AnalyzerComponentType[1],
                        detector = new DetectorComponentType[1],
                    };
                string ionizationModeAccession;
                if (InstrumentModelNames.TryGetValue(instrumentData.Name, out ionizationModeAccession))
                {
                    ionizationModeAccession = "MS:1000008";
                }
                string ionizationModeValue;
                if (InstrumentModelNames.TryGetValue(instrumentData.Name, out ionizationModeValue))
                {
                    ionizationModeValue = "ionization type";
                }                                
                instrumentConfigurationList.instrumentConfiguration[i].componentList.source[0] =
                    new SourceComponentType
                    {
                        order = 1,
                        cvParam = new CVParamType[1]
                    };
                instrumentConfigurationList.instrumentConfiguration[i].componentList.source[0].cvParam[0] =
                    new CVParamType
                    {
                        cvRef = "MS",
                        accession = ionizationModeAccession,
                        name = ionizationModeAccession,
                        value = ""
                    };

                instrumentConfigurationList.instrumentConfiguration[i].componentList.analyzer[0] =
                    new AnalyzerComponentType
                    {
                        order = 2,
                        cvParam = new CVParamType[1]
                    };
//                string anName = "";
//                if (analyzersInThisFile[i].ToString().ToLower() == "unknown")
//                {
//                    anName = "mass analyzer type";
//                }
//                else
//                    anName = analyzersInThisFile[i].ToString().ToLower();
//
//                instrumentConfigurationList.instrumentConfiguration[i].componentList.analyzer[0].cvParam[0] =
//                    new CVParamType
//                    {
//                        cvRef = "MS",
//                        accession = analyzerDictionary[analyzersInThisFile[i]],
//                        name = anName,
//                        value = ""
//                    };
//
//                instrumentConfigurationList.instrumentConfiguration[i].componentList.detector[0] =
//                    new DetectorComponentType
//                    {
//                        order = 3,
//                        cvParam = new CVParamType[1]
//                    };
//                instrumentConfigurationList.instrumentConfiguration[i].componentList.detector[0].cvParam[0] =
//                    new CVParamType
//                    {
//                        cvRef = "MS",
//                        accession = "MS:1000026",
//                        name = "detector type",
//                        value = ""
//                    };
            }
        }

        private SpectrumType constructSpectrum(IRawDataPlus rawFile, int scanNumber)
        {
            // Get each scan from the RAW file
            var scan = Scan.FromFile(rawFile, scanNumber);

            // Get the scan filter for this scan number
            var scanFilter = rawFile.GetFilterForScanNumber(scanNumber);

            // Get the scan event for this scan number
            var scanEvent = rawFile.GetScanEventForScanNumber(scanNumber);
            SpectrumType spectrum = new SpectrumType()
            {
                cvParam = new CVParamType[8]
            };

            // MS level
            spectrum.cvParam[1] = new CVParamType
            {
                name = "ms level",
                accession = "MS:1000511",
                value = ((int) scanFilter.MSOrder).ToString(CultureInfo.InvariantCulture),
                cvRef = "MS"
            };

            if (scanFilter.MSOrder == MSOrderType.Ms)
            {
                spectrum.cvParam[0] = new CVParamType
                {
                    accession = "MS:1000579",
                    cvRef = "MS",
                    name = "MS1 spectrum",
                    value = ""
                };
            }
            else if (scanFilter.MSOrder == MSOrderType.Ms2)
            {
                spectrum.cvParam[0] = new CVParamType
                {
                    accession = "MS:1000580",
                    cvRef = "MS",
                    name = "MSn spectrum",
                    value = ""
                };

                // Construct the precursor
                spectrum.precursorList = new PrecursorListType
                {
                    count = 1.ToString(),
                    precursor = new PrecursorType[1],
                };
                spectrum.precursorList.precursor[0] = new PrecursorType
                {
                    selectedIonList = new SelectedIonListType()
                    {
                        count = 1.ToString(),
                        selectedIon = new ParamGroupType[1]
                    }
                };

                spectrum.precursorList.precursor[0].spectrumRef = ConstructSpectrumTitle(scanNumber);

                spectrum.precursorList.precursor[0].selectedIonList.selectedIon[0] =
                    new ParamGroupType
                    {
                        cvParam = new CVParamType[3]
                    };

                IReaction reaction = null;
                double precursorMass = 0.0;
                double? isolationWidth = null;
                try
                {
                    reaction = scanEvent.GetReaction(0);
                    precursorMass = reaction.PrecursorMass;
                    isolationWidth = reaction.IsolationWidth;
                }
                catch (ArgumentOutOfRangeException exception)
                {
                    //do nothing
                }

                // Selected ion MZ
                spectrum.precursorList.precursor[0].selectedIonList.selectedIon[0]
                    .cvParam[0] = new CVParamType
                {
                    name = "selected ion m/z",
                    value = precursorMass.ToString(CultureInfo.InvariantCulture),
                    accession = "MS:1000744",
                    cvRef = "MS",
                    unitCvRef = "MS",
                    unitAccession = "MS:1000040",
                    unitName = "m/z"
                };

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

                spectrum.precursorList.precursor[0].selectedIonList.selectedIon[0]
                    .cvParam[1] = new CVParamType
                {
                    name = "charge state",
                    value = charge.ToString(),
                    accession = "MS:1000041",
                    cvRef = "MS",
                };

                // TODO find selected ion intensity
                // Selected ion intensity
//                spectrum.precursorList.precursor[0].selectedIonList.selectedIon[0]
//                    .cvParam[2] = new CVParamType
//                {
//                    name = "peak intensity",
//                    value = "?????",
//                    accession = "MS:1000042",
//                    cvRef = "MS"
//                };

                spectrum.precursorList.precursor[0].isolationWindow =
                    new ParamGroupType
                    {
                        cvParam = new CVParamType[3]
                    };
                spectrum.precursorList.precursor[0].isolationWindow.cvParam[0] =
                    new CVParamType
                    {
                        accession = "MS:1000827",
                        name = "isolation window target m/z",
                        value = precursorMass.ToString(CultureInfo.InvariantCulture),
                        cvRef = "MS",
                        unitCvRef = "MS",
                        unitAccession = "MS:1000040",
                        unitName = "m/z"
                    };
                spectrum.precursorList.precursor[0].isolationWindow.cvParam[1] =
                    new CVParamType
                    {
                        accession = "MS:1000828",
                        name = "isolation window lower offset",
                        value = scan.ScanStatistics.LowMass.ToString(CultureInfo.InvariantCulture),
                        cvRef = "MS",
                        unitCvRef = "MS",
                        unitAccession = "MS:1000040",
                        unitName = "m/z"
                    };
                spectrum.precursorList.precursor[0].isolationWindow.cvParam[2] =
                    new CVParamType
                    {
                        accession = "MS:1000829",
                        name = "isolation window upper offset",
                        value = scan.ScanStatistics.HighMass.ToString(CultureInfo.InvariantCulture),
                        cvRef = "MS",
                        unitCvRef = "MS",
                        unitAccession = "MS:1000040",
                        unitName = "m/z"
                    };

                spectrum.precursorList.precursor[0].activation =
                    new ParamGroupType
                    {
                        cvParam = new CVParamType[2]
                    };
                spectrum.precursorList.precursor[0].activation.cvParam[0] = new CVParamType()
                {
                    accession = "MS:1000045",
                    name = "collision energy",
                    cvRef = "MS",
                    value = "",
                };
                string activationAccession;
                if (DissociationTypeAccessions.TryGetValue(reaction.ActivationType, out activationAccession))
                {
                    activationAccession = "MS:1000044";
                }

                string activationName;
                if (DissociationTypeAccessions.TryGetValue(reaction.ActivationType, out activationName))
                {
                    activationAccession = "Activation Method";
                }

                spectrum.precursorList.precursor[0].activation.cvParam[1] = new CVParamType()
                {
                    accession = DissociationTypeAccessions[reaction.ActivationType],
                    name = DissociationTypeNames[reaction.ActivationType],
                    cvRef = "MS",
                    value = "",
                };
            }

            // Retention time            
            spectrum.cvParam[3] = new CVParamType
            {
                name = "scan start time",
                accession = "MS:1000016",
                value = rawFile.RetentionTimeFromScanNumber(scanNumber).ToString(CultureInfo.InvariantCulture),
                unitCvRef = "UO",
                unitAccession = "UO:0000031",
                unitName = "minute",
                cvRef = "MS",
            };

            // Lowest observed mz
            spectrum.cvParam[4] = new CVParamType
            {
                name = "lowest observed m/z",
                accession = "MS:1000528",
                value = scan.ScanStatistics.LowMass.ToString(CultureInfo.InvariantCulture),
                unitCvRef = "MS",
                unitAccession = "MS:1000040",
                unitName = "m/z",
                cvRef = "MS"
            };

            // Highest observed mz
            spectrum.cvParam[5] = new CVParamType
            {
                name = "highest observed m/z",
                accession = "MS:1000527",
                value = scan.ScanStatistics.HighMass.ToString(CultureInfo.InvariantCulture),
                unitAccession = "MS:1000040",
                unitName = "m/z",
                unitCvRef = "MS",
                cvRef = "MS"
            };

            // Total ion current
            spectrum.cvParam[6] = new CVParamType
            {
                name = "total ion current",
                accession = "MS:1000285",
                value = scan.ScanStatistics.TIC.ToString(CultureInfo.InvariantCulture),
                cvRef = "MS",
            };

            double? basePeakMass = null;
            double? basePeakIntensity = null;
            if (scan.HasCentroidStream)
            {
                var centroidStream = rawFile.GetCentroidStream(scanNumber, false);
                if (scan.CentroidScan.Length > 0)
                {
                    basePeakMass = centroidStream.BasePeakMass;
                    basePeakIntensity = centroidStream.BasePeakIntensity;
                }
            }
            else
            {
                // Get the scan statistics from the RAW file for this scan number
                var scanStatistics = rawFile.GetScanStatsForScanNumber(scanNumber);

                basePeakMass = scanStatistics.BasePeakMass;
                basePeakIntensity = scanStatistics.BasePeakIntensity;

                // Get the segmented (low res and profile) scan data
                var segmentedScan = rawFile.GetSegmentedScanFromScanNumber(scanNumber, scanStatistics);
            }

            // Base peak m/z
            if (basePeakMass != null)
            {
                spectrum.cvParam[7] = new CVParamType
                {
                    name = "base peak m/z",
                    accession = "MS:1000504",
                    value = basePeakMass.ToString(),
                    unitCvRef = "MS",
                    unitName = "m/z",
                    unitAccession = "MS:1000040",
                    cvRef = "MS"
                };
            }

            //base peak intensity
            if (basePeakMass != null)
            {
                spectrum.cvParam[8] = new CVParamType
                {
                    name = "base peak intensity",
                    accession = "MS:1000505",
                    value = basePeakIntensity.ToString(),
                    unitCvRef = "MS",
                    unitName = "number of detector counts",
                    unitAccession = "MS:1000131",
                    cvRef = "MS"
                };
            }

            // Scan list
            spectrum.scanList = new ScanListType
            {
                count = "1",
                scan = new ScanType[1],
                cvParam = new CVParamType[1]
            };

            spectrum.scanList.cvParam[0] = new CVParamType
            {
                accession = "MS:1000795",
                cvRef = "MS",
                name = "no combination",
                value = ""
            };


//            if (myMsDataFile.GetOneBasedScan(i).MzAnalyzer.Equals(analyzersInThisFile[0]))
//            {
//                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0] = new Generated.ScanType
//                {
//                    cvParam = new Generated.CVParamType[3]
//                };
//            }
//            else
//
//            {
//                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0] = new Generated.ScanType
//                {
//                    cvParam = new Generated.CVParamType[3],
//                    instrumentConfigurationRef = analyzersInThisFileDict[myMsDataFile.GetOneBasedScan(i).MzAnalyzer]
//                };
//            }
//
//            mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[0] = new Generated.CVParamType
//            {
//                name = "scan start time",
//                accession = "MS:1000016",
//                value = myMsDataFile.GetOneBasedScan(i).RetentionTime.ToString(CultureInfo.InvariantCulture),
//                unitCvRef = "UO",
//                unitAccession = "UO:0000031",
//                unitName = "minute",
//                cvRef = "MS",
//            };
//            mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[1] = new Generated.CVParamType
//            {
//                name = "filter string",
//                accession = "MS:1000512",
//                value = myMsDataFile.GetOneBasedScan(i).ScanFilter,
//                cvRef = "MS"
//            };
//            if (myMsDataFile.GetOneBasedScan(i).InjectionTime.HasValue)
//            {
//                mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].cvParam[2] = new Generated.CVParamType
//                {
//                    name = "ion injection time",
//                    accession = "MS:1000927",
//                    value = myMsDataFile.GetOneBasedScan(i).InjectionTime.Value.ToString(CultureInfo.InvariantCulture),
//                    cvRef = "MS",
//                    unitName = "millisecond",
//                    unitAccession = "UO:0000028",
//                    unitCvRef = "UO"
//                };
//            }
//
//            if (myMsDataFile.GetOneBasedScan(i) is IMsDataScanWithPrecursor<IMzSpectrum<IMzPeak>>)
//            {
//                var scanWithPrecursor =
//                    myMsDataFile.GetOneBasedScan(i) as IMsDataScanWithPrecursor<IMzSpectrum<IMzPeak>>;
//                if (scanWithPrecursor.SelectedIonMonoisotopicGuessMz.HasValue)
//                {
//                    mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].userParam = new Generated.UserParamType[1];
//                    mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].userParam[0] = new Generated.UserParamType
//                    {
//                        name = "[mzLib]Monoisotopic M/Z:",
//                        value = scanWithPrecursor.SelectedIonMonoisotopicGuessMz.Value.ToString(CultureInfo
//                            .InvariantCulture)
//                    };
//                }
//            }
//
//            mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList = new Generated.ScanWindowListType
//            {
//                count = 1,
//                scanWindow = new Generated.ParamGroupType[1]
//            };
//            mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0] =
//                new Generated.ParamGroupType
//                {
//                    cvParam = new Generated.CVParamType[2]
//                };
//            mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[0] =
//                new Generated.
//                    CVParamType
//                    {
//                        name = "scan window lower limit",
//                        accession = "MS:1000501",
//                        value = myMsDataFile.GetOneBasedScan(i).ScanWindowRange.Minimum
//                            .ToString(CultureInfo.InvariantCulture),
//                        cvRef = "MS",
//                        unitCvRef = "MS",
//                        unitAccession = "MS:1000040",
//                        unitName = "m/z"
//                    };
//            mzML.run.spectrumList.spectrum[i - 1].scanList.scan[0].scanWindowList.scanWindow[0].cvParam[1] =
//                new Generated.
//                    CVParamType
//                    {
//                        name = "scan window upper limit",
//                        accession = "MS:1000500",
//                        value = myMsDataFile.GetOneBasedScan(i).ScanWindowRange.Maximum
//                            .ToString(CultureInfo.InvariantCulture),
//                        cvRef = "MS",
//                        unitCvRef = "MS",
//                        unitAccession = "MS:1000040",
//                        unitName = "m/z"
//                    };
//            if (myMsDataFile.GetOneBasedScan(i).NoiseData == null)
//            {
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList = new Generated.BinaryDataArrayListType
//                {
//                    // ONLY WRITING M/Z AND INTENSITY DATA, NOT THE CHARGE! (but can add charge info later)
//                    // CHARGE (and other stuff) CAN BE IMPORTANT IN ML APPLICATIONS!!!!!
//                    count = 2.ToString(),
//                    binaryDataArray = new Generated.BinaryDataArrayType[2]
//                };
//            }
//
//            if (myMsDataFile.GetOneBasedScan(i).NoiseData != null)
//            {
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList = new Generated.BinaryDataArrayListType
//                {
//                    // ONLY WRITING M/Z AND INTENSITY DATA, NOT THE CHARGE! (but can add charge info later)
//                    // CHARGE (and other stuff) CAN BE IMPORTANT IN ML APPLICATIONS!!!!!
//                    count = 5.ToString(),
//                    binaryDataArray = new Generated.BinaryDataArrayType[5]
//                };
//            }
//
//            // M/Z Data
//            mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0] =
//                new Generated.BinaryDataArrayType
//                {
//                    binary = myMsDataFile.GetOneBasedScan(i).MassSpectrum.Get64BitXarray()
//                };
//            mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].encodedLength =
//                (4 * Math.Ceiling(((double) mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0]
//                                       .binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
//            mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam =
//                new Generated.CVParamType[3];
//            mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[0] =
//                new Generated.CVParamType
//                {
//                    accession = "MS:1000514",
//                    name = "m/z array",
//                    cvRef = "MS",
//                    unitName = "m/z",
//                    value = "",
//                    unitCvRef = "MS",
//                    unitAccession = "MS:1000040",
//                };
//            mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[1] =
//                new Generated.CVParamType
//                {
//                    accession = "MS:1000523",
//                    name = "64-bit float",
//                    cvRef = "MS",
//                    value = ""
//                };
//            mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[0].cvParam[2] =
//                new Generated.CVParamType
//                {
//                    accession = "MS:1000576",
//                    name = "no compression",
//                    cvRef = "MS",
//                    value = ""
//                };
//
//            // Intensity Data
//            mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1] =
//                new Generated.BinaryDataArrayType
//                {
//                    binary = myMsDataFile.GetOneBasedScan(i).MassSpectrum.Get64BitYarray()
//                };
//            mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].encodedLength =
//                (4 * Math.Ceiling(((double) mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1]
//                                       .binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
//            mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam =
//                new Generated.CVParamType[3];
//            mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[0] =
//                new Generated.CVParamType
//                {
//                    accession = "MS:1000515",
//                    name = "intensity array",
//                    cvRef = "MS",
//                    unitCvRef = "MS",
//                    unitAccession = "MS:1000131",
//                    unitName = "number of counts",
//                    value = ""
//                };
//            mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[1] =
//                new Generated.CVParamType
//                {
//                    accession = "MS:1000523",
//                    name = "64-bit float",
//                    cvRef = "MS",
//                    value = ""
//                };
//            mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[1].cvParam[2] =
//                new Generated.CVParamType
//                {
//                    accession = "MS:1000576",
//                    name = "no compression",
//                    cvRef = "MS",
//                    value = ""
//                };
//            if (myMsDataFile.GetOneBasedScan(i).NoiseData != null)
//            {
//                // mass
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2] =
//                    new Generated.BinaryDataArrayType
//                    {
//                        binary = myMsDataFile.GetOneBasedScan(i).Get64BitNoiseDataMass()
//                    };
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].arrayLength = (
//                        mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].binary.Length / 8)
//                    .ToString();
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].encodedLength =
//                    (4 * Math.Ceiling(((
//                                           double) mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList
//                                           .binaryDataArray[2].binary.Length / 3))).ToString(
//                        CultureInfo.InvariantCulture);
//
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam =
//                    new Generated.CVParamType[3]
//                    ;
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[0] =
//                    new Generated.CVParamType
//                    {
//                        accession = "MS:1000786",
//                        name = "non-standard data array",
//                        cvRef = "MS",
//                        value = "mass",
//                        unitCvRef = "MS",
//                    };
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[1] =
//                    new Generated.CVParamType
//                    {
//                        accession = "MS:1000523",
//                        name = "64-bit float",
//                        cvRef = "MS",
//                        value = ""
//                    };
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].cvParam[2] =
//                    new Generated.CVParamType
//                    {
//                        accession = "MS:1000576",
//                        name = "no compression",
//                        cvRef = "MS",
//                        value = ""
//                    };
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].userParam = new
//                    Generated.UserParamType[1];
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[2].userParam[0] = new
//                    Generated.UserParamType
//                    {
//                        name = "kelleherCustomType",
//                        value = "noise m/z",
//                    };
//
//                // noise
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3] =
//                    new Generated.BinaryDataArrayType
//                    {
//                        binary = myMsDataFile.GetOneBasedScan(i).Get64BitNoiseDataNoise()
//                    };
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].arrayLength = (
//                        mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].binary.Length / 8)
//                    .ToString();
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].encodedLength =
//                    (4 * Math.Ceiling(((
//                                           double) mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList
//                                           .binaryDataArray[3].binary.Length / 3))).ToString(
//                        CultureInfo.InvariantCulture);
//
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam =
//                    new Generated.CVParamType[3]
//                    ;
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[0] =
//                    new Generated.CVParamType
//                    {
//                        accession = "MS:1000786",
//                        name = "non-standard data array",
//                        cvRef = "MS",
//                        value = "SignalToNoise"
//                    };
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[1] =
//                    new Generated.CVParamType
//                    {
//                        accession = "MS:1000523",
//                        name = "64-bit float",
//                        cvRef = "MS",
//                        value = ""
//                    };
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].cvParam[2] =
//                    new Generated.CVParamType
//                    {
//                        accession = "MS:1000576",
//                        name = "no compression",
//                        cvRef = "MS",
//                        value = ""
//                    };
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].userParam = new
//                    Generated.UserParamType[1];
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[3].userParam[0] = new
//                    Generated.UserParamType
//                    {
//                        name = "kelleherCustomType",
//                        value = "noise baseline",
//                    };
//
//                // baseline
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4] =
//                    new Generated.BinaryDataArrayType
//                    {
//                        binary = myMsDataFile.GetOneBasedScan(i).Get64BitNoiseDataBaseline(),
//                    };
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].arrayLength = (
//                        mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].binary.Length / 8)
//                    .ToString();
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].encodedLength =
//                    (4 * Math.Ceiling(((
//                                           double) mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList
//                                           .binaryDataArray[4].binary.Length / 3))).ToString(
//                        CultureInfo.InvariantCulture);
//
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam =
//                    new Generated.CVParamType[3]
//                    ;
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[0] =
//                    new Generated.CVParamType
//                    {
//                        accession = "MS:1000786",
//                        name = "non-standard data array",
//                        cvRef = "MS",
//                        value = "baseline"
//                    };
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[1] =
//                    new Generated.CVParamType
//                    {
//                        accession = "MS:1000523",
//                        name = "64-bit float",
//                        cvRef = "MS",
//                        value = ""
//                    };
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].cvParam[2] =
//                    new Generated.CVParamType
//                    {
//                        accession = "MS:1000576",
//                        name = "no compression",
//                        cvRef = "MS",
//                        value = ""
//                    };
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].userParam = new
//                    Generated.UserParamType[1];
//                mzML.run.spectrumList.spectrum[i - 1].binaryDataArrayList.binaryDataArray[4].userParam[0] = new
//                    Generated.UserParamType
//                    {
//                        name = "kelleherCustomType",
//                        value = "noise intensity",
//                    };
//            }
            return null;
        }
    }
}