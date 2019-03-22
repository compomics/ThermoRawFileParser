using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
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
    public class MzMlSpectrumWriter2 : SpectrumWriter
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

        // Precursor scan number for reference in the precursor element of an MS2 spectrum
        private int _precursorScanNumber;

        private readonly XmlSerializerFactory factory = new XmlSerializerFactory();
        private const string Ns = "http://psi.hupo.org/ms/mzml";
        private readonly XmlSerializer cvParamSerializer;
        private readonly XmlSerializerNamespaces mzMlNamespace;
        private bool doIndexing;

        private XmlWriter _writer;
        //private XmlWriter _memoryWriter;

        public MzMlSpectrumWriter2(ParseInput parseInput) : base(parseInput)
        {
            cvParamSerializer = factory.CreateSerializer(typeof(CVParamType));
            mzMlNamespace = new XmlSerializerNamespaces();
            mzMlNamespace.Add(string.Empty, "http://psi.hupo.org/ms/mzml");
            doIndexing = ParseInput.OutputFormat == OutputFormat.IndexMzML ? true : false;
        }

        /// <inheritdoc />
        public override void Write(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            _rawFile = rawFile;

            OrderedDictionary spectrumOffSets = new OrderedDictionary();
            OrderedDictionary chromatogramOffSets = new OrderedDictionary();

            ConfigureWriter(".mzML");

            XmlSerializer serializer;
            var settings = new XmlWriterSettings {Indent = true, Encoding = Encoding.UTF8};
            var sha1 = SHA1.Create();
            CryptoStream cryptoStream = null;
            if (doIndexing)
            {
                cryptoStream = new CryptoStream(Writer.BaseStream, sha1, CryptoStreamMode.Write);
                _writer = XmlWriter.Create(cryptoStream, settings);
            }
            else
            {
                _writer = XmlWriter.Create(Writer, settings);
            }

            try
            {
                WriteStartDocument();

                if (doIndexing)
                {
                    //indexedmzML
                    WriteStartElementWithNamespace("indexedmzML");
                    WriteAttributeString("xmlns", "xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    WriteAttributeString("xsi", "schemaLocation",
                        "http://psi.hupo.org/ms/mzml http://psidev.info/files/ms/mzML/xsd/mzML1.1.0.xsd");
                }

                //  mzML
                WriteStartElementWithNamespace("mzML");
                WriteAttributeString("xmlns", "xsi", "http://www.w3.org/2001/XMLSchema-instance");
                WriteAttributeString("xsi", "schemaLocation",
                    "http://psi.hupo.org/ms/mzml http://psidev.info/files/ms/mzML/xsd/mzML1.1.0.xsd");
                WriteAttributeString("version", "1.1.0");
                WriteAttributeString("id", ParseInput.RawFileNameWithoutExtension);

                // CV list
                serializer = factory.CreateSerializer(typeof(CVType));
                WriteStartElement("cvList");
                WriteAttributeString("count", "2");
                Serialize(serializer, new CVType
                {
                    URI = @"https://raw.githubusercontent.com/HUPO-PSI/psi-ms-CV/master/psi-ms.obo",
                    fullName = "Mass spectrometry ontology",
                    id = "MS",
                    version = "4.1.12"
                });
                Serialize(serializer, new CVType
                {
                    URI =
                        @"https://raw.githubusercontent.com/bio-ontology-research-group/unit-ontology/master/unit.obo",
                    fullName = "Unit Ontology",
                    id = "UO",
                    version = "09:04:2014"
                });
                WriteEndElement(); // cvList                

                // fileDescription
                WriteStartElement("fileDescription");
                //   fileContent
                WriteStartElement("fileContent");
                //     MS1
                SerializeCvParam(new CVParamType
                {
                    accession = "MS:1000579",
                    name = "MS1 spectrum",
                    cvRef = "MS",
                    value = ""
                });
                //     MSn
                SerializeCvParam(new CVParamType
                {
                    accession = "MS:1000580",
                    name = "MSn spectrum",
                    cvRef = "MS",
                    value = ""
                });
                WriteEndElement(); // fileContent                

                //   sourceFileList
                WriteStartElement("sourceFileList");
                WriteAttributeString("count", "1");
                //     sourceFile
                WriteStartElement("sourceFile");
                WriteAttributeString("id", ParseInput.RawFileName);
                WriteAttributeString("name", ParseInput.RawFileNameWithoutExtension);
                WriteAttributeString("location", ParseInput.RawFilePath);
                SerializeCvParam(new CVParamType
                {
                    accession = "MS:1000768",
                    name = "Thermo nativeID format",
                    cvRef = "MS",
                    value = ""
                });
                SerializeCvParam(new CVParamType
                {
                    accession = "MS:1000563",
                    name = "Thermo RAW format",
                    cvRef = "MS",
                    value = ""
                });
                SerializeCvParam(new CVParamType
                {
                    accession = "MS:1000569",
                    name = "SHA-1",
                    cvRef = "MS",
                    value = CalculateSHAChecksum()
                });
                WriteEndElement(); // sourceFile                
                WriteEndElement(); // sourceFileList               
                WriteEndElement(); // fileDescription                

                var instrumentData = _rawFile.GetInstrumentData();

                // referenceableParamGroupList   
                WriteStartElement("referenceableParamGroupList");
                WriteAttributeString("count", "1");
                //   referenceableParamGroup
                WriteStartElement("referenceableParamGroup");
                WriteAttributeString("id", "commonInstrumentParams");
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

                SerializeCvParam(instrumentModel);
                SerializeCvParam(new CVParamType
                {
                    cvRef = "MS",
                    accession = "MS:1000529",
                    name = "instrument serial number",
                    value = instrumentData.SerialNumber
                });
                WriteEndElement(); // referenceableParamGroup                
                WriteEndElement(); // referenceableParamGroupList                

                // softwareList      
                WriteStartElement("softwareList");
                WriteAttributeString("count", "1");
                //   software
                WriteStartElement("software");
                WriteAttributeString("id", "ThermoRawFileParser");
                WriteAttributeString("version", "1.0.7");
                SerializeCvParam(new CVParamType
                {
                    accession = "MS:1000799",
                    value = "ThermoRawFileParser",
                    name = "custom unreleased software tool",
                    cvRef = "MS"
                });
                WriteEndElement(); // software                
                WriteEndElement(); // softwareList                                                                                

                PopulateInstrumentConfigurationList(firstScanNumber, instrumentModel);

                // dataProcessingList
                WriteStartElement("dataProcessingList");
                WriteAttributeString("count", "1");
                //    dataProcessing
                WriteStartElement("dataProcessing");
                WriteAttributeString("id", "ThermoRawFileParserProcessing");
                //      processingMethod
                WriteStartElement("processingMethod");
                WriteAttributeString("order", "0");
                WriteAttributeString("softwareRef", "ThermoRawFileParser");
                SerializeCvParam(new CVParamType
                {
                    accession = "MS:1000544",
                    cvRef = "MS",
                    name = "Conversion to mzML",
                    value = ""
                });
                WriteEndElement(); // processingMethod                
                WriteEndElement(); // dataProcessing                
                WriteEndElement(); // dataProcessingList                

                // run
                WriteStartElement("run");
                WriteAttributeString("id", ParseInput.RawFileNameWithoutExtension);
                WriteAttributeString("defaultInstrumentConfigurationRef", "IC1");
                WriteAttributeString("startTimeStamp", XmlConvert.ToString(_rawFile.CreationDate));
                //    spectrumList
                WriteStartElement("spectrumList");
                WriteAttributeString("count", _rawFile.RunHeaderEx.SpectraCount.ToString());
                WriteAttributeString("defaultDataProcessingRef", "ThermoRawFileParserProcessing");

                serializer = factory.CreateSerializer(typeof(SpectrumType));

                var index = 0;
                for (var scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
                {
                    var spectrum = ConstructSpectrum(scanNumber);
                    if (spectrum != null)
                    {
                        spectrum.index = index.ToString();
                        if (doIndexing)
                        {
                            // flush the writers before getting the position                
                            _writer.Flush();
                            Writer.Flush();
                            if (spectrumOffSets.Count != 0)
                            {
                                spectrumOffSets.Add(spectrum.id, Writer.BaseStream.Position + 6);
                                //spectrumOffSets.Add(spectrum.id, memoryStream.Position + 6);
                            }
                            else
                            {
                                spectrumOffSets.Add(spectrum.id, Writer.BaseStream.Position + 7);
                                //spectrumOffSets.Add(spectrum.id, memoryStream.Position + 7);
                            }
                        }

                        Serialize(serializer, spectrum);

                        index++;
                    }
                }

                WriteEndElement(); // spectrumList                                                

                index = 0;
                var chromatograms = ConstructChromatograms(firstScanNumber, lastScanNumber);
                if (!chromatograms.IsNullOrEmpty())
                {
                    //    chromatogramList
                    WriteStartElement("chromatogramList");
                    WriteAttributeString("count", chromatograms.Count.ToString());
                    WriteAttributeString("defaultDataProcessingRef", "ThermoRawFileParserProcessing");
                    serializer = factory.CreateSerializer(typeof(ChromatogramType));
                    chromatograms.ForEach(chromatogram =>
                    {
                        chromatogram.index = index.ToString();
                        if (doIndexing)
                        {
                            // flush the writers before getting the posistion
                            _writer.Flush();
                            Writer.Flush();
                            if (chromatogramOffSets.Count != 0)
                            {
                                chromatogramOffSets.Add(chromatogram.id, Writer.BaseStream.Position + 6);
                                //chromatogramOffSets.Add(chromatogram.id, memoryStream.Position + 6);
                            }
                            else
                            {
                                chromatogramOffSets.Add(chromatogram.id, Writer.BaseStream.Position + 7);
                                //chromatogramOffSets.Add(chromatogram.id, memoryStream.Position + 7);
                            }
                        }

                        Serialize(serializer, chromatogram);

                        index++;
                    });

                    WriteEndElement(); // chromatogramList                    
                }

                WriteEndElement(); // run                
                WriteEndElement(); // mzML                

                if (doIndexing)
                {
                    _writer.Flush();
                    Writer.Flush();

                    var indexListPosition = Writer.BaseStream.Position;
                    //var indexListPosition = memoryStream.Position;                

                    //  indexList
                    WriteStartElement("indexList");
                    var indexCount = chromatograms.IsNullOrEmpty() ? 1 : 2;
                    WriteAttributeString("count", indexCount.ToString());
                    //    index
                    WriteStartElement("index");
                    WriteAttributeString("name", "spectrum");
                    IDictionaryEnumerator spectrumOffsetEnumerator = spectrumOffSets.GetEnumerator();
                    while (spectrumOffsetEnumerator.MoveNext())
                    {
                        //      offset
                        WriteStartElement("offset");
                        WriteAttributeString("idRef", spectrumOffsetEnumerator.Key.ToString());
                        WriteString(spectrumOffsetEnumerator.Value.ToString());
                        WriteEndElement(); // offset                    
                    }

                    WriteEndElement(); // index                

                    if (!chromatograms.IsNullOrEmpty())
                    {
                        //    index
                        WriteStartElement("index");
                        WriteAttributeString("name", "chromatogram");
                        IDictionaryEnumerator chromatogramOffsetEnumerator = chromatogramOffSets.GetEnumerator();
                        while (chromatogramOffsetEnumerator.MoveNext())
                        {
                            //      offset
                            WriteStartElement("offset");
                            WriteAttributeString("idRef", chromatogramOffsetEnumerator.Key.ToString());
                            WriteString(chromatogramOffsetEnumerator.Value.ToString());
                            WriteEndElement(); // offset                        
                        }

                        WriteEndElement(); // index                    
                    }

                    WriteEndElement(); // indexList                                                

                    //  indexListOffset
                    WriteStartElement("indexListOffset");
                    WriteString(indexListPosition.ToString());
                    WriteEndElement(); // indexListOffset                                                

                    //  fileChecksum
                    WriteStartElement("fileChecksum");
                    WriteString("");

                    _writer.Flush();
                    Writer.Flush();

                    // Write data here
                    cryptoStream.FlushFinalBlock();
                    var hash = sha1.Hash;

                    _writer.WriteValue(BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
                    _writer.WriteEndElement(); // fileChecksum

                    WriteEndElement(); // indexedmzML   
                }

                WriteEndDocument();
            }
            finally
            {
                _writer.Flush();
                _writer.Close();

                Writer.Flush();
                Writer.Close();
            }
        }

        /// <summary>
        /// Populate the instrument configuration list
        /// </summary>
        /// <param name="firstScanNumber"></param>
        /// <param name="instrumentModel"></param>
        private void PopulateInstrumentConfigurationList(int firstScanNumber, CVParamType instrumentModel)
        {
            // go over the first scans until an MS2 scan is encountered
            // to collect all mass analyzer and ionization types
            var encounteredMs2 = false;
            var scanNumber = firstScanNumber;
            do
            {
                // Get the scan filter for this scan number
                var scanFilter = _rawFile.GetFilterForScanNumber(scanNumber);

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

                if (scanFilter.MSOrder == MSOrderType.Ms2)
                {
                    encounteredMs2 = true;
                }

                scanNumber++;
            } while (!encounteredMs2);

            // Add a default analyzer if none were found
            if (_massAnalyzers.Count == 0)
            {
                _massAnalyzers.Add(MassAnalyzerType.Any, "IC1");
            }

            // instrumentConfigurationList
            WriteStartElement("instrumentConfigurationList");
            WriteAttributeString("count", _massAnalyzers.Count.ToString());

            // Make a new instrument configuration for each analyzer
            var massAnalyzerIndex = 0;
            foreach (var massAnalyzer in _massAnalyzers)
            {
                //    instrumentConfiguration
                WriteStartElement("instrumentConfiguration");
                WriteAttributeString("id", massAnalyzer.Value);
                //      referenceableParamGroupRef
                WriteStartElement("referenceableParamGroupRef");
                WriteAttributeString("ref", "commonInstrumentParams");
                WriteEndElement(); // referenceableParamGroupRef
                //        componentList
                WriteStartElement("componentList");
                WriteAttributeString("count", "3");

                //          source
                WriteStartElement("source");
                WriteAttributeString("order", "1");
                if (_ionizationTypes.IsNullOrEmpty())
                {
                    _ionizationTypes.Add(IonizationModeType.Any,
                        OntologyMapping.IonizationTypes[IonizationModeType.Any]);
                }

                var index = 0;
                // Ionization type
                foreach (var ionizationType in _ionizationTypes)
                {
                    SerializeCvParam(ionizationType.Value);
                    index++;
                }

                WriteEndElement(); // source

                //          analyzer             
                WriteStartElement("analyzer");
                WriteAttributeString("order", (index + 1).ToString());
                SerializeCvParam(OntologyMapping.MassAnalyzerTypes[massAnalyzer.Key]);
                index++;
                WriteEndElement(); // analyzer

                //          detector
                WriteStartElement("detector");
                WriteAttributeString("order", (index + 1).ToString());

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

                SerializeCvParam(detectorCvParam);
                WriteEndElement(); // detector
                WriteEndElement(); // componentList
                WriteEndElement(); // instrumentConfiguration

                massAnalyzerIndex++;
            }

            WriteEndElement(); // instrumentConfigurationList
        }

        void WriteStartDocument()
        {
            _writer.WriteStartDocument();
            //_memoryWriter.WriteStartDocument();
        }

        void WriteEndDocument()
        {
            _writer.WriteEndDocument();
            //_memoryWriter.WriteEndDocument();
        }

        void WriteStartElement(string elementName)
        {
            _writer.WriteStartElement(elementName);
            //_memoryWriter.WriteStartElement(elementName);
        }

        void WriteEndElement()
        {
            _writer.WriteEndElement();
            //_memoryWriter.WriteEndElement();
        }

        void WriteStartElementWithNamespace(string elementName)
        {
            _writer.WriteStartElement(elementName, Ns);
            //_memoryWriter.WriteStartElement(elementName, Ns);
        }

        void WriteAttributeString(string prefix, string localName, string value)
        {
            _writer.WriteAttributeString(prefix, localName, null, value);
            //_memoryWriter.WriteAttributeString(prefix, localName, null, value);
        }

        void WriteAttributeString(string localName, string value)
        {
            _writer.WriteAttributeString(localName, value);
            //_memoryWriter.WriteAttributeString(localName, value);
        }

        void SerializeCvParam(CVParamType cvParam)
        {
            cvParamSerializer.Serialize(_writer, cvParam, mzMlNamespace);
            //cvParamSerializer.Serialize(_memoryWriter, cvParam, mzMlNamespace);
        }

        void WriteString(String value)
        {
            _writer.WriteString(value);
            //_memoryWriter.WriteString(value);
        }

        void Serialize<T>(XmlSerializer serializer, T t)
        {
            serializer.Serialize(_writer, t, mzMlNamespace);
            //serializer.Serialize(_memoryWriter, t, mzMlNamespace);
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
            float? monoisotopicMass = null;
            float? ionInjectionTime = null;
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
                    monoisotopicMass = float.Parse(trailerData.Values[i]);
                }

                if (trailerData.Labels[i] == "Ion Injection Time (ms):")
                {
                    ionInjectionTime = float.Parse(trailerData.Values[i]);
                }
            }

            // Construct and set the scan list element of the spectrum
            var scanListType = ConstructScanList(scanNumber, scan, scanFilter, scanEvent, monoisotopicMass,
                ionInjectionTime);
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

                    // Keep track of scan number for precursor reference
                    _precursorScanNumber = scanNumber;

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
                    var precursorListType = ConstructPrecursorList(scanEvent, charge);
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

            // Base peak intensity
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
        /// <param name="scanEvent">the scan event</param>
        /// <param name="charge">the charge</param>
        /// <returns>the precursor list</returns>
        private PrecursorListType ConstructPrecursorList(IScanEventBase scanEvent, int? charge)
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

            precursor.spectrumRef = ConstructSpectrumTitle(_precursorScanNumber);

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

//            var precursorIntensity = GetPrecursorIntensity(_rawFile, _precursorScanNumber, precursorMass,
//                _rawFile.RetentionTimeFromScanNumber(scanNumber), isolationWidth);
//            if (precursorIntensity != null)
//            {
//                ionCvParams.Add(new CVParamType
//                {
//                    name = "peak intensity",
//                    value = precursorIntensity.ToString(),
//                    accession = "MS:1000042",
//                    cvRef = "MS",
//                    unitCvRef = "MS",
//                    unitAccession = "MS:1000131",
//                    unitName = "number of detector counts"
//                });
//            }

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

            // Activation            
            var activationCvParams = new List<CVParamType>();
            if (reaction != null)
            {
                if (reaction.CollisionEnergyValid)
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
            }

            // Check for supplemental activation
            if (scanEvent.SupplementalActivation == TriState.On)
            {
                try
                {
                    reaction = scanEvent.GetReaction(1);

                    if (reaction != null)
                    {
                        if (reaction.CollisionEnergyValid)
                        {
                            activationCvParams.Add(
                                new CVParamType
                                {
                                    accession = "MS:1002680",
                                    name = "supplemental collision energy",
                                    cvRef = "MS",
                                    value = reaction.CollisionEnergy.ToString(CultureInfo.InvariantCulture),
                                    unitCvRef = "UO",
                                    unitAccession = "UO:0000266",
                                    unitName = "electronvolt"
                                });
                        }

                        // Add this supplemental CV term
                        // TODO: use a more generic approach
                        if (reaction.ActivationType == ActivationType.HigherEnergyCollisionalDissociation)
                        {
                            activationCvParams.Add(new CVParamType
                            {
                                accession = "MS:1002678",
                                name = "supplemental beam-type collision-induced dissociation",
                                cvRef = "MS",
                                value = ""
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
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Do nothing
                }
            }

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
        /// <param name="ionInjectionTime">the ion injection time</param>
        /// <returns></returns>
        private ScanListType ConstructScanList(int scanNumber, Scan scan, IScanFilter scanFilter, IScanEvent scanEvent,
            float? monoisotopicMass, float? ionInjectionTime)
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

            var scanTypeCvParams = new List<CVParamType>();

            // Scan start time
            scanTypeCvParams.Add(new CVParamType
            {
                name = "scan start time",
                accession = "MS:1000016",
                value = _rawFile.RetentionTimeFromScanNumber(scanNumber).ToString(CultureInfo.InvariantCulture),
                unitCvRef = "UO",
                unitAccession = "UO:0000031",
                unitName = "minute",
                cvRef = "MS"
            });

            // Scan filter string
            scanTypeCvParams.Add(new CVParamType
            {
                name = "filter string",
                accession = "MS:1000512",
                value = scanEvent.ToString(),
                cvRef = "MS"
            });

            // Ion injection time
            if (ionInjectionTime.HasValue)
            {
                scanTypeCvParams.Add(new CVParamType
                {
                    name = "ion injection time",
                    cvRef = "MS",
                    accession = "MS:1000927",
                    value = ionInjectionTime.ToString(),
                    unitCvRef = "UO",
                    unitAccession = "UO:0000028",
                    unitName = "millisecond"
                });
            }

            var scanType = new ScanType
            {
                instrumentConfigurationRef = instrumentConfigurationRef,
                cvParam = scanTypeCvParams.ToArray()
            };

            // Monoisotopic mass
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