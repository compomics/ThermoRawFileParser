using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using log4net;
using NUnit.Framework;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Util;
using ThermoRawFileParser.Writer.MzML;
using zlib;

namespace ThermoRawFileParser.Writer
{
    public class MzMlSpectrumWriter : SpectrumWriter
    {
        private static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string FilterStringIsolationMzPattern = @"ms2 (.*?)@";

        private IRawDataPlus _rawFile;

        // Dictionary to keep track of the different mass analyzers (key: Thermo MassAnalyzerType; value: the reference string)       
        private readonly Dictionary<MassAnalyzerType, string> _massAnalyzers =
            new Dictionary<MassAnalyzerType, string>();

        // Dictionary to keep track of the different ionization modes (key: Thermo IonizationModeType; value: the reference string)
        private readonly Dictionary<IonizationModeType, CVParamType> _ionizationTypes =
            new Dictionary<IonizationModeType, CVParamType>();

        // Precursor scan number for reference in the precursor element of an MS2 spectrum
        private int _precursorMs1ScanNumber;

        // Precursor scan number (value) and isolation m/z (key) for reference in the precursor element of an MS3 spectrum
        private readonly LimitedSizeDictionary<string, int> _precursorMs2ScanNumbers =
            new LimitedSizeDictionary<string, int>(40);

        private const string SourceFileId = "RAW1";
        private readonly XmlSerializerFactory _factory = new XmlSerializerFactory();
        private const string Ns = "http://psi.hupo.org/ms/mzml";
        private readonly XmlSerializer _cvParamSerializer;
        private readonly XmlSerializerNamespaces _mzMlNamespace;
        private readonly bool _doIndexing;
        private readonly int _osOffset;

        private XmlWriter _writer;

        public MzMlSpectrumWriter(ParseInput parseInput) : base(parseInput)
        {
            _cvParamSerializer = _factory.CreateSerializer(typeof(CVParamType));
            _mzMlNamespace = new XmlSerializerNamespaces();
            _mzMlNamespace.Add(string.Empty, "http://psi.hupo.org/ms/mzml");
            _doIndexing = ParseInput.OutputFormat == OutputFormat.IndexMzML;
            _osOffset = System.Environment.NewLine == "\n" ? 0 : 1;
        }

