using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Serialization;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Writer.MzML;
using zlib;
using CVParamType = ThermoRawFileParser.Writer.MzML.CVParamType;
using SourceFileType = ThermoRawFileParser.Writer.MzML.SourceFileType;
using UserParamType = ThermoRawFileParser.Writer.MzML.UserParamType;

namespace ThermoRawFileParser.Writer
{
    public class MzMlSpectrumWriter : SpectrumWriter
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IRawDataPlus _rawFile;

        // Dictionary to keep track of the different mass analyzers (key: Thermo MassAnalyzerType; value: the reference string)       
        private readonly Dictionary<MassAnalyzerType, string> _massAnalyzers =
            new Dictionary<MassAnalyzerType, string>();

        // Dictionary to keep track of the different ionization modes (key: Thermo IonizationModeType; value: the reference string)
        private readonly Dictionary<IonizationModeType, CVParamType> _ionizationTypes =
            new Dictionary<IonizationModeType, CVParamType>();

        public MzMlSpectrumWriter(ParseInput parseInput) : base(parseInput)
        {
        }

        /// <inheritdoc />
        public override void Write(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            _rawFile = rawFile;

            // Initialize the mzML root element
            var mzMl = InitializeMzMl();

            // Add the spectra to a temporary list
            var spectra = new List<SpectrumType>();
            for (var scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
            {
                var spectrum = ConstructSpectrum(scanNumber);
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
                for (var i = 0; i < spectra.Count; i++)
                {
                    var spectrum = spectra[i];
                    spectrum.index = i.ToString();
                    mzMl.run.spectrumList.spectrum[i] = spectrum;
                }
            }

            // Construct and add the instrument configuration(s)
            ConstructInstrumentConfigurationList(mzMl);

            // Add the chromatogram data
            var chromatograms = ConstructChromatograms(firstScanNumber, lastScanNumber);
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
            using (Writer)
            {
                var mzmlSerializer = new XmlSerializer(typeof(mzMLType));
                mzmlSerializer.Serialize(Writer, mzMl);
            }
        }

        /// <summary>
        /// Initialize the mzML root element
        /// </summary>
        /// <returns></returns>
        private mzMLType InitializeMzMl()
        {
            var mzMl = new mzMLType
            {
                version = "1.1.0",
                id = ParseInput.RawFileNameWithoutExtension
            };

            // Add the controlled vocabularies     
            var cvs = new List<CVType>
            {
                new CVType
                {
                    URI = @"https://raw.githubusercontent.com/HUPO-PSI/psi-ms-CV/master/psi-ms.obo",
                    fullName = "Mass spectrometry ontology",
                    id = "MS",
                    version = "4.1.12"
                },
                new CVType
                {
                    URI =
                        @"https://raw.githubusercontent.com/bio-ontology-research-group/unit-ontology/master/unit.obo",
                    fullName = "Unit Ontology",
                    id = "UO",
                    version = "09:04:2014"
                }
            };

            mzMl.cvList = new CVListType
            {
                count = cvs.Count.ToString(),
                cv = cvs.ToArray()
            };

            // File description
            mzMl.fileDescription = new FileDescriptionType
            {
                fileContent = new ParamGroupType(),
                sourceFileList = new SourceFileListType()
            };

            // File description content
            var fileContentCvParams = new List<CVParamType>
            {
                // MS1
                new CVParamType
                {
                    accession = "MS:1000579", // MS1 Data
                    name = "MS1 spectrum",
                    cvRef = "MS",
                    value = ""
                },
                // MS2
                new CVParamType
                {
                    accession = "MS:1000580", // MSn Data
                    name = "MSn spectrum",
                    cvRef = "MS",
                    value = ""
                }
            };
            mzMl.fileDescription.fileContent.cvParam = fileContentCvParams.ToArray();

            // File description source files
            mzMl.fileDescription.sourceFileList = new SourceFileListType
            {
                count = "1",
                sourceFile = new SourceFileType[1]
            };

            var sourceFileCvParams = new List<CVParamType>
            {
                new CVParamType
                {
                    accession = "MS:1000768",
                    name = "Thermo nativeID format",
                    cvRef = "MS",
                    value = ""
                },
                new CVParamType
                {
                    accession = "MS:1000563",
                    name = "Thermo RAW format",
                    cvRef = "MS",
                    value = ""
                },
                //new CVParamType
                //{
                //    accession = "MS:1000568",
                //    name = "MD5",
                //    cvRef = "MS",
                //    value = CalculateMD5Checksum()
                //},
                new CVParamType
                {
                    accession = "MS:1000569",
                    name = "SHA-1",
                    cvRef = "MS",
                    value = CalculateSHAChecksum()
                }
            };

            mzMl.fileDescription.sourceFileList.sourceFile[0] = new SourceFileType
            {
                id = ParseInput.RawFileName,
                name = ParseInput.RawFileNameWithoutExtension,
                location = ParseInput.RawFilePath,
                cvParam = sourceFileCvParams.ToArray()
            };

            // Software            
            mzMl.softwareList = new SoftwareListType
            {
                count = "1",
                software = new SoftwareType[1]
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
                processingMethod = new ProcessingMethodType[1]
            };
            mzMl.dataProcessingList.dataProcessing[0].processingMethod[0] = new ProcessingMethodType
            {
                order = "0",
                softwareRef = "ThermoRawFileParser",
                cvParam = new CVParamType[1]
            };
            mzMl.dataProcessingList.dataProcessing[0].processingMethod[0].cvParam[0] = new CVParamType
            {
                accession = "MS:1000544",
                cvRef = "MS",
                name = "Conversion to mzML",
                value = ""
            };

            // Add the run element
            mzMl.run = new RunType
            {
                id = ParseInput.RawFileNameWithoutExtension,
                startTimeStampSpecified = true,
                startTimeStamp = _rawFile.CreationDate,
                spectrumList = new SpectrumListType
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
            var instrumentData = _rawFile.GetInstrumentData();

            // Referenceable param group for common instrument properties
            mzMl.referenceableParamGroupList = new ReferenceableParamGroupListType
            {
                count = "1",
                referenceableParamGroup = new ReferenceableParamGroupType[1]
            };
            mzMl.referenceableParamGroupList.referenceableParamGroup[0] = new ReferenceableParamGroupType
            {
                id = "commonInstrumentParams",
                cvParam = new CVParamType[2]
            };

            // Instrument model
            if (!OntologyMapping.InstrumentModels.TryGetValue(instrumentData.Name, out var instrumentModel))
            {
                instrumentModel = new CVParamType
                {
                    accession = "MS:1000483",
                    name = "Thermo Fisher Scientific instrument model",
                    cvRef = "MS",
                    value = ""
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

            // Add a default analyzer if none were found
            if (_massAnalyzers.Count == 0)
            {
                _massAnalyzers.Add(MassAnalyzerType.Any, "IC1");
            }

            // Set the run default instrument configuration ref
            mzMl.run.defaultInstrumentConfigurationRef = "IC1";

            var instrumentConfigurationList = new InstrumentConfigurationListType
            {
                count = _massAnalyzers.Count.ToString(),
                instrumentConfiguration = new InstrumentConfigurationType[_massAnalyzers.Count]
            };

            // Make a new instrument configuration for each analyzer
            var massAnalyzerIndex = 0;
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
                    new ReferenceableParamGroupRefType
                    {
                        @ref = "commonInstrumentParams"
                    };
                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList =
                    new ComponentListType
                    {
                        count = "3",
                        source = new SourceComponentType[1],
                        analyzer = new AnalyzerComponentType[1],
                        detector = new DetectorComponentType[1]
                    };

                // Instrument source                                
                if (_ionizationTypes.IsNullOrEmpty())
                {
                    _ionizationTypes.Add(IonizationModeType.Any,
                        OntologyMapping.IonizationTypes[IonizationModeType.Any]);
                }

                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList.source[0] =
                    new SourceComponentType
                    {
                        order = 1,
                        cvParam = new CVParamType[_ionizationTypes.Count]
                    };

                var index = 0;
                // Ionization type
                foreach (var ionizationType in _ionizationTypes)
                {
                    instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList.source[0]
                            .cvParam[index] =
                        ionizationType.Value;
                    index++;
                }

                // Instrument analyzer             
                // Mass analyzer type                    
                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList.analyzer[0] =
                    new AnalyzerComponentType
                    {
                        order = index + 1,
                        cvParam = new CVParamType[1]
                    };
                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList.analyzer[0]
                        .cvParam[0] =
                    OntologyMapping.MassAnalyzerTypes[massAnalyzer.Key];
                index++;

                // Instrument detector                
                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList.detector[0] =
                    new DetectorComponentType
                    {
                        order = index + 1,
                        cvParam = new CVParamType[1]
                    };

                // Try to map the instrument to the detector
                var detectorCvParams = OntologyMapping.InstrumentToDetectors[instrumentModel.accession];
                CVParamType detectorCvParam;
                if (massAnalyzerIndex < detectorCvParams.Count)
                {
                    detectorCvParam = detectorCvParams[massAnalyzerIndex];
                }
                else
                {
                    detectorCvParam = OntologyMapping.InstrumentToDetectors["MS:1000483"][0];
                }

                instrumentConfigurationList.instrumentConfiguration[massAnalyzerIndex].componentList.detector[0]
                    .cvParam[0] = detectorCvParam;
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
            var chromatograms = new List<ChromatogramType>();

            // Define the settings for getting the Base Peak chromatogram
            var settings = new ChromatogramTraceSettings(TraceType.BasePeak);

            // Get the chromatogram from the RAW file. 
            var data = _rawFile.GetChromatogramData(new IChromatogramSettings[] {settings}, firstScanNumber,
                lastScanNumber);

            // Split the data into the chromatograms
            var trace = ChromatogramSignal.FromChromatogramData(data);

            for (var i = 0; i < trace.Length; i++)
            {
                if (trace[i].Length > 0)
                {
                    // Binary data array list
                    var binaryData = new List<BinaryDataArrayType>();

                    var chromatogram = new ChromatogramType
                    {
                        index = i.ToString(),
                        id = "base_peak_" + i,
                        defaultArrayLength = 0,
                        binaryDataArrayList = new BinaryDataArrayListType
                        {
                            count = "2",
                            binaryDataArray = new BinaryDataArrayType[2]
                        },
                        cvParam = new CVParamType[1]
                    };
                    chromatogram.cvParam[0] = new CVParamType
                    {
                        accession = "MS:1000235",
                        name = "total ion current chromatogram",
                        cvRef = "MS",
                        value = ""
                    };

                    // Chromatogram times
                    if (!trace[i].Times.IsNullOrEmpty())
                    {
                        // Set the chromatogram default array length
                        chromatogram.defaultArrayLength = trace[i].Times.Count;

                        var timesBinaryData =
                            new BinaryDataArrayType
                            {
                                binary = GetZLib64BitArray(trace[i].Times)
                            };
                        timesBinaryData.encodedLength =
                            (4 * Math.Ceiling((double) timesBinaryData
                                                  .binary.Length / 3)).ToString(CultureInfo.InvariantCulture);
                        timesBinaryData.cvParam =
                            new CVParamType[3];
                        timesBinaryData.cvParam[0] =
                            new CVParamType
                            {
                                accession = "MS:1000595",
                                name = "time array",
                                cvRef = "MS",
                                unitName = "minute",
                                value = "",
                                unitCvRef = "UO",
                                unitAccession = "UO:0000031"
                            };
                        timesBinaryData.cvParam[1] =
                            new CVParamType
                            {
                                accession = "MS:1000523",
                                name = "64-bit float",
                                cvRef = "MS",
                                value = ""
                            };
                        timesBinaryData.cvParam[2] =
                            new CVParamType
                            {
                                accession = "MS:1000574",
                                name = "zlib compression",
                                cvRef = "MS",
                                value = ""
                            };

                        binaryData.Add(timesBinaryData);
                    }

                    // Chromatogram intensities                    
                    if (!trace[i].Times.IsNullOrEmpty())
                    {
                        // Set the spectrum default array length if necessary
                        if (chromatogram.defaultArrayLength == 0)
                        {
                            chromatogram.defaultArrayLength = trace[i].Intensities.Count;
                        }

                        var intensitiesBinaryData =
                            new BinaryDataArrayType
                            {
                                binary = GetZLib64BitArray(trace[i].Intensities)
                            };
                        intensitiesBinaryData.encodedLength =
                            (4 * Math.Ceiling((double) intensitiesBinaryData
                                                  .binary.Length / 3)).ToString(CultureInfo.InvariantCulture);
                        intensitiesBinaryData.cvParam =
                            new CVParamType[3];
                        intensitiesBinaryData.cvParam[0] =
                            new CVParamType
                            {
                                accession = "MS:1000515",
                                name = "intensity array",
                                cvRef = "MS",
                                unitName = "number of counts",
                                value = "",
                                unitCvRef = "MS",
                                unitAccession = "MS:1000131"
                            };
                        intensitiesBinaryData.cvParam[1] =
                            new CVParamType
                            {
                                accession = "MS:1000523",
                                name = "64-bit float",
                                cvRef = "MS",
                                value = ""
                            };
                        intensitiesBinaryData.cvParam[2] =
                            new CVParamType
                            {
                                accession = "MS:1000574",
                                name = "zlib compression",
                                cvRef = "MS",
                                value = ""
                            };

                        binaryData.Add(intensitiesBinaryData);
                    }

                    if (!binaryData.IsNullOrEmpty())
                    {
                        chromatogram.binaryDataArrayList = new BinaryDataArrayListType
                        {
                            count = binaryData.Count.ToString(),
                            binaryDataArray = binaryData.ToArray()
                        };
                    }

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
            var scan = Scan.FromFile(_rawFile, scanNumber);

            // Get the scan filter for this scan number
            var scanFilter = _rawFile.GetFilterForScanNumber(scanNumber);

            // Get the scan event for this scan number
            var scanEvent = _rawFile.GetScanEventForScanNumber(scanNumber);
            var spectrum = new SpectrumType
            {
                id = ConstructSpectrumTitle(scanNumber),
                defaultArrayLength = 0
            };

            // Add the ionization type if necessary
            if (!_ionizationTypes.ContainsKey(scanFilter.IonizationMode))
            {
                _ionizationTypes.Add(scanFilter.IonizationMode,
                    OntologyMapping.IonizationTypes[scanFilter.IonizationMode]);
            }

            // Add the mass analyzer if necessary
            if (!_massAnalyzers.ContainsKey(scanFilter.MassAnalyzer) &&
                OntologyMapping.MassAnalyzerTypes.ContainsKey(scanFilter.MassAnalyzer))
            {
                _massAnalyzers.Add(scanFilter.MassAnalyzer, "IC" + (_massAnalyzers.Count + 1));
            }

            // Keep the CV params in a list and convert to array afterwards
            var spectrumCvParams = new List<CVParamType>
            {
                new CVParamType
                {
                    name = "ms level",
                    accession = "MS:1000511",
                    value = ((int) scanFilter.MSOrder).ToString(CultureInfo.InvariantCulture),
                    cvRef = "MS"
                }
            };

            // Trailer extra data list
            var trailerData = _rawFile.GetTrailerExtraInformation(scanNumber);
            int? charge = null;
            double? monoisotopicMass = null;
            int? masterScanNumber = null;
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
                    monoisotopicMass = double.Parse(trailerData.Values[i]);
                }

                if (trailerData.Labels[i] == "Master Index:")
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

            switch (scanFilter.MSOrder)
            {
                case MSOrderType.Ms:
                    spectrumCvParams.Add(new CVParamType
                    {
                        accession = "MS:1000579",
                        cvRef = "MS",
                        name = "MS1 spectrum",
                        value = ""
                    });
                    break;
                case MSOrderType.Ms2:
                    spectrumCvParams.Add(new CVParamType
                    {
                        accession = "MS:1000580",
                        cvRef = "MS",
                        name = "MSn spectrum",
                        value = ""
                    });

                    // Construct and set the precursor list element of the spectrum
                    var precursorListType = ConstructPrecursorList(masterScanNumber, scanEvent, charge);
                    spectrum.precursorList = precursorListType;
                    break;
                case MSOrderType.Ng:
                    break;
                case MSOrderType.Nl:
                    break;
                case MSOrderType.Par:
                    break;
                case MSOrderType.Any:
                    break;
                case MSOrderType.Ms3:
                    break;
                case MSOrderType.Ms4:
                    break;
                case MSOrderType.Ms5:
                    break;
                case MSOrderType.Ms6:
                    break;
                case MSOrderType.Ms7:
                    break;
                case MSOrderType.Ms8:
                    break;
                case MSOrderType.Ms9:
                    break;
                case MSOrderType.Ms10:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Spectrum scan data
//            var scanData = scanFilter.ScanData;
//            switch (scanData)
//            {
//                case ScanDataType.Profile:
//                    spectrumCvParams.Add(new CVParamType
//                    {
//                        accession = "MS:1000128",
//                        cvRef = "MS",
//                        name = "profile spectrum",
//                        value = ""
//                    });
//                    break;
//                case ScanDataType.Centroid:
//                    spectrumCvParams.Add(new CVParamType
//                    {
//                        accession = "MS:1000127",
//                        cvRef = "MS",
//                        name = "centroid spectrum",
//                        value = ""
//                    });
//                    break;
//                case ScanDataType.Any:
//                    break;
//                default:
//                    throw new ArgumentOutOfRangeException();
//            }

            // Scan polarity            
            var polarityType = scanFilter.Polarity;
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
                case PolarityType.Any:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Total ion current
            spectrumCvParams.Add(new CVParamType
            {
                name = "total ion current",
                accession = "MS:1000285",
                value = scan.ScanStatistics.TIC.ToString(CultureInfo.InvariantCulture),
                cvRef = "MS"
            });

            double? basePeakMass = null;
            double? basePeakIntensity = null;
            double? lowestObservedMz = null;
            double? highestObservedMz = null;
            double[] masses = null;
            double[] intensities = null;
            if (scan.HasCentroidStream)
            {
                var centroidStream = _rawFile.GetCentroidStream(scanNumber, false);
                if (scan.CentroidScan.Length > 0)
                {
                    basePeakMass = centroidStream.BasePeakMass;
                    basePeakIntensity = centroidStream.BasePeakIntensity;
                    lowestObservedMz = centroidStream.Masses[0];
                    highestObservedMz = centroidStream.Masses[centroidStream.Masses.Length - 1];
                    masses = centroidStream.Masses;
                    intensities = centroidStream.Intensities;

                    // Note that although the scan data type is profile,
                    // centroid data might be available
                    spectrumCvParams.Add(new CVParamType
                    {
                        accession = "MS:1000127",
                        cvRef = "MS",
                        name = "centroid spectrum",
                        value = ""
                    });
                }
            }
            else
            {
                // Get the scan statistics from the RAW file for this scan number
                var scanStatistics = _rawFile.GetScanStatsForScanNumber(scanNumber);

                basePeakMass = scanStatistics.BasePeakMass;
                basePeakIntensity = scanStatistics.BasePeakIntensity;

                // Get the segmented (low res and profile) scan data
                var segmentedScan = _rawFile.GetSegmentedScanFromScanNumber(scanNumber, scanStatistics);
                if (segmentedScan.Positions.Length > 0)
                {
                    lowestObservedMz = segmentedScan.Positions[0];
                    highestObservedMz = segmentedScan.Positions[segmentedScan.Positions.Length - 1];
                    masses = segmentedScan.Positions;
                    intensities = segmentedScan.Intensities;

                    spectrumCvParams.Add(new CVParamType
                    {
                        accession = "MS:1000128",
                        cvRef = "MS",
                        name = "profile spectrum",
                        value = ""
                    });
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
            var binaryData = new List<BinaryDataArrayType>();

            // M/Z Data
            if (!masses.IsNullOrEmpty())
            {
                // Set the spectrum default array length
                spectrum.defaultArrayLength = masses.Length;

                var massesBinaryData =
                    new BinaryDataArrayType
                    {
                        binary = GetZLib64BitArray(masses)
                    };
                massesBinaryData.encodedLength =
                    (4 * Math.Ceiling((double) massesBinaryData
                                          .binary.Length / 3)).ToString(CultureInfo.InvariantCulture);
                massesBinaryData.cvParam =
                    new CVParamType[3];
                massesBinaryData.cvParam[0] =
                    new CVParamType
                    {
                        accession = "MS:1000514",
                        name = "m/z array",
                        cvRef = "MS",
                        unitName = "m/z",
                        value = "",
                        unitCvRef = "MS",
                        unitAccession = "MS:1000040"
                    };
                massesBinaryData.cvParam[1] =
                    new CVParamType
                    {
                        accession = "MS:1000523",
                        name = "64-bit float",
                        cvRef = "MS",
                        value = ""
                    };
                massesBinaryData.cvParam[2] =
                    new CVParamType
                    {
                        accession = "MS:1000574",
                        name = "zlib compression",
                        cvRef = "MS",
                        value = ""
                    };

                binaryData.Add(massesBinaryData);
            }

            // Intensity Data
            if (!intensities.IsNullOrEmpty())
            {
                // Set the spectrum default array length if necessary
                if (spectrum.defaultArrayLength == 0)
                {
                    spectrum.defaultArrayLength = masses.Length;
                }

                var intensitiesBinaryData =
                    new BinaryDataArrayType
                    {
                        binary = GetZLib64BitArray(intensities)
                    };
                intensitiesBinaryData.encodedLength =
                    (4 * Math.Ceiling((double) intensitiesBinaryData
                                          .binary.Length / 3)).ToString(CultureInfo.InvariantCulture);
                intensitiesBinaryData.cvParam =
                    new CVParamType[3];
                intensitiesBinaryData.cvParam[0] =
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
                intensitiesBinaryData.cvParam[1] =
                    new CVParamType
                    {
                        accession = "MS:1000523",
                        name = "64-bit float",
                        cvRef = "MS",
                        value = ""
                    };
                intensitiesBinaryData.cvParam[2] =
                    new CVParamType
                    {
                        accession = "MS:1000574",
                        name = "zlib compression",
                        cvRef = "MS",
                        value = ""
                    };

                binaryData.Add(intensitiesBinaryData);
            }

            if (!binaryData.IsNullOrEmpty())
            {
                spectrum.binaryDataArrayList = new BinaryDataArrayListType
                {
                    count = binaryData.Count.ToString(),
                    binaryDataArray = binaryData.ToArray()
                };
            }

            return spectrum;
        }

        /// <summary>
        /// Populate the precursor list element
        /// </summary>
        /// <param name="masterScanNumber">the master scan number</param>
        /// <param name="scanEvent">the scan event</param>
        /// <param name="charge">the charge</param>
        /// <returns>the precursor list</returns>
        private PrecursorListType ConstructPrecursorList(int? masterScanNumber,
            IScanEventBase scanEvent,
            int? charge)
        {
            // Construct the precursor
            var precursorList = new PrecursorListType
            {
                count = "1",
                precursor = new PrecursorType[1]
            };

            var precursor = new PrecursorType
            {
                selectedIonList = new SelectedIonListType
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
            var precursorMass = 0.0;
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
            var ionCvParams = new List<CVParamType>
            {
                new CVParamType
                {
                    name = "selected ion m/z",
                    value = precursorMass.ToString(CultureInfo.InvariantCulture),
                    accession = "MS:1000744",
                    cvRef = "MS",
                    unitCvRef = "MS",
                    unitAccession = "MS:1000040",
                    unitName = "m/z"
                }
            };

            if (charge != null)
            {
                ionCvParams.Add(new CVParamType
                {
                    name = "charge state",
                    value = charge.ToString(),
                    accession = "MS:1000041",
                    cvRef = "MS"
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
                var offset = isolationWidth.Value / 2;
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

            var activationCvParams = new List<CVParamType>();
            if (reaction != null && reaction.CollisionEnergyValid)
            {
                activationCvParams.Add(
                    new CVParamType
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

            if (!OntologyMapping.DissociationTypes.TryGetValue(reaction.ActivationType, out var activation))
            {
                activation = new CVParamType
                {
                    accession = "MS:1000044",
                    name = "Activation Method",
                    cvRef = "MS",
                    value = ""
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
            var scanList = new ScanListType
            {
                count = "1",
                scan = new ScanType[1],
                cvParam = new CVParamType[1]
            };

            scanList.cvParam[0] = new CVParamType
            {
                accession = "MS:1000795",
                cvRef = "MS",
                name = "no combination",
                value = ""
            };

            // Reference the right instrument configuration
            if (!_massAnalyzers.TryGetValue(scanFilter.MassAnalyzer, out var instrumentConfigurationRef))
            {
                instrumentConfigurationRef = "IC1";
            }

            var scanType = new ScanType
            {
                instrumentConfigurationRef = instrumentConfigurationRef,
                cvParam = new CVParamType[2]
            };

            scanType.cvParam[0] = new CVParamType
            {
                name = "scan start time",
                accession = "MS:1000016",
                value = _rawFile.RetentionTimeFromScanNumber(scanNumber).ToString(CultureInfo.InvariantCulture),
                unitCvRef = "UO",
                unitAccession = "UO:0000031",
                unitName = "minute",
                cvRef = "MS"
            };

            scanType.cvParam[1] = new CVParamType
            {
                name = "filter string",
                accession = "MS:1000512",
                value = scanEvent.ToString(),
                cvRef = "MS"
            };

            if (monoisotopicMass.HasValue)
            {
                scanType.userParam = new UserParamType[1];
                scanType.userParam[0] = new UserParamType
                {
                    name = "[Thermo Trailer Extra]Monoisotopic M/Z:",
                    value = monoisotopicMass.ToString(),
                    type = "xsd:float"
                };
            }

            // Scan window list
            scanType.scanWindowList = new ScanWindowListType
            {
                count = 1,
                scanWindow = new ParamGroupType[1]
            };
            var scanWindow = new ParamGroupType
            {
                cvParam = new CVParamType[2]
            };
            scanWindow.cvParam[0] = new CVParamType
            {
                name = "scan window lower limit",
                accession = "MS:1000501",
                value = scan.ScanStatistics.LowMass.ToString(CultureInfo.InvariantCulture),
                cvRef = "MS",
                unitAccession = "MS:1000040",
                unitCvRef = "MS",
                unitName = "m/z"
            };
            scanWindow.cvParam[1] = new CVParamType
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
        /// <param name="array">the double collection</param>
        /// <returns>the byte array</returns>
        private static byte[] Get64BitArray(IEnumerable<double> array)
        {
            byte[] bytes;

            using (var memoryStream = new MemoryStream())
            {
                foreach (var doubleValue in array)
                {
                    var doubleValueByteArray = BitConverter.GetBytes(doubleValue);
                    memoryStream.Write(doubleValueByteArray, 0, doubleValueByteArray.Length);
                }

                memoryStream.Position = 0;
                bytes = memoryStream.ToArray();
            }

            return bytes;
        }

        /// <summary>
        /// Convert the double array into a compressed zlib byte array
        /// </summary>
        /// <param name="array">the double collection</param>
        /// <returns>the byte array</returns>
        private static byte[] GetZLib64BitArray(IEnumerable<double> array)
        {
            byte[] bytes;

            using (var memoryStream = new MemoryStream())
            using (var outZStream = new ZOutputStream(memoryStream, zlibConst.Z_DEFAULT_COMPRESSION))
            {
                foreach (var doubleValue in array)
                {
                    var doubleValueByteArray = BitConverter.GetBytes(doubleValue);
                    outZStream.Write(doubleValueByteArray, 0, doubleValueByteArray.Length);
                }

                outZStream.finish();
                memoryStream.Position = 0;
                bytes = memoryStream.ToArray();
            }

            return bytes;
        }

        /// <summary>
        /// Calculate the RAW file checksum
        /// </summary>
        /// <returns>the checksum string</returns>
        private string CalculateMD5Checksum()
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(ParseInput.RawFilePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }


        /// <summary>
        /// Calculate the RAW file checksum
        /// </summary>
        /// <returns>the checksum string</returns>
        private string CalculateSHAChecksum()
        {
            using (var sha1 = SHA1.Create())
            {
                using (var stream = File.OpenRead(ParseInput.RawFilePath))
                {
                    var hash = sha1.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}