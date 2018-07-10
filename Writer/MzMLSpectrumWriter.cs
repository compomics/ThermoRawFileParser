using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Serialization;
using IO.MzML;
using mzIdentML120.Generated;
using MassSpectrometry;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Writer.MzML;
using CVParamType = ThermoRawFileParser.Writer.MzML.CVParamType;
using SourceFileType = ThermoRawFileParser.Writer.MzML.SourceFileType;
using UserParamType = ThermoRawFileParser.Writer.MzML.UserParamType;

namespace ThermoRawFileParser.Writer
{
    public class MzMLSpectrumWriter : SpectrumWriter
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Dictionary<MassAnalyzerType, CVParamType> MassAnalyzerTypes =
            new Dictionary<MassAnalyzerType, CVParamType>
            {
                {
                    MassAnalyzerType.MassAnalyzerFTMS, new CVParamType
                    {
                        accession = "MS:1000079",
                        name = "fourier transform ion cyclotron resonance mass spectrometer",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    MassAnalyzerType.MassAnalyzerITMS, new CVParamType
                    {
                        accession = "MS:1000264",
                        name = "ion trap",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    MassAnalyzerType.MassAnalyzerSector, new CVParamType
                    {
                        accession = "MS:1000080",
                        name = "magnetic sector",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    MassAnalyzerType.MassAnalyzerTOFMS, new CVParamType
                    {
                        accession = "MS:1000084",
                        name = "time-of-flight",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    MassAnalyzerType.MassAnalyzerTQMS, new CVParamType
                    {
                        accession = "MS:1000081",
                        name = "quadrupole",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    MassAnalyzerType.MassAnalyzerSQMS, new CVParamType
                    {
                        accession = "MS:1000081",
                        name = "quadrupole",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    MassAnalyzerType.Any, new CVParamType
                    {
                        accession = "MS:1000443",
                        name = "mass analyzer type",
                        cvRef = "MS",
                        value = "",
                    }
                },
            };

        private static readonly Dictionary<IonizationModeType, CVParamType> IonizationTypes =
            new Dictionary<IonizationModeType, CVParamType>
            {
                {
                    IonizationModeType.ElectroSpray, new CVParamType
                    {
                        accession = "MS:1000073",
                        name = "electrospray ionization",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    IonizationModeType.GlowDischarge, new CVParamType
                    {
                        accession = "MS:1000259",
                        name = "glow discharge ionization",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    IonizationModeType.ChemicalIonization, new CVParamType
                    {
                        accession = "MS:1000071",
                        name = "chemical ionization",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    IonizationModeType.AtmosphericPressureChemicalIonization, new CVParamType
                    {
                        accession = "MS:1000070",
                        name = "atmospheric pressure chemical ionization",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    IonizationModeType.MatrixAssistedLaserDesorptionIonization, new CVParamType
                    {
                        accession = "MS:1000239",
                        name = "atmospheric pressure matrix-assisted laser desorption ionization",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    IonizationModeType.NanoSpray, new CVParamType
                    {
                        accession = "MS:1000398",
                        name = "nanoelectrospray",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    IonizationModeType.Any, new CVParamType
                    {
                        accession = "MS:1000008",
                        name = "ionization type",
                        cvRef = "MS",
                        value = "",
                    }
                }
            };


        private static readonly Dictionary<ActivationType, CVParamType> DissociationTypes =
            new Dictionary<ActivationType, CVParamType>
            {
                {
                    ActivationType.CollisionInducedDissociation, new CVParamType
                    {
                        accession = "MS:1000133",
                        name = "collision-induced dissociation",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    ActivationType.HigherEnergyCollisionalDissociation, new CVParamType
                    {
                        accession = "MS:1002481",
                        name = "higher energy beam-type collision-induced dissociation",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    ActivationType.ElectronTransferDissociation, new CVParamType
                    {
                        accession = "MS:1000598",
                        name = "electron transfer dissociation",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    ActivationType.MultiPhotonDissociation, new CVParamType
                    {
                        accession = "MS:1000435",
                        name = "photodissociation",
                        cvRef = "MS",
                        value = "",
                    }
                },
            };

        private static readonly Dictionary<string, CVParamType> InstrumentModels =
            new Dictionary<string, CVParamType>
            {
                {
                    "LTQ FT", new CVParamType
                    {
                        accession = "MS:1000448",
                        name = "LTQ FT",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    "LTQ Orbitrap Velos", new CVParamType
                    {
                        accession = "MS:1001742",
                        name = "LTQ Orbitrap Velos",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    "LTQ Orbitrap", new CVParamType
                    {
                        accession = "MS:1000449",
                        name = "LTQ Orbitrap",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    "LTQ Velos", new CVParamType
                    {
                        accession = "MS:1000855",
                        name = "LTQ Velos",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    "Orbitrap Fusion", new CVParamType
                    {
                        accession = "MS:1002416",
                        name = "Orbitrap Fusion",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    "Q Exactive", new CVParamType
                    {
                        accession = "MS:1001911",
                        name = "Q Exactive",
                        cvRef = "MS",
                        value = "",
                    }
                },
                {
                    "Q Exactive Orbitrap", new CVParamType
                    {
                        accession = "MS:1001911",
                        name = "Q Exactive",
                        cvRef = "MS",
                        value = "",
                    }
                }
            };

        private IRawDataPlus rawFile;

        // Use ordered dictionary because the order of the analyzers is of importance        
        private readonly OrderedDictionary _massAnalyzers =
            new OrderedDictionary();

        private readonly Dictionary<IonizationModeType, CVParamType> _ionizationTypes =
            new Dictionary<IonizationModeType, CVParamType>();

        private readonly XmlSerializer mzmlSerializer = new XmlSerializer(typeof(mzMLType));

        public MzMLSpectrumWriter(ParseInput parseInput) : base(parseInput)
        {
        }

        public override void WriteSpectra(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            this.rawFile = rawFile;
            var mzMl = InitializeMz();

            List<SpectrumType> spectra = new List<SpectrumType>();
            for (int scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
            {
                SpectrumType spectrum = constructSpectrum(rawFile, scanNumber);
                if (spectrum != null)
                {
                    spectra.Add(spectrum);
                }
            }

            if (spectra.Count > 0)
            {
                mzMl.run.spectrumList.spectrum = new SpectrumType[spectra.Count];
                mzMl.run.spectrumList.count = spectra.Count.ToString();
                for (int i = 0; i < spectra.Count; i++)
                {
                    SpectrumType spectrum = spectra[i];
                    spectrum.index = i.ToString();
                    mzMl.run.spectrumList.spectrum[i] = spectrum;
                }
            }

            constructInstrumentConfigurationList(mzMl);

            using (TextWriter writer =
                new StreamWriter(ParseInput.OutputDirectory + "//" + ParseInput.RawFileNameWithoutExtension + ".mzml"))
            {
                mzmlSerializer.Serialize(writer, mzMl);
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
                count = "3",
                cv = new CVType[3]
            };

            mzMl.cvList.cv[0] = new CVType()
            {
                URI = @"http://purl.obolibrary.org/obo/ms.owl",
                fullName = "Mass spectrometry ontology",
                id = "MS",
                version = "20-06-2018"
            };

            mzMl.cvList.cv[1] = new CVType()
            {
                URI = @"http://purl.obolibrary.org/obo/uo.owl",
                fullName = "Unit Ontology",
                id = "UO",
                version = "2018-03-24"
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

            // Software
            mzMl.softwareList = new SoftwareListType
            {
                count = "1",
                software = new SoftwareType[2]
            };

            mzMl.softwareList.software[0] = new SoftwareType
            {
                id = "ThermoRawFileParser",
                version = "1.0.0",
                cvParam = new CVParamType[1]
            };

            mzMl.softwareList.software[0].cvParam[0] = new CVParamType
            {
                accession = "MS:1000799",
                value = "ThermoRawFileParser",
                name = "custom unreleased software tool",
                cvRef = "MS"
            };

            // Data processing
            mzMl.dataProcessingList = new DataProcessingListType
            {
                count = "1",
                dataProcessing = new DataProcessingType[1]
            };
            mzMl.dataProcessingList.dataProcessing[0] = new DataProcessingType
            {
                id = "ThermoRawFileParserProcessing",
                processingMethod = new ProcessingMethodType[1],
            };
            mzMl.dataProcessingList.dataProcessing[0].processingMethod[0] = new ProcessingMethodType
            {
                order = "0",
                softwareRef = "ThermoRawFileParser",
                cvParam = new CVParamType[1],
            };
            mzMl.dataProcessingList.dataProcessing[0].processingMethod[0].cvParam[0] = new CVParamType
            {
                accession = "MS:1000544",
                cvRef = "MS",
                name = "Conversion to mzML",
                value = ""
            };

            mzMl.run = new RunType()
            {
                id = ParseInput.RawFileNameWithoutExtension,
                startTimeStampSpecified = true,
                startTimeStamp = rawFile.CreationDate,
                spectrumList = new SpectrumListType()
                {
                    defaultDataProcessingRef = "ThermoRawFileParserProcessing"
                }
            };

            return mzMl;
        }

        private void constructInstrumentConfigurationList(mzMLType mzMl)
        {
            var instrumentData = rawFile.GetInstrumentData();

            // Referenceable param group for common instrument properties
            mzMl.referenceableParamGroupList = new ReferenceableParamGroupListType()
            {
                count = "1",
                referenceableParamGroup = new ReferenceableParamGroupType[1]
            };
            mzMl.referenceableParamGroupList.referenceableParamGroup[0] = new ReferenceableParamGroupType()
            {
                id = "commonInstrumentParams",
                cvParam = new CVParamType[3]
            };

            // Instrument model
            CVParamType instrumentModel;
            if (!InstrumentModels.TryGetValue(instrumentData.Name, out instrumentModel))
            {
                instrumentModel = new CVParamType()
                {
                    accession = "MS:1000483",
                    name = "Thermo Fisher Scientific instrument model",
                    cvRef = "MS",
                    value = "",
                };
            }

            mzMl.referenceableParamGroupList.referenceableParamGroup[0].cvParam[0] = instrumentModel;

            // Instrument serial number
            mzMl.referenceableParamGroupList.referenceableParamGroup[0].cvParam[1] = new CVParamType
            {
                cvRef = "MS",
                accession = "MS:1000529",
                name = "instrument serial number",
                value = instrumentData.SerialNumber
            };

            // Instrument software version
            mzMl.referenceableParamGroupList.referenceableParamGroup[0].cvParam[2] = new CVParamType
            {
                cvRef = "NCIT",
                accession = "NCIT:C111093",
                name = "Software Version",
                value = instrumentData.SoftwareVersion
            };

            if (_massAnalyzers.Count == 0)
            {
                _massAnalyzers.Add(MassAnalyzerType.Any, MassAnalyzerTypes[MassAnalyzerType.Any]);
            }

            // Set the run default instrument configuration ref
            mzMl.run.defaultInstrumentConfigurationRef = "IC1";

            InstrumentConfigurationListType instrumentConfigurationList = new InstrumentConfigurationListType()
            {
                count = _massAnalyzers.Count.ToString(),
                instrumentConfiguration = new InstrumentConfigurationType[_massAnalyzers.Count]
            };
            // Make a new instrument configuration for each analyzer
            for (int i = 0; i < _massAnalyzers.Count; i++)
            {
                instrumentConfigurationList.instrumentConfiguration[i] = new InstrumentConfigurationType
                {
                    id = instrumentData.Name,
                    referenceableParamGroupRef = new ReferenceableParamGroupRefType[1],
                    componentList = new ComponentListType(),
                    cvParam = new CVParamType[3]
                };
                instrumentConfigurationList.instrumentConfiguration[i].referenceableParamGroupRef[0] =
                    new ReferenceableParamGroupRefType()
                    {
                        @ref = "commonInstrumentParams"
                    };

                instrumentConfigurationList.instrumentConfiguration[i].componentList =
                    new ComponentListType
                    {
                        count = "3",
                        source = new SourceComponentType[1],
                        analyzer = new AnalyzerComponentType[1],
                        detector = new DetectorComponentType[1],
                    };

                // Instrument source                                
                if (_ionizationTypes.IsNullOrEmpty())
                {
                    _ionizationTypes.Add(IonizationModeType.Any, IonizationTypes[IonizationModeType.Any]);
                }

                instrumentConfigurationList.instrumentConfiguration[i].componentList.source[0] =
                    new SourceComponentType
                    {
                        order = 1,
                        cvParam = new CVParamType[_ionizationTypes.Count]
                    };

                int index = 0;
                // Ionization type
                foreach (KeyValuePair<IonizationModeType, CVParamType> ionizationType in _ionizationTypes)
                {
                    instrumentConfigurationList.instrumentConfiguration[i].componentList.source[0].cvParam[index] =
                        ionizationType.Value;
                    index++;
                }

                // Instrument analyzer             
                // Mass analyer type                    
                instrumentConfigurationList.instrumentConfiguration[i].componentList.analyzer[0] =
                    new AnalyzerComponentType
                    {
                        order = (index + 1),
                        cvParam = new CVParamType[1]
                    };
                instrumentConfigurationList.instrumentConfiguration[i].componentList.analyzer[0].cvParam[0] =
                    (CVParamType) _massAnalyzers[i];
                index++;

                // Instrument detector
                // TODO find a detector type
                instrumentConfigurationList.instrumentConfiguration[i].componentList.detector[0] =
                    new DetectorComponentType
                    {
                        order = (index + 1),
                        cvParam = new CVParamType[1]
                    };
                instrumentConfigurationList.instrumentConfiguration[i].componentList.detector[0].cvParam[0] =
                    new CVParamType
                    {
                        cvRef = "MS",
                        accession = "MS:1000026",
                        name = "detector type",
                        value = ""
                    };
            }

            mzMl.instrumentConfigurationList = instrumentConfigurationList;
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
                id = ConstructSpectrumTitle(scanNumber)
            };

            // Keep the CV params in a list and convert to an array afterwards
            List<CVParamType> spectrumCvParams = new List<CVParamType>();

            // MS level
            spectrumCvParams.Add(new CVParamType
            {
                name = "ms level",
                accession = "MS:1000511",
                value = ((int) scanFilter.MSOrder).ToString(CultureInfo.InvariantCulture),
                cvRef = "MS"
            });

            // Trailer extra data list
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

            // Construct and set the scan list element of the spectrum
            var scanListType = ConstructScanList(scanNumber, scan, scanFilter, scanEvent, monoisotopicMass);
            spectrum.scanList = scanListType;

            if (scanFilter.MSOrder == MSOrderType.Ms)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    accession = "MS:1000579",
                    cvRef = "MS",
                    name = "MS1 spectrum",
                    value = ""
                });
            }
            else if (scanFilter.MSOrder == MSOrderType.Ms2)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    accession = "MS:1000580",
                    cvRef = "MS",
                    name = "MSn spectrum",
                    value = ""
                });

                // Construct and set the precursor list element of the spectrum
                var precursorListType = ConstructPrecursorList(scanNumber, scan, scanEvent, charge);
                spectrum.precursorList = precursorListType;
            }

            // Spectrum scan data
            ScanDataType scanData = scanFilter.ScanData;
            switch (scanData)
            {
                case ScanDataType.Profile:
                    spectrumCvParams.Add(new CVParamType
                    {
                        accession = "MS:1000128",
                        cvRef = "MS",
                        name = "profile spectrum",
                        value = ""
                    });
                    break;
                case ScanDataType.Centroid:
                    spectrumCvParams.Add(new CVParamType
                    {
                        accession = "MS:1000127",
                        cvRef = "MS",
                        name = "centroid spectrum",
                        value = ""
                    });
                    break;
            }

            // Scan polarity            
            PolarityType polarityType = scanFilter.Polarity;
            switch (polarityType)
            {
                case PolarityType.Positive:
                    spectrumCvParams.Add(new CVParamType
                    {
                        accession = "MS:1000130",
                        cvRef = "MS",
                        name = "positive scan",
                        value = ""
                    });
                    break;
                case PolarityType.Negative:
                    spectrumCvParams.Add(new CVParamType
                    {
                        accession = "MS:1000129",
                        cvRef = "MS",
                        name = "negative scan",
                        value = ""
                    });
                    break;
            }

            // Total ion current
            spectrumCvParams.Add(new CVParamType
            {
                name = "total ion current",
                accession = "MS:1000285",
                value = scan.ScanStatistics.TIC.ToString(CultureInfo.InvariantCulture),
                cvRef = "MS",
            });

            double? basePeakMass = null;
            double? basePeakIntensity = null;
            double? lowestObservedMz = null;
            double? highestObservedMz = null;
            if (scan.HasCentroidStream)
            {
                var centroidStream = rawFile.GetCentroidStream(scanNumber, false);
                if (scan.CentroidScan.Length > 0)
                {
                    basePeakMass = centroidStream.BasePeakMass;
                    basePeakIntensity = centroidStream.BasePeakIntensity;
                    lowestObservedMz = centroidStream.Masses[0];
                    highestObservedMz = centroidStream.Masses[centroidStream.Masses.Length - 1];
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
                if (segmentedScan.Positions.Length > 0)
                {
                    lowestObservedMz = segmentedScan.Positions[0];
                    highestObservedMz = segmentedScan.Positions[segmentedScan.Positions.Length - 1];
                }
            }

            // Base peak m/z
            if (basePeakMass != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "base peak m/z",
                    accession = "MS:1000504",
                    value = basePeakMass.ToString(),
                    unitCvRef = "MS",
                    unitName = "m/z",
                    unitAccession = "MS:1000040",
                    cvRef = "MS"
                });
            }

            //base peak intensity
            if (basePeakMass != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "base peak intensity",
                    accession = "MS:1000505",
                    value = basePeakIntensity.ToString(),
                    unitCvRef = "MS",
                    unitName = "number of detector counts",
                    unitAccession = "MS:1000131",
                    cvRef = "MS"
                });
            }

            // Lowest observed mz
            if (lowestObservedMz != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "lowest observed m/z",
                    accession = "MS:1000528",
                    value = lowestObservedMz.ToString(),
                    unitCvRef = "MS",
                    unitAccession = "MS:1000040",
                    unitName = "m/z",
                    cvRef = "MS"
                });
            }

            // Highest observed mz
            if (highestObservedMz != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "highest observed m/z",
                    accession = "MS:1000527",
                    value = highestObservedMz.ToString(),
                    unitAccession = "MS:1000040",
                    unitName = "m/z",
                    unitCvRef = "MS",
                    cvRef = "MS"
                });
            }

            // Ionization type
            if (!_ionizationTypes.ContainsKey(scanFilter.IonizationMode))
            {
                _ionizationTypes.Add(scanFilter.IonizationMode, IonizationTypes[scanFilter.IonizationMode]);
            }

            // Mass analyzer
            if (!_massAnalyzers.Contains(scanFilter.MassAnalyzer) &&
                MassAnalyzerTypes.ContainsKey(scanFilter.MassAnalyzer))
            {
                _massAnalyzers.Add(scanFilter.MassAnalyzer, MassAnalyzerTypes[scanFilter.MassAnalyzer]);
            }


            // Add the mass analyzer ref           


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

            // Add the CV params to the spectrum
            spectrum.cvParam = spectrumCvParams.ToArray();

            return spectrum;
        }

        private PrecursorListType ConstructPrecursorList(int scanNumber, Scan scan, IScanEvent scanEvent,
            int? charge)
        {
            PrecursorListType precursorList = new PrecursorListType();

            // Construct the precursor
            precursorList = new PrecursorListType
            {
                count = "1",
                precursor = new PrecursorType[1],
            };
            precursorList.precursor[0] = new PrecursorType
            {
                selectedIonList = new SelectedIonListType()
                {
                    count = 1.ToString(),
                    selectedIon = new ParamGroupType[1]
                },
                spectrumRef = ConstructSpectrumTitle(scanNumber)
            };

            precursorList.precursor[0].selectedIonList.selectedIon[0] =
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
            precursorList.precursor[0].selectedIonList.selectedIon[0]
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

            precursorList.precursor[0].selectedIonList.selectedIon[0]
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

            precursorList.precursor[0].isolationWindow =
                new ParamGroupType
                {
                    cvParam = new CVParamType[3]
                };
            precursorList.precursor[0].isolationWindow.cvParam[0] =
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
            precursorList.precursor[0].isolationWindow.cvParam[1] =
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
            precursorList.precursor[0].isolationWindow.cvParam[2] =
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

            precursorList.precursor[0].activation =
                new ParamGroupType
                {
                    cvParam = new CVParamType[2]
                };
            precursorList.precursor[0].activation.cvParam[0] = new CVParamType()
            {
                accession = "MS:1000045",
                name = "collision energy",
                cvRef = "MS",
                value = "",
            };

            CVParamType activation;
            if (!DissociationTypes.TryGetValue(reaction.ActivationType, out activation))
            {
                activation = new CVParamType()
                {
                    accession = "MS:1000044",
                    name = "Activation Method",
                    cvRef = "MS",
                    value = "",
                };
            }

            precursorList.precursor[0].activation.cvParam[1] = activation;

            return precursorList;
        }

        private ScanListType ConstructScanList(int scanNumber, Scan scan, IScanFilter scanFilter, IScanEvent scanEvent,
            double? monoisotopicMass)
        {
            // Scan list
            ScanListType scanList = new ScanListType
            {
                count = "1",
                scan = new ScanType[1],
                cvParam = new CVParamType[1],
            };

            scanList.cvParam[0] = new CVParamType
            {
                accession = "MS:1000795",
                cvRef = "MS",
                name = "no combination",
                value = ""
            };

            // Instrument configuration
            
            
            // Reference the right instrument configuration
            string instrumentConfigurationRef = "IC1";
            if (scanFilter.MSOrder == MSOrderType.Ms2 && _massAnalyzers.Count > 1)
            {
                instrumentConfigurationRef = "IC2";
            }

            scanList.scan[0] = new ScanType()
            {
                instrumentConfigurationRef = instrumentConfigurationRef,
                cvParam = new CVParamType[2],
            };

            scanList.scan[0].cvParam[0] = new CVParamType
            {
                name = "scan start time",
                accession = "MS:1000016",
                value = rawFile.RetentionTimeFromScanNumber(scanNumber).ToString(CultureInfo.InvariantCulture),
                unitCvRef = "UO",
                unitAccession = "UO:0000031",
                unitName = "minute",
                cvRef = "MS",
            };

            scanList.scan[0].cvParam[1] = new CVParamType
            {
                name = "filter string",
                accession = "MS:1000512",
                value = scanEvent.ToString(),
                cvRef = "MS",
            };

            if (monoisotopicMass.HasValue)
            {
                scanList.scan[0].userParam = new UserParamType[1];
                scanList.scan[0].userParam[0] = new UserParamType()
                {
                    name = "[Thermo Trailer Extra]Monoisotopic M/Z:",
                    value = monoisotopicMass.ToString(),
                    type = "xsd:float"
                };
            }

            // Scan window list
            scanList.scan[0].scanWindowList = new ScanWindowListType()
            {
                count = 1,
                scanWindow = new ParamGroupType[1]
            };
            scanList.scan[0].scanWindowList.scanWindow[0] = new ParamGroupType()
            {
                cvParam = new CVParamType[2]
            };
            scanList.scan[0].scanWindowList.scanWindow[0].cvParam[0] = new CVParamType()
            {
                name = "scan window lower limit",
                accession = "MS:1000501",
                value = scan.ScanStatistics.LowMass.ToString(CultureInfo.InvariantCulture),
                cvRef = "MS",
                unitAccession = "MS:1000040",
                unitCvRef = "MS",
                unitName = "m/z"
            };
            scanList.scan[0].scanWindowList.scanWindow[0].cvParam[1] = new CVParamType()
            {
                name = "scan window upper limit",
                accession = "MS:1000500",
                value = scan.ScanStatistics.HighMass.ToString(CultureInfo.InvariantCulture),
                cvRef = "MS",
                unitAccession = "MS:1000040",
                unitCvRef = "MS",
                unitName = "m/z"
            };

            return scanList;
        }
    }
}