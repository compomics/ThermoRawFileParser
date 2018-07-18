using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
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

        // Dictionary to keep track of the different mass analyzers (key: Thermo MassAnalyzerType; value: the reference string)       
        private readonly Dictionary<MassAnalyzerType, String> _massAnalyzers =
            new Dictionary<MassAnalyzerType, String>();

        private readonly Dictionary<IonizationModeType, CVParamType> _ionizationTypes =
            new Dictionary<IonizationModeType, CVParamType>();

        public MzMLSpectrumWriter(ParseInput parseInput) : base(parseInput)
        {
        }

        /// <inheritdoc />
        public override void Write(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            this.rawFile = rawFile;

            // Initialize the mzML root element
            var mzMl = InitializeMzMl();

            // Add the spectra to a temporary list
            List<SpectrumType> spectra = new List<SpectrumType>();
            for (int scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
            {
                SpectrumType spectrum = ConstructSpectrum(scanNumber);
                if (spectrum != null)
                {
                    spectra.Add(spectrum);
                }
            }

            // Add the spectra to the spectrum list element
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

            // Construct and add the instrument configuration(s)
            ConstructInstrumentConfigurationList(mzMl);

            // Add the chromatogram data
            List<ChromatogramType> chromatograms = ConstructChromatograms(firstScanNumber, lastScanNumber);
            if (!chromatograms.IsNullOrEmpty())
            {
                mzMl.run.chromatogramList = new ChromatogramListType
                {
                    count = chromatograms.Count.ToString(),
                    defaultDataProcessingRef = "ThermoRawFileParserProcessing",
                    chromatogram = chromatograms.ToArray()
                };
            }

            ConfigureWriter(".mzML");
            using (writer)
            {
                XmlSerializer mzmlSerializer = new XmlSerializer(typeof(mzMLType));
                mzmlSerializer.Serialize(writer, mzMl);
            }
        }

        /// <summary>
        /// Initialize the mzML root element
        /// </summary>
        /// <returns></returns>
        private mzMLType InitializeMzMl()
        {
            mzMLType mzMl = new mzMLType()
            {
                version = "1.1.0",
                cvList = new CVListType(),
                id = ParseInput.RawFileNameWithoutExtension,
            };

            // Add the controlled vocabularies
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

            // Add the run element
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

        /// <summary>
        /// Populate the instrument configuration list
        /// </summary>
        /// <param name="mzMl"></param>
        private void ConstructInstrumentConfigurationList(mzMLType mzMl)
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

            // Add a default analyzer if none were found
            if (_massAnalyzers.Count == 0)
            {
                _massAnalyzers.Add(MassAnalyzerType.Any, "IC1");
            }

            // Set the run default instrument configuration ref
            mzMl.run.defaultInstrumentConfigurationRef = "IC1";

            InstrumentConfigurationListType instrumentConfigurationList = new InstrumentConfigurationListType()
            {
                count = _massAnalyzers.Count.ToString(),
                instrumentConfiguration = new InstrumentConfigurationType[_massAnalyzers.Count]
            };
            // Make a new instrument configuration for each analyzer
            int massAnalyzerIndex = 0;
            foreach (var massAnalyzer in _massAnalyzers)
            {
                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex] = new InstrumentConfigurationType
                {
                    id = massAnalyzer.Value,
                    referenceableParamGroupRef = new ReferenceableParamGroupRefType[1],
                    componentList = new ComponentListType(),
                    cvParam = new CVParamType[3]
                };
                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].referenceableParamGroupRef[0] =
                    new ReferenceableParamGroupRefType()
                    {
                        @ref = "commonInstrumentParams"
                    };

                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList =
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

                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList.source[0] =
                    new SourceComponentType
                    {
                        order = 1,
                        cvParam = new CVParamType[_ionizationTypes.Count]
                    };

                int index = 0;
                // Ionization type
                foreach (KeyValuePair<IonizationModeType, CVParamType> ionizationType in _ionizationTypes)
                {
                    instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList.source[0]
                            .cvParam[index] =
                        ionizationType.Value;
                    index++;
                }

                // Instrument analyzer             
                // Mass analyer type                    
                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList.analyzer[0] =
                    new AnalyzerComponentType
                    {
                        order = (index + 1),
                        cvParam = new CVParamType[1]
                    };
                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList.analyzer[0]
                        .cvParam[0] =
                    MassAnalyzerTypes[massAnalyzer.Key];
                index++;

                // Instrument detector
                // TODO find a detector type
                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList.detector[0] =
                    new DetectorComponentType
                    {
                        order = (index + 1),
                        cvParam = new CVParamType[1]
                    };
                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList.detector[0]
                        .cvParam[0] =
                    new CVParamType
                    {
                        cvRef = "MS",
                        accession = "MS:1000026",
                        name = "detector type",
                        value = ""
                    };
                massAnalyzerIndex++;
            }

            mzMl.instrumentConfigurationList = instrumentConfigurationList;
        }

        /// <summary>
        /// Construct the chromatogram element(s)
        /// </summary>
        /// <param name="firstScanNumber">the first scan number</param>
        /// <param name="lastScanNumber">the last scan number</param>
        /// <returns>a list of chromatograms</returns>
        private List<ChromatogramType> ConstructChromatograms(int firstScanNumber, int lastScanNumber)
        {
            List<ChromatogramType> chromatograms = new List<ChromatogramType>();

            // Define the settings for getting the Base Peak chromatogram
            ChromatogramTraceSettings settings = new ChromatogramTraceSettings(TraceType.BasePeak);

            // Get the chromatogram from the RAW file. 
            var data = rawFile.GetChromatogramData(new IChromatogramSettings[] {settings}, firstScanNumber,
                lastScanNumber);

            // Split the data into the chromatograms
            var trace = ChromatogramSignal.FromChromatogramData(data);

            for (int i = 0; i < trace.Length; i++)
            {
                if (trace[i].Length > 0)
                {
                    ChromatogramType chromatogram = new ChromatogramType()
                    {
                        index = i.ToString(),
                        id = "base_peak_" + i,
                        defaultArrayLength = trace[i].Times.Count,
                        binaryDataArrayList = new BinaryDataArrayListType()
                        {
                            binaryDataArray = new BinaryDataArrayType[2]
                        },
                        cvParam = new CVParamType[1]
                    };
                    chromatogram.cvParam[0] = new CVParamType()
                    {
                        accession = "MS:1000235",
                        name = "total ion current chromatogram",
                        cvRef = "MS",
                        value = "",
                    };

                    // Chromatogram times
                    chromatogram.binaryDataArrayList.binaryDataArray[0] =
                        new BinaryDataArrayType
                        {
                            binary = Get64BitArray(trace[i].Times)
                        };
                    chromatogram.binaryDataArrayList.binaryDataArray[0].encodedLength =
                        (4 * Math.Ceiling(((double) chromatogram.binaryDataArrayList.binaryDataArray[0]
                                               .binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
                    chromatogram.binaryDataArrayList.binaryDataArray[0].cvParam =
                        new CVParamType[3];
                    chromatogram.binaryDataArrayList.binaryDataArray[0].cvParam[0] =
                        new CVParamType
                        {
                            accession = "MS:1000595",
                            name = "time array",
                            cvRef = "MS",
                            unitName = "minute",
                            value = "",
                            unitCvRef = "UO",
                            unitAccession = "UO:0000031",
                        };
                    chromatogram.binaryDataArrayList.binaryDataArray[0].cvParam[1] =
                        new CVParamType
                        {
                            accession = "MS:1000523",
                            name = "64-bit float",
                            cvRef = "MS",
                            value = ""
                        };
                    chromatogram.binaryDataArrayList.binaryDataArray[0].cvParam[2] =
                        new CVParamType
                        {
                            accession = "MS:1000576",
                            name = "no compression",
                            cvRef = "MS",
                            value = ""
                        };

                    // Chromatogram intensities
                    chromatogram.binaryDataArrayList.binaryDataArray[1] =
                        new BinaryDataArrayType
                        {
                            binary = Get64BitArray(trace[i].Intensities)
                        };
                    chromatogram.binaryDataArrayList.binaryDataArray[1].encodedLength =
                        (4 * Math.Ceiling(((double) chromatogram.binaryDataArrayList.binaryDataArray[1]
                                               .binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
                    chromatogram.binaryDataArrayList.binaryDataArray[1].cvParam =
                        new CVParamType[3];
                    chromatogram.binaryDataArrayList.binaryDataArray[1].cvParam[0] =
                        new CVParamType
                        {
                            accession = "MS:1000515",
                            name = "intensity array",
                            cvRef = "MS",
                            unitName = "number of counts",
                            value = "",
                            unitCvRef = "MS",
                            unitAccession = "MS:1000131",
                        };
                    chromatogram.binaryDataArrayList.binaryDataArray[1].cvParam[1] =
                        new CVParamType
                        {
                            accession = "MS:1000523",
                            name = "64-bit float",
                            cvRef = "MS",
                            value = ""
                        };
                    chromatogram.binaryDataArrayList.binaryDataArray[1].cvParam[2] =
                        new CVParamType
                        {
                            accession = "MS:1000576",
                            name = "no compression",
                            cvRef = "MS",
                            value = ""
                        };

                    chromatograms.Add(chromatogram);
                }
            }

            return chromatograms;
        }

        /// <summary>
        /// Construct a spectrum element for the given scan
        /// </summary>
        /// <param name="scanNumber">the scan number</param>
        /// <returns>The SpectrumType object</returns>
        private SpectrumType ConstructSpectrum(int scanNumber)
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

            // Add the ionization type if necessary
            if (!_ionizationTypes.ContainsKey(scanFilter.IonizationMode))
            {
                _ionizationTypes.Add(scanFilter.IonizationMode, IonizationTypes[scanFilter.IonizationMode]);
            }

            // Add the mass analyzer if necessary
            if (!_massAnalyzers.ContainsKey(scanFilter.MassAnalyzer) &&
                MassAnalyzerTypes.ContainsKey(scanFilter.MassAnalyzer))
            {
                _massAnalyzers.Add(scanFilter.MassAnalyzer, "IC" + (_massAnalyzers.Count + 1));
            }

            // Keep the CV params in a list and convert to array afterwards
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
            int? masterScanNumber = null;
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

                if ((trailerData.Labels[i] == "Master Index:"))
                {
                    if (Convert.ToInt32(trailerData.Values[i]) > 0)
                    {
                        masterScanNumber = Convert.ToInt32(trailerData.Values[i]);
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
                var precursorListType = ConstructPrecursorList(scanNumber, masterScanNumber, scan, scanEvent, charge);
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
            double[] masses = null;
            double[] intensities = null;
            if (scan.HasCentroidStream)
            {
                var centroidStream = rawFile.GetCentroidStream(scanNumber, false);
                if (scan.CentroidScan.Length > 0)
                {
                    basePeakMass = centroidStream.BasePeakMass;
                    basePeakIntensity = centroidStream.BasePeakIntensity;
                    lowestObservedMz = centroidStream.Masses[0];
                    highestObservedMz = centroidStream.Masses[centroidStream.Masses.Length - 1];
                    masses = centroidStream.Masses;
                    intensities = centroidStream.Intensities;
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
                    masses = segmentedScan.Positions;
                    intensities = segmentedScan.Intensities;
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

            // Add the CV params to the spectrum
            spectrum.cvParam = spectrumCvParams.ToArray();

            // Binary data array list
            spectrum.binaryDataArrayList = new BinaryDataArrayListType
            {
                count = "2",
                binaryDataArray = new BinaryDataArrayType[2]
            };

            // M/Z Data
            spectrum.binaryDataArrayList.binaryDataArray[0] =
                new BinaryDataArrayType
                {
                    binary = Get64BitArray(masses)
                };
            spectrum.binaryDataArrayList.binaryDataArray[0].encodedLength =
                (4 * Math.Ceiling(((double) spectrum.binaryDataArrayList.binaryDataArray[0]
                                       .binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
            spectrum.binaryDataArrayList.binaryDataArray[0].cvParam =
                new CVParamType[3];
            spectrum.binaryDataArrayList.binaryDataArray[0].cvParam[0] =
                new CVParamType
                {
                    accession = "MS:1000514",
                    name = "m/z array",
                    cvRef = "MS",
                    unitName = "m/z",
                    value = "",
                    unitCvRef = "MS",
                    unitAccession = "MS:1000040",
                };
            spectrum.binaryDataArrayList.binaryDataArray[0].cvParam[1] =
                new CVParamType
                {
                    accession = "MS:1000523",
                    name = "64-bit float",
                    cvRef = "MS",
                    value = ""
                };
            spectrum.binaryDataArrayList.binaryDataArray[0].cvParam[2] =
                new CVParamType
                {
                    accession = "MS:1000576",
                    name = "no compression",
                    cvRef = "MS",
                    value = ""
                };

            // Intensity Data
            spectrum.binaryDataArrayList.binaryDataArray[1] =
                new BinaryDataArrayType
                {
                    binary = Get64BitArray(intensities)
                };
            spectrum.binaryDataArrayList.binaryDataArray[1].encodedLength =
                (4 * Math.Ceiling(((double) spectrum.binaryDataArrayList.binaryDataArray[1]
                                       .binary.Length / 3))).ToString(CultureInfo.InvariantCulture);
            spectrum.binaryDataArrayList.binaryDataArray[1].cvParam =
                new CVParamType[3];
            spectrum.binaryDataArrayList.binaryDataArray[1].cvParam[0] =
                new CVParamType
                {
                    accession = "MS:1000515",
                    name = "intensity array",
                    cvRef = "MS",
                    unitCvRef = "MS",
                    unitAccession = "MS:1000131",
                    unitName = "number of counts",
                    value = ""
                };
            spectrum.binaryDataArrayList.binaryDataArray[1].cvParam[1] =
                new CVParamType
                {
                    accession = "MS:1000523",
                    name = "64-bit float",
                    cvRef = "MS",
                    value = ""
                };
            spectrum.binaryDataArrayList.binaryDataArray[1].cvParam[2] =
                new CVParamType
                {
                    accession = "MS:1000576",
                    name = "no compression",
                    cvRef = "MS",
                    value = ""
                };

            return spectrum;
        }

        /// <summary>
        /// Populate the precursor list element
        /// </summary>
        /// <param name="scanNumber">the scan number</param>
        /// <param name="masterScanNumber">the master scan number</param>
        /// <param name="scan">the scan object</param>
        /// <param name="scanEvent">the scan event</param>
        /// <param name="charge">the charge</param>
        /// <returns></returns>
        private PrecursorListType ConstructPrecursorList(int scanNumber, int? masterScanNumber, Scan scan,
            IScanEvent scanEvent,
            int? charge)
        {
            // Construct the precursor
            PrecursorListType precursorList = new PrecursorListType
            {
                count = "1",
                precursor = new PrecursorType[1],
            };

            PrecursorType precursor = new PrecursorType
            {
                selectedIonList = new SelectedIonListType()
                {
                    count = 1.ToString(),
                    selectedIon = new ParamGroupType[1]
                }
            };

            if (masterScanNumber != null)
            {
                precursor.spectrumRef = ConstructSpectrumTitle(masterScanNumber.Value);
            }

            precursor.selectedIonList.selectedIon[0] =
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
            List<CVParamType> ionCvParams = new List<CVParamType>();
            ionCvParams.Add(new CVParamType
            {
                name = "selected ion m/z",
                value = precursorMass.ToString(CultureInfo.InvariantCulture),
                accession = "MS:1000744",
                cvRef = "MS",
                unitCvRef = "MS",
                unitAccession = "MS:1000040",
                unitName = "m/z"
            });

            if (charge != null)
            {
                ionCvParams.Add(new CVParamType
                {
                    name = "charge state",
                    value = charge.ToString(),
                    accession = "MS:1000041",
                    cvRef = "MS",
                });
            }

            precursor.selectedIonList.selectedIon[0].cvParam = ionCvParams.ToArray();

            precursor.isolationWindow =
                new ParamGroupType
                {
                    cvParam = new CVParamType[3]
                };
            precursor.isolationWindow.cvParam[0] =
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
            if (isolationWidth != null)
            {
                double offset = isolationWidth.Value / 2;
                precursor.isolationWindow.cvParam[1] =
                    new CVParamType
                    {
                        accession = "MS:1000828",
                        name = "isolation window lower offset",
                        value = offset.ToString(CultureInfo.InvariantCulture),
                        cvRef = "MS",
                        unitCvRef = "MS",
                        unitAccession = "MS:1000040",
                        unitName = "m/z"
                    };
                precursor.isolationWindow.cvParam[2] =
                    new CVParamType
                    {
                        accession = "MS:1000829",
                        name = "isolation window upper offset",
                        value = offset.ToString(CultureInfo.InvariantCulture),
                        cvRef = "MS",
                        unitCvRef = "MS",
                        unitAccession = "MS:1000040",
                        unitName = "m/z"
                    };
            }

            List<CVParamType> activationCvParams = new List<CVParamType>();
            if (reaction != null && reaction.CollisionEnergyValid)
            {
                activationCvParams.Add(
                    new CVParamType()
                    {
                        accession = "MS:1000045",
                        name = "collision energy",
                        cvRef = "MS",
                        value = reaction.CollisionEnergy.ToString(CultureInfo.InvariantCulture),
                        unitCvRef = "UO",
                        unitAccession = "UO:0000266",
                        unitName = "electronvolt"
                    });
            }

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

            activationCvParams.Add(activation);

            precursor.activation =
                new ParamGroupType
                {
                    cvParam = activationCvParams.ToArray()
                };

            precursorList.precursor[0] = precursor;

            return precursorList;
        }

        /// <summary>
        /// Populate the scan list element
        /// </summary>
        /// <param name="scanNumber">the scan number</param>
        /// <param name="scan">the scan object</param>
        /// <param name="scanFilter">the scan filter</param>
        /// <param name="scanEvent">the scan event</param>
        /// <param name="monoisotopicMass">the monoisotopic mass</param>
        /// <returns></returns>
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

            // Reference the right instrument configuration
            string instrumentConfigurationRef;
            if (!_massAnalyzers.TryGetValue(scanFilter.MassAnalyzer, out instrumentConfigurationRef))
            {
                instrumentConfigurationRef = "IC1";
            }

            ScanType scanType = new ScanType()
            {
                instrumentConfigurationRef = instrumentConfigurationRef,
                cvParam = new CVParamType[2],
            };

            scanType.cvParam[0] = new CVParamType
            {
                name = "scan start time",
                accession = "MS:1000016",
                value = rawFile.RetentionTimeFromScanNumber(scanNumber).ToString(CultureInfo.InvariantCulture),
                unitCvRef = "UO",
                unitAccession = "UO:0000031",
                unitName = "minute",
                cvRef = "MS",
            };

            scanType.cvParam[1] = new CVParamType
            {
                name = "filter string",
                accession = "MS:1000512",
                value = scanEvent.ToString(),
                cvRef = "MS",
            };

            if (monoisotopicMass.HasValue)
            {
                scanType.userParam = new UserParamType[1];
                scanType.userParam[0] = new UserParamType()
                {
                    name = "[Thermo Trailer Extra]Monoisotopic M/Z:",
                    value = monoisotopicMass.ToString(),
                    type = "xsd:float"
                };
            }

            // Scan window list
            scanType.scanWindowList = new ScanWindowListType()
            {
                count = 1,
                scanWindow = new ParamGroupType[1]
            };
            ParamGroupType scanWindow = new ParamGroupType()
            {
                cvParam = new CVParamType[2]
            };
            scanWindow.cvParam[0] = new CVParamType()
            {
                name = "scan window lower limit",
                accession = "MS:1000501",
                value = scan.ScanStatistics.LowMass.ToString(CultureInfo.InvariantCulture),
                cvRef = "MS",
                unitAccession = "MS:1000040",
                unitCvRef = "MS",
                unitName = "m/z"
            };
            scanWindow.cvParam[1] = new CVParamType()
            {
                name = "scan window upper limit",
                accession = "MS:1000500",
                value = scan.ScanStatistics.HighMass.ToString(CultureInfo.InvariantCulture),
                cvRef = "MS",
                unitAccession = "MS:1000040",
                unitCvRef = "MS",
                unitName = "m/z"
            };

            scanType.scanWindowList.scanWindow[0] = scanWindow;

            scanList.scan[0] = scanType;

            return scanList;
        }

        /// <summary>
        /// Convert the double array into a byte array
        /// </summary>
        /// <param name="array">the double array</param>
        /// <returns>the byte array</returns>
        private byte[] Get64BitArray(IEnumerable<double> array)
        {
            MemoryStream memoryStream = new MemoryStream();
            foreach (var doubleValue in array)
            {
                byte[] doubleValueByteArray = BitConverter.GetBytes(doubleValue);
                memoryStream.Write(doubleValueByteArray, 0, doubleValueByteArray.Length);
            }

            memoryStream.Position = 0;
            return memoryStream.ToArray();
        }
    }
}