        /// <inheritdoc />
        public override void Write(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            _rawFile = rawFile;

            var spectrumOffSets = new OrderedDictionary();
            var chromatogramOffSets = new OrderedDictionary();

            ConfigureWriter(".mzML");

            XmlSerializer serializer;
            var settings = new XmlWriterSettings {Indent = true, Encoding = Encoding.UTF8};
            var sha1 = SHA1.Create();
            CryptoStream cryptoStream = null;
            if (_doIndexing)
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
                _writer.WriteStartDocument();

                if (_doIndexing)
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
                _writer.WriteAttributeString("version", "1.1.0");
                _writer.WriteAttributeString("id", ParseInput.RawFileNameWithoutExtension);

                // CV list
                serializer = _factory.CreateSerializer(typeof(CVType));
                _writer.WriteStartElement("cvList");
                _writer.WriteAttributeString("count", "2");
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
                _writer.WriteEndElement(); // cvList                

                // fileDescription
                _writer.WriteStartElement("fileDescription");
                //   fileContent
                _writer.WriteStartElement("fileContent");
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
                _writer.WriteEndElement(); // fileContent                

                //   sourceFileList
                _writer.WriteStartElement("sourceFileList");
                _writer.WriteAttributeString("count", "1");
                //     sourceFile
                _writer.WriteStartElement("sourceFile");
                _writer.WriteAttributeString("id", SourceFileId);
                _writer.WriteAttributeString("name", ParseInput.RawFileNameWithoutExtension);
                _writer.WriteAttributeString("location", ParseInput.RawFilePath);
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
                _writer.WriteEndElement(); // sourceFile                
                _writer.WriteEndElement(); // sourceFileList               
                _writer.WriteEndElement(); // fileDescription                

                var instrumentData = _rawFile.GetInstrumentData();

                // referenceableParamGroupList   
                _writer.WriteStartElement("referenceableParamGroupList");
                _writer.WriteAttributeString("count", "1");
                //   referenceableParamGroup
                _writer.WriteStartElement("referenceableParamGroup");
                _writer.WriteAttributeString("id", "commonInstrumentParams");
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
                _writer.WriteEndElement(); // referenceableParamGroup                
                _writer.WriteEndElement(); // referenceableParamGroupList                

                // softwareList      
                _writer.WriteStartElement("softwareList");
                _writer.WriteAttributeString("count", "1");
                //   software
                _writer.WriteStartElement("software");
                _writer.WriteAttributeString("id", "ThermoRawFileParser");
                _writer.WriteAttributeString("version", "1.1.5");
                SerializeCvParam(new CVParamType
                {
                    accession = "MS:1000799",
                    value = "ThermoRawFileParser",
                    name = "custom unreleased software tool",
                    cvRef = "MS"
                });
                _writer.WriteEndElement(); // software                
                _writer.WriteEndElement(); // softwareList                                                                                

                PopulateInstrumentConfigurationList(firstScanNumber, lastScanNumber, instrumentModel);

                // dataProcessingList
                _writer.WriteStartElement("dataProcessingList");
                _writer.WriteAttributeString("count", "1");
                //    dataProcessing
                _writer.WriteStartElement("dataProcessing");
                _writer.WriteAttributeString("id", "ThermoRawFileParserProcessing");
                //      processingMethod
                _writer.WriteStartElement("processingMethod");
                _writer.WriteAttributeString("order", "0");
                _writer.WriteAttributeString("softwareRef", "ThermoRawFileParser");
                SerializeCvParam(new CVParamType
                {
                    accession = "MS:1000544",
                    cvRef = "MS",
                    name = "Conversion to mzML",
                    value = ""
                });
                _writer.WriteEndElement(); // processingMethod                
                _writer.WriteEndElement(); // dataProcessing                
                _writer.WriteEndElement(); // dataProcessingList                

                // run
                _writer.WriteStartElement("run");
                _writer.WriteAttributeString("id", ParseInput.RawFileNameWithoutExtension);
                _writer.WriteAttributeString("defaultInstrumentConfigurationRef", "IC1");
                _writer.WriteAttributeString("startTimeStamp",
                    XmlConvert.ToString(_rawFile.CreationDate, XmlDateTimeSerializationMode.Utc));
                _writer.WriteAttributeString("defaultSourceFileRef", SourceFileId);
                //    spectrumList
                _writer.WriteStartElement("spectrumList");
                _writer.WriteAttributeString("count", _rawFile.RunHeaderEx.SpectraCount.ToString());
                _writer.WriteAttributeString("defaultDataProcessingRef", "ThermoRawFileParserProcessing");

                serializer = _factory.CreateSerializer(typeof(SpectrumType));

                var index = 0;
                for (var scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
                {
                    var spectrum = ConstructSpectrum(scanNumber);
                    if (spectrum != null)
                    {
                        spectrum.index = index.ToString();
                        if (_doIndexing)
                        {
                            // flush the writers before getting the position                
                            _writer.Flush();
                            Writer.Flush();
                            if (spectrumOffSets.Count != 0)
                            {
                                spectrumOffSets.Add(spectrum.id, Writer.BaseStream.Position + 6 + _osOffset);
                            }
                            else
                            {
                                spectrumOffSets.Add(spectrum.id, Writer.BaseStream.Position + 7 + _osOffset);
                            }
                        }

                        Serialize(serializer, spectrum);

                        Log.Debug("Spectrum Added to List of Spectra -- ID " + spectrum.id);

                        index++;
                    }
                }

                _writer.WriteEndElement(); // spectrumList                                                

                index = 0;
                var chromatograms = ConstructChromatograms(firstScanNumber, lastScanNumber);
                if (!chromatograms.IsNullOrEmpty())
                {
                    //    chromatogramList
                    _writer.WriteStartElement("chromatogramList");
                    _writer.WriteAttributeString("count", chromatograms.Count.ToString());
                    _writer.WriteAttributeString("defaultDataProcessingRef", "ThermoRawFileParserProcessing");
                    serializer = _factory.CreateSerializer(typeof(ChromatogramType));
                    chromatograms.ForEach(chromatogram =>
                    {
                        chromatogram.index = index.ToString();
                        if (_doIndexing)
                        {
                            // flush the writers before getting the posistion
                            _writer.Flush();
                            Writer.Flush();
                            if (chromatogramOffSets.Count != 0)
                            {
                                chromatogramOffSets.Add(chromatogram.id,
                                    Writer.BaseStream.Position + 6 + _osOffset);
                            }
                            else
                            {
                                chromatogramOffSets.Add(chromatogram.id,
                                    Writer.BaseStream.Position + 7 + _osOffset);
                            }
                        }

                        Serialize(serializer, chromatogram);

                        index++;
                    });

                    _writer.WriteEndElement(); // chromatogramList                    
                }

                _writer.WriteEndElement(); // run                
                _writer.WriteEndElement(); // mzML                

                if (_doIndexing)
                {
                    _writer.Flush();
                    Writer.Flush();

                    var indexListPosition = Writer.BaseStream.Position + _osOffset;

                    //  indexList
                    _writer.WriteStartElement("indexList");
                    var indexCount = chromatograms.IsNullOrEmpty() ? 1 : 2;
                    _writer.WriteAttributeString("count", indexCount.ToString());
                    //    index
                    _writer.WriteStartElement("index");
                    _writer.WriteAttributeString("name", "spectrum");
                    var spectrumOffsetEnumerator = spectrumOffSets.GetEnumerator();
                    while (spectrumOffsetEnumerator.MoveNext())
                    {
                        //      offset
                        _writer.WriteStartElement("offset");
                        _writer.WriteAttributeString("idRef", spectrumOffsetEnumerator.Key.ToString());
                        _writer.WriteString(spectrumOffsetEnumerator.Value.ToString());
                        _writer.WriteEndElement(); // offset                    
                    }

                    _writer.WriteEndElement(); // index                

                    if (!chromatograms.IsNullOrEmpty())
                    {
                        //    index
                        _writer.WriteStartElement("index");
                        _writer.WriteAttributeString("name", "chromatogram");
                        var chromatogramOffsetEnumerator = chromatogramOffSets.GetEnumerator();
                        while (chromatogramOffsetEnumerator.MoveNext())
                        {
                            //      offset
                            _writer.WriteStartElement("offset");
                            _writer.WriteAttributeString("idRef", chromatogramOffsetEnumerator.Key.ToString());
                            _writer.WriteString(chromatogramOffsetEnumerator.Value.ToString());
                            _writer.WriteEndElement(); // offset                        
                        }

                        _writer.WriteEndElement(); // index                    
                    }

                    _writer.WriteEndElement(); // indexList                                                

                    //  indexListOffset
                    _writer.WriteStartElement("indexListOffset");
                    _writer.WriteString(indexListPosition.ToString());
                    _writer.WriteEndElement(); // indexListOffset                                                

                    //  fileChecksum
                    _writer.WriteStartElement("fileChecksum");
                    _writer.WriteString("");

                    _writer.Flush();
                    Writer.Flush();

                    // Write data here
                    cryptoStream.FlushFinalBlock();
                    var hash = sha1.Hash;

                    // do this for avoiding the "Hash must be finalized before the hash value is retrieved"
                    // error on Windows 
                    sha1.Initialize();

                    _writer.WriteValue(BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant());
                    _writer.WriteEndElement(); // fileChecksum

                    _writer.WriteEndElement(); // indexedmzML                                           
                }

                _writer.WriteEndDocument();
            }
            finally
            {
                _writer.Flush();
                _writer.Close();

                Writer.Flush();
                Writer.Close();

                if (_doIndexing)
                {
                    cryptoStream.Flush();
                    cryptoStream.Close();
                }
            }

            // in case of indexed mzML, change the extension from xml to mzML and check for the gzip option
            if (_doIndexing && ParseInput.Gzip)
            {
                var mzMLFile = new FileInfo(ParseInput.OutputDirectory + "//" +
                                            ParseInput.RawFileNameWithoutExtension + ".mzML");
                var gzipMzMLFile = new FileInfo(string.Concat(mzMLFile.FullName, ".gz"));
                using (var fileToBeZippedAsStream = mzMLFile.OpenRead())
                using (var gzipTargetAsStream = gzipMzMLFile.Create())
                using (var gzipStream = new GZipStream(gzipTargetAsStream, CompressionMode.Compress))
                {
                    try
                    {
                        fileToBeZippedAsStream.CopyTo(gzipStream);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }

                // remove the unzipped mzML file
                mzMLFile.Delete();
            }
        }

        /// <summary>
        /// Populate the instrument configuration list
        /// </summary>
        /// <param name="firstScanNumber"></param>
        /// <param name="lastScanNumber"></param>
        /// <param name="instrumentModel"></param>
        private void PopulateInstrumentConfigurationList(int firstScanNumber, int lastScanNumber,
            CVParamType instrumentModel)
        {
            // go over the first scans until an MS2 scan is encountered
            // to collect all mass analyzer and ionization types
            var encounteredMs2 = false;
            var scanNumber = firstScanNumber;
            do
            {
                // Get the scan filter for this scan number

                try
                {
                    var scanFilter = _rawFile.GetFilterForScanNumber(scanNumber);

                    // Add the ionization type if necessary
                    try
                    {
                        if (!_ionizationTypes.ContainsKey(scanFilter.IonizationMode))
                        {
                            _ionizationTypes.Add(scanFilter.IonizationMode,
                                OntologyMapping.IonizationTypes[scanFilter.IonizationMode]);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warn("The IonizationMode does not contains the following property --" + e.Message);
                        if (!ParseInput.IgnoreInstrumentErrors)
                        {
                            throw e;
                        }
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
                }
                catch (Exception e)
                {
                    Log.Warn("No Scan Filter found for the following scan --" + scanNumber);
                    if (!ParseInput.IgnoreInstrumentErrors)
                    {
                        throw e;
                    }
                }

                scanNumber++;
            } while (!encounteredMs2 && scanNumber <= lastScanNumber);

            // Add a default analyzer if none were found
            if (_massAnalyzers.Count == 0)
            {
                _massAnalyzers.Add(MassAnalyzerType.Any, "IC1");
            }

            // instrumentConfigurationList
            _writer.WriteStartElement("instrumentConfigurationList");
            _writer.WriteAttributeString("count", _massAnalyzers.Count.ToString());

            // Make a new instrument configuration for each analyzer
            var massAnalyzerIndex = 0;
            foreach (var massAnalyzer in _massAnalyzers)
            {
                //    instrumentConfiguration
                _writer.WriteStartElement("instrumentConfiguration");
                _writer.WriteAttributeString("id", massAnalyzer.Value);
                //      referenceableParamGroupRef
                _writer.WriteStartElement("referenceableParamGroupRef");
                _writer.WriteAttributeString("ref", "commonInstrumentParams");
                _writer.WriteEndElement(); // referenceableParamGroupRef
                //        componentList
                _writer.WriteStartElement("componentList");
                _writer.WriteAttributeString("count", "3");

                //          source
                _writer.WriteStartElement("source");
                _writer.WriteAttributeString("order", "1");
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

                _writer.WriteEndElement(); // source

                //          analyzer             
                _writer.WriteStartElement("analyzer");
                _writer.WriteAttributeString("order", (index + 1).ToString());
                SerializeCvParam(OntologyMapping.MassAnalyzerTypes[massAnalyzer.Key]);
                index++;
                _writer.WriteEndElement(); // analyzer

                //          detector
                _writer.WriteStartElement("detector");
                _writer.WriteAttributeString("order", (index + 1).ToString());

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
                _writer.WriteEndElement(); // detector
                _writer.WriteEndElement(); // componentList
                _writer.WriteEndElement(); // instrumentConfiguration

                massAnalyzerIndex++;
            }

            _writer.WriteEndElement(); // instrumentConfigurationList
        }

        private void WriteStartElementWithNamespace(string elementName)
        {
            _writer.WriteStartElement(elementName, Ns);
        }

        private void WriteAttributeString(string prefix, string localName, string value)
        {
            _writer.WriteAttributeString(prefix, localName, null, value);
        }

        private void SerializeCvParam(CVParamType cvParam)
        {
            _cvParamSerializer.Serialize(_writer, cvParam, _mzMlNamespace);
        }

        private void Serialize<T>(XmlSerializer serializer, T t)
        {
            serializer.Serialize(_writer, t, _mzMlNamespace);
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
            double? monoisotopicMass = null;
            double? ionInjectionTime = null;
            double? isolationWidth = null;
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
                    monoisotopicMass = double.Parse(trailerData.Values[i], NumberStyles.Any,
                        CultureInfo.CurrentCulture);
                }

                if (trailerData.Labels[i] == "Ion Injection Time (ms):")
                {
                    ionInjectionTime = double.Parse(trailerData.Values[i], NumberStyles.Any,
                        CultureInfo.CurrentCulture);
                }

                if (trailerData.Labels[i] == "MS" + (int) scanFilter.MSOrder + " Isolation Width:")
                {
                    isolationWidth = double.Parse(trailerData.Values[i], NumberStyles.Any,
                        CultureInfo.CurrentCulture);
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
                    _precursorMs1ScanNumber = scanNumber;

                    break;
                case MSOrderType.Ms2:
                    spectrumCvParams.Add(new CVParamType
                    {
                        accession = "MS:1000580",
                        cvRef = "MS",
                        name = "MSn spectrum",
                        value = ""
                    });

                    // Keep track of scan number and isolation m/z for precursor reference                   
                    var result = Regex.Match(scanEvent.ToString(), FilterStringIsolationMzPattern);
                    if (result.Success)
                    {
                        if (!_precursorMs2ScanNumbers.ContainsKey(result.Groups[1].Value))
                        {
                            _precursorMs2ScanNumbers.Add(result.Groups[1].Value, scanNumber);
                        }
                        else
                        {
                            // update the existing value
                            _precursorMs2ScanNumbers[result.Groups[1].Value] = scanNumber;
                        }
                    }

                    // Construct and set the precursor list element of the spectrum                    
                    var precursorListType =
                        ConstructPrecursorList(scanEvent, charge, scanFilter.MSOrder, isolationWidth);
                    spectrum.precursorList = precursorListType;
                    break;
                case MSOrderType.Ms3:
                    spectrumCvParams.Add(new CVParamType
                    {
                        accession = "MS:1000580",
                        cvRef = "MS",
                        name = "MSn spectrum",
                        value = ""
                    });
                    precursorListType = ConstructPrecursorList(scanEvent, charge, scanFilter.MSOrder, isolationWidth);
                    spectrum.precursorList = precursorListType;
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

            // Scan type, centroid or profile
            switch (scanEvent.ScanData)
            {
                case ScanDataType.Centroid:
                    spectrumCvParams.Add(new CVParamType
                    {
                        accession = "MS:1000127",
                        cvRef = "MS",
                        name = "centroid spectrum",
                        value = ""
                    });
                    break;
                case ScanDataType.Profile:
                    spectrumCvParams.Add(new CVParamType
                    {
                        accession = "MS:1000128",
                        cvRef = "MS",
                        name = "profile spectrum",
                        value = ""
                    });
                    break;
            }

            double? basePeakMass = null;
            double? basePeakIntensity = null;
            double? lowestObservedMz = null;
            double? highestObservedMz = null;
            double[] masses = null;
            double[] intensities = null;
            if (scan.HasCentroidStream && (scanEvent.ScanData == ScanDataType.Centroid ||
                                           (scanEvent.ScanData == ScanDataType.Profile &&
                                            !ParseInput.NoPeakPicking)))
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
                }
            }

            // Base peak m/z
            if (basePeakMass != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "base peak m/z",
                    accession = "MS:1000504",
                    value = basePeakMass.Value.ToString(CultureInfo.InvariantCulture),
                    unitCvRef = "MS",
                    unitName = "m/z",
                    unitAccession = "MS:1000040",
                    cvRef = "MS"
                });
            }

            // Base peak intensity
            if (basePeakIntensity != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "base peak intensity",
                    accession = "MS:1000505",
                    value = basePeakIntensity.Value.ToString(CultureInfo.InvariantCulture),
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
                    value = lowestObservedMz.Value.ToString(CultureInfo.InvariantCulture),
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
                    value = highestObservedMz.Value.ToString(CultureInfo.InvariantCulture),
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
        /// <param name="msLevel">the MS level</param>
        /// <param name="isolationWidth">the isolation width</param>
        /// <returns>the precursor list</returns>
        private PrecursorListType ConstructPrecursorList(IScanEventBase scanEvent, int? charge, MSOrderType msLevel,
            double? isolationWidth)
        {
            // Construct the precursor
            var precursorList = new PrecursorListType
            {
                count = "1",
                precursor = new PrecursorType[1]
            };

            var spectrumRef = "";
            switch (msLevel)
            {
                case MSOrderType.Ms2:
                    spectrumRef = ConstructSpectrumTitle(_precursorMs1ScanNumber);
                    break;
                case MSOrderType.Ms3:
                    var precursorMs2ScanNumber =
                        _precursorMs2ScanNumbers.Keys.FirstOrDefault(isolationMz =>
                            scanEvent.ToString().Contains(isolationMz));
                    if (!precursorMs2ScanNumber.IsNullOrEmpty())
                    {
                        spectrumRef = ConstructSpectrumTitle(_precursorMs2ScanNumbers[precursorMs2ScanNumber]);
                    }
                    else
                    {
                        throw new InvalidOperationException("Couldn't find a MS2 precursor scan for MS3 scan " +
                                                            scanEvent.ToString());
                    }

                    break;
            }

            var precursor = new PrecursorType
            {
                selectedIonList =
                    new SelectedIonListType {count = 1.ToString(), selectedIon = new ParamGroupType[1]},
                spectrumRef = spectrumRef
            };

            precursor.selectedIonList.selectedIon[0] =
                new ParamGroupType
                {
                    cvParam = new CVParamType[3]
                };

            IReaction reaction = null;
            var precursorMass = 0.0;
            try
            {
                switch (msLevel)
                {
                    case MSOrderType.Ms2:
                        reaction = scanEvent.GetReaction(0);
                        break;
                    case MSOrderType.Ms3:
                        reaction = scanEvent.GetReaction(1);
                        break;
                }

                precursorMass = reaction.PrecursorMass;

                // take isolation width from the reaction if no value was found in the trailer data               
                if (isolationWidth == null || isolationWidth < ZeroDelta)
                {
                    isolationWidth = reaction.IsolationWidth;
                }
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
            double? monoisotopicMass, double? ionInjectionTime)
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

            var scanTypeCvParams = new List<CVParamType>
            {
                new CVParamType
                {
                    name = "scan start time",
                    accession = "MS:1000016",
                    value = _rawFile.RetentionTimeFromScanNumber(scanNumber)
                        .ToString(CultureInfo.InvariantCulture),
                    unitCvRef = "UO",
                    unitAccession = "UO:0000031",
                    unitName = "minute",
                    cvRef = "MS"
                },
                new CVParamType
                {
                    name = "filter string", accession = "MS:1000512", value = scanEvent.ToString(), cvRef = "MS"
                }
            };

            // Scan start time

            // Scan filter string

            // Ion injection time
            if (ionInjectionTime.HasValue)
            {
                scanTypeCvParams.Add(new CVParamType
                {
                    name = "ion injection time",
                    cvRef = "MS",
                    accession = "MS:1000927",
                    value = ionInjectionTime.Value.ToString(CultureInfo.InvariantCulture),
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