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

        private readonly Regex _filterStringIsolationMzPattern = new Regex(@"ms\d+ (.+?) \[");

        // Tune version < 3 produces multiple trailer entry like "SPS Mass [number]"
        private readonly Regex _spSentry = new Regex(@"SPS Mass\s+\d+:");

        // Tune version == 3 produces trailer entry "SPS Masses/Continued"
        private readonly Regex _spSentry3 = new Regex(@"SPS Masses(?:\s+Continued)?:");

        private IRawDataPlus _rawFile;

        // Dictionary to keep track of the different mass analyzers (key: Thermo MassAnalyzerType; value: the reference string)       
        private readonly Dictionary<MassAnalyzerType, string> _massAnalyzers =
            new Dictionary<MassAnalyzerType, string>();

        // Dictionary to keep track of the different ionization modes (key: Thermo IonizationModeType; value: the reference string)
        private readonly Dictionary<IonizationModeType, CVParamType> _ionizationTypes =
            new Dictionary<IonizationModeType, CVParamType>();

        // Precursor scan number (value) and isolation m/z (key) for reference in the precursor element of an MSn spectrum
        private readonly Dictionary<string, int> _precursorScanNumbers = new Dictionary<string, int>();

        //Precursor information for scans
        private Dictionary<int, PrecursorInfo> _precursorTree = new Dictionary<int, PrecursorInfo>();

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
            _osOffset = Environment.NewLine == "\n" ? 0 : 1;
        }

        /// <inheritdoc />
        public override void Write(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            _rawFile = rawFile;

            var spectrumOffSets = new OrderedDictionary();
            var chromatogramOffSets = new OrderedDictionary();

            ConfigureWriter(".mzML");

            XmlSerializer serializer;
            var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding() };
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
                    // IndexedmzML
                    WriteStartElementWithNamespace("indexedmzML");
                    WriteAttributeString("xmlns", "xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    WriteAttributeString("xsi", "schemaLocation",
                        "http://psi.hupo.org/ms/mzml http://psidev.info/files/ms/mzML/xsd/mzML1.1.2_idx.xsd");
                }

                // MzML
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
                    version = "4.1.79"
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

                // FileDescription
                _writer.WriteStartElement("fileDescription");
                //   fileContent
                _writer.WriteStartElement("fileContent");
                // MS1
                if (rawFile.SelectMsData())
                {
                    SerializeCvParam(new CVParamType
                    {
                        accession = "MS:1000579",
                        name = "MS1 spectrum",
                        cvRef = "MS",
                        value = ""
                    });
                    // MSn
                    SerializeCvParam(new CVParamType
                    {
                        accession = "MS:1000580",
                        name = "MSn spectrum",
                        cvRef = "MS",
                        value = ""
                    });
                    // Ion current chromatogram
                    SerializeCvParam(new CVParamType
                    {
                        accession = "MS:1000810",
                        name = "ion current chromatogram",
                        cvRef = "MS",
                        value = ""
                    });
                }

                // Other detector data
                if (ParseInput.AllDetectors)
                {
                    // PDA spectrum
                    if (_rawFile.GetInstrumentCountOfType(Device.Pda) > 0)
                    {
                        SerializeCvParam(new CVParamType
                        {
                            accession = "MS:1000806",
                            name = "absorption spectrum",
                            cvRef = "MS",
                            value = ""
                        });
                    }

                    // Absorption chromatogram
                    if (_rawFile.GetInstrumentCountOfType(Device.Pda) > 0 ||
                        _rawFile.GetInstrumentCountOfType(Device.UV) > 0)
                    {
                        SerializeCvParam(new CVParamType
                        {
                            accession = "MS:1000812",
                            name = "absorption chromatogram",
                            cvRef = "MS",
                            value = ""
                        });
                    }

                    // Pressure chromatogram
                    if (_rawFile.GetInstrumentCountOfType(Device.Analog) > 0)
                    {
                        SerializeCvParam(new CVParamType
                        {
                            accession = "MS:1003019",
                            name = "pressure chromatogram",
                            cvRef = "MS",
                            value = ""
                        });
                    }

                    // MSAnalog chromatogram
                    if (_rawFile.GetInstrumentCountOfType(Device.MSAnalog) > 0)
                    {
                        // TODO Write MSAnalog fileContent
                        // e.g. Front FID or Back FID
                    }
                }

                _writer.WriteEndElement(); // fileContent                

                // SourceFileList
                _writer.WriteStartElement("sourceFileList");
                _writer.WriteAttributeString("count", "1");
                // SourceFile
                _writer.WriteStartElement("sourceFile");
                _writer.WriteAttributeString("id", SourceFileId);
                _writer.WriteAttributeString("name", ParseInput.RawFileNameWithoutExtension);
                _writer.WriteAttributeString("location", "file:///" + ParseInput.RawFilePath);
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

                if (_rawFile.SelectedInstrument.DeviceType != Device.None && _rawFile.SelectedInstrument.InstrumentIndex != -1)
                {
                    var instrumentData = _rawFile.GetInstrumentData();

                    // ReferenceableParamGroupList   
                    _writer.WriteStartElement("referenceableParamGroupList");
                    _writer.WriteAttributeString("count", "1");
                    // ReferenceableParamGroup
                    _writer.WriteStartElement("referenceableParamGroup");
                    _writer.WriteAttributeString("id", "commonInstrumentParams");

                    var instrumentModel = OntologyMapping.getInstrumentModel(instrumentData.Name);
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

                    PopulateInstrumentConfigurationList(firstScanNumber, lastScanNumber, instrumentModel);
                }

                // SoftwareList      
                _writer.WriteStartElement("softwareList");
                _writer.WriteAttributeString("count", "1");
                // Software
                _writer.WriteStartElement("software");
                _writer.WriteAttributeString("id", "ThermoRawFileParser");
                _writer.WriteAttributeString("version", MainClass.Version);
                SerializeCvParam(new CVParamType
                {
                    accession = "MS:1003145",
                    name = "ThermoRawFileParser",
                    cvRef = "MS"
                });
                _writer.WriteEndElement(); // software                
                _writer.WriteEndElement(); // softwareList                                                                                


                // DataProcessingList
                _writer.WriteStartElement("dataProcessingList");
                _writer.WriteAttributeString("count", "1");
                // DataProcessing
                _writer.WriteStartElement("dataProcessing");
                _writer.WriteAttributeString("id", "ThermoRawFileParserProcessing");
                // ProcessingMethod
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
                if (ParseInput.NoPeakPicking.Count < ParseInput.AllLevels.Count)
                {
                    _writer.WriteStartElement("processingMethod");
                    _writer.WriteAttributeString("order", "1");
                    _writer.WriteAttributeString("softwareRef", "ThermoRawFileParser");
                    SerializeCvParam(new CVParamType
                    {
                        accession = "MS:1000035",
                        cvRef = "MS",
                        name = "peak picking",
                        value = ""
                    });
                    _writer.WriteEndElement(); // processingMethod
                }

                _writer.WriteEndElement(); // dataProcessing
                _writer.WriteEndElement(); // dataProcessingList

                // Run
                _writer.WriteStartElement("run");
                //TODO: validate id against NCName
                _writer.WriteAttributeString("id", ParseInput.RawFileNameWithoutExtension);
                _writer.WriteAttributeString("defaultInstrumentConfigurationRef", "IC1");
                _writer.WriteAttributeString("startTimeStamp",
                    XmlConvert.ToString(_rawFile.CreationDate, XmlDateTimeSerializationMode.Utc));
                _writer.WriteAttributeString("defaultSourceFileRef", SourceFileId);
                // SpectrumList
                _writer.WriteStartElement("spectrumList");
                _writer.WriteAttributeString("count", GetTotalScanNumber());
                _writer.WriteAttributeString("defaultDataProcessingRef", "ThermoRawFileParserProcessing");

                serializer = _factory.CreateSerializer(typeof(SpectrumType));

                // MS Spectra
                var index = 0;
                var lastScanProgress = 0;

                if (_rawFile.SelectMsData())
                {
                    Log.Info(String.Format("Processing {0} MS scans", +(1 + lastScanNumber - firstScanNumber)));

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


                        SpectrumType spectrum = null;

                        try
                        {
                            spectrum = ConstructMSSpectrum(scanNumber);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Scan #{scanNumber} cannot be processed because of the following exception: {ex.Message}\n{ex.StackTrace}");
                            ParseInput.NewError();
                        }

                        var level = spectrum != null ? int.Parse(spectrum.cvParam.Where(p => p.accession == "MS:1000511").First().value) : 0;

                        if (spectrum != null && ParseInput.MsLevel.Contains(level)) //applying MS level filter
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

                            Log.Debug("Spectrum added to list of spectra -- ID " + spectrum.id);

                            index++;
                        }
                    }
                }


                if (ParseInput.LogFormat == LogFormat.DEFAULT)
                {
                    Console.WriteLine();
                }

                // PDA spectra
                if (ParseInput.AllDetectors && _rawFile.GetInstrumentCountOfType(Device.Pda) > 0)
                {
                    for (int nrI = 1; nrI < _rawFile.GetInstrumentCountOfType(Device.Pda) + 1; nrI++)
                    {
                        _rawFile.SelectInstrument(Device.Pda, nrI);
                        firstScanNumber = _rawFile.RunHeader.FirstSpectrum;
                        lastScanNumber = _rawFile.RunHeader.LastSpectrum;
                        lastScanProgress = 0;

                        Log.Info(String.Format("Processing {0} PDA scans from Device #{1}",
                            (1 + lastScanNumber - firstScanNumber), nrI));

                        for (var scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
                        {
                            if (ParseInput.LogFormat == LogFormat.DEFAULT)
                            {
                                var scanProgress =
                                    (int)((double)scanNumber / (lastScanNumber - firstScanNumber + 1) * 100);
                                if (scanProgress % ProgressPercentageStep == 0)
                                {
                                    if (scanProgress != lastScanProgress)
                                    {
                                        Console.Write("" + scanProgress + "% ");
                                        lastScanProgress = scanProgress;
                                    }
                                }
                            }

                            SpectrumType spectrum = null;

                            try
                            {
                                spectrum = ConstructPDASpectrum(scanNumber, nrI);
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Scan #{scanNumber} cannot be processed because of the following exception: {ex.Message}\n{ex.StackTrace}");
                                ParseInput.NewError();
                            }

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

                                Log.Debug("Spectrum added to list of spectra -- ID " + spectrum.id);

                                index++;
                            }
                        }
                    }
                }

                if (ParseInput.LogFormat == LogFormat.DEFAULT)
                {
                    Console.WriteLine();
                }

                _writer.WriteEndElement(); // spectrumList                                                

                index = 0;
                var chromatograms = ConstructChromatograms();
                if (!chromatograms.IsNullOrEmpty())
                {
                    // ChromatogramList
                    _writer.WriteStartElement("chromatogramList");
                    _writer.WriteAttributeString("count", chromatograms.Count.ToString());
                    _writer.WriteAttributeString("defaultDataProcessingRef", "ThermoRawFileParserProcessing");
                    serializer = _factory.CreateSerializer(typeof(ChromatogramType));
                    chromatograms.ForEach(chromatogram =>
                    {
                        chromatogram.index = index.ToString();
                        if (_doIndexing)
                        {
                            // Flush the writers before getting the position
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

                    // IndexList
                    _writer.WriteStartElement("indexList");
                    var indexCount = chromatograms.IsNullOrEmpty() ? 1 : 2;
                    _writer.WriteAttributeString("count", indexCount.ToString());
                    // Index
                    _writer.WriteStartElement("index");
                    _writer.WriteAttributeString("name", "spectrum");
                    var spectrumOffsetEnumerator = spectrumOffSets.GetEnumerator();
                    while (spectrumOffsetEnumerator.MoveNext())
                    {
                        // Offset
                        _writer.WriteStartElement("offset");
                        _writer.WriteAttributeString("idRef", spectrumOffsetEnumerator.Key.ToString());
                        _writer.WriteString(spectrumOffsetEnumerator.Value.ToString());
                        _writer.WriteEndElement(); // offset                    
                    }

                    _writer.WriteEndElement(); // index                

                    if (!chromatograms.IsNullOrEmpty())
                    {
                        // Index
                        _writer.WriteStartElement("index");
                        _writer.WriteAttributeString("name", "chromatogram");
                        var chromatogramOffsetEnumerator = chromatogramOffSets.GetEnumerator();
                        while (chromatogramOffsetEnumerator.MoveNext())
                        {
                            // Offset
                            _writer.WriteStartElement("offset");
                            _writer.WriteAttributeString("idRef", chromatogramOffsetEnumerator.Key.ToString());
                            _writer.WriteString(chromatogramOffsetEnumerator.Value.ToString());
                            _writer.WriteEndElement(); // offset                        
                        }

                        _writer.WriteEndElement(); // index                    
                    }

                    _writer.WriteEndElement(); // indexList                                                

                    // IndexListOffset
                    _writer.WriteStartElement("indexListOffset");
                    _writer.WriteString(indexListPosition.ToString());
                    _writer.WriteEndElement(); // indexListOffset                                                

                    // FileChecksum
                    _writer.WriteStartElement("fileChecksum");
                    _writer.WriteString("");

                    _writer.Flush();
                    Writer.Flush();

                    // Write data here
                    cryptoStream.FlushFinalBlock();
                    var hash = sha1.Hash;

                    // Do this for avoiding the "Hash must be finalized before the hash value is retrieved"
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
                    try
                    {
                        cryptoStream.Flush();
                        cryptoStream.Close();
                    }
                    catch (System.ObjectDisposedException e)
                    {
                        // Cannot access a closed file.  CryptoStream was already closed when closing _writer
                        Log.Warn($"Warning: {e.Message}");
                    }
                }
            }

            // In case of indexed mzML, change the extension from xml to mzML and check for the gzip option
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
                        ParseInput.NewError();
                    }
                }

                // Remove the unzipped mzML file
                mzMLFile.Delete();
            }
        }

        private string GetTotalScanNumber()
        {
            // Save the last selected instrument
            var lastSelectedInstrument = _rawFile.SelectedInstrument;
            var numScans = 0;

            if (_rawFile.GetInstrumentCountOfType(Device.MS) != 0)
            {
                _rawFile.SelectInstrument(Device.MS, 1);
                var levelFilter = _rawFile.GetFilterFromString("");

                foreach (var level in ParseInput.MsLevel)
                {
                    levelFilter.MSOrder = (MSOrderType)level;

                    var filteredScans = _rawFile.GetFilteredScansListByScanRange(levelFilter,
                        _rawFile.RunHeader.FirstSpectrum, _rawFile.RunHeader.LastSpectrum);

                    numScans += filteredScans.Count;
                }
            }

            if (ParseInput.AllDetectors)
            {
                for (int nrI = 1; nrI < _rawFile.GetInstrumentCountOfType(Device.Pda) + 1; nrI++)
                {
                    _rawFile.SelectInstrument(Device.Pda, nrI);
                    numScans += 1 + _rawFile.RunHeader.LastSpectrum - _rawFile.RunHeader.FirstSpectrum;
                }
            }

            // Return instrument to last selected one
            if (lastSelectedInstrument != null && lastSelectedInstrument.DeviceType != Device.None && lastSelectedInstrument.InstrumentIndex != -1)
                _rawFile.SelectInstrument(lastSelectedInstrument.DeviceType, lastSelectedInstrument.InstrumentIndex);

            return numScans.ToString();
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
            // Go over the first scans until an MS2 scan is encountered
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
                    catch (Exception)
                    {
                        if (!ParseInput.IgnoreInstrumentErrors)
                        {
                            Log.ErrorFormat("Unsupported ionization mode - {0}; Use ignoreInstrumentErrors key to suppress this error", scanFilter.IonizationMode.ToString());
                            throw;
                        }
                        else
                        {
                            Log.WarnFormat("Unsupported Ionization Mode - {0}", scanFilter.IonizationMode.ToString());
                            ParseInput.NewWarn();
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
                catch (Exception)
                {
                    if (!ParseInput.IgnoreInstrumentErrors)
                    {
                        Log.ErrorFormat("No scan filter found for the following scan {0}; Use ignoreInstrumentErrors key to suppress", scanNumber);
                        throw;
                    }
                    else
                    {
                        Log.WarnFormat("No Scan Filter found for the following scan {0}", scanNumber);
                        ParseInput.NewWarn();
                    }
                }

                scanNumber++;
            } while (!encounteredMs2 && scanNumber <= lastScanNumber);

            // Add a default analyzer if none were found
            if (_massAnalyzers.Count == 0)
            {
                _massAnalyzers.Add(MassAnalyzerType.Any, "IC1");
            }

            // InstrumentConfigurationList
            _writer.WriteStartElement("instrumentConfigurationList");
            _writer.WriteAttributeString("count", _massAnalyzers.Count.ToString());

            // Make a new instrument configuration for each analyzer
            var massAnalyzerIndex = 0;
            foreach (var massAnalyzer in _massAnalyzers)
            {
                // InstrumentConfiguration
                _writer.WriteStartElement("instrumentConfiguration");
                _writer.WriteAttributeString("id", massAnalyzer.Value);
                // ReferenceableParamGroupRef
                _writer.WriteStartElement("referenceableParamGroupRef");
                _writer.WriteAttributeString("ref", "commonInstrumentParams");
                _writer.WriteEndElement(); // referenceableParamGroupRef
                // ComponentList
                _writer.WriteStartElement("componentList");
                _writer.WriteAttributeString("count", "3");

                // Source
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

                // Analyzer             
                _writer.WriteStartElement("analyzer");
                _writer.WriteAttributeString("order", (index + 1).ToString());
                SerializeCvParam(OntologyMapping.MassAnalyzerTypes[massAnalyzer.Key]);
                index++;
                _writer.WriteEndElement(); // analyzer

                // Detector
                _writer.WriteStartElement("detector");
                _writer.WriteAttributeString("order", (index + 1).ToString());

                // Try to map the instrument to the detector
                var detectorCvParams = OntologyMapping.GetDetectors(instrumentModel.accession);
                CVParamType detectorCvParam;
                if (massAnalyzerIndex < detectorCvParams.Count)
                {
                    detectorCvParam = detectorCvParams[massAnalyzerIndex];
                }
                else
                {
                    detectorCvParam = OntologyMapping.GetDetectors("default")[0];
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
        private List<ChromatogramType> ConstructChromatograms()
        {
            var chromatograms = new List<ChromatogramType>();

            // MS chromatograms
            // Reselect MS device
            if (_rawFile.GetInstrumentCountOfType(Device.MS) != 0)
            {
                _rawFile.SelectInstrument(Device.MS, 1);

                // Define the settings for getting the Base Peak chromatogram
                var settings = new ChromatogramTraceSettings(TraceType.BasePeak);

                // Get the chromatogram from the RAW file. 
                var data = _rawFile.GetChromatogramData(new IChromatogramSettings[] { settings }, -1, -1);

                // Split the data into the chromatograms
                var trace = ChromatogramSignal.FromChromatogramData(data);

                for (var i = 0; i < trace.Length; i++)
                {
                    if (trace[i].Length > 0)
                    {
                        // CV Data for Base Peak Chromatogram
                        var chroType = new CVParamType
                        {
                            accession = "MS:1000628",
                            name = "basepeak chromatogram",
                            cvRef = "MS",
                            value = ""
                        };

                        var intensType = new CVParamType
                        {
                            accession = "MS:1000515",
                            name = "intensity array",
                            cvRef = "MS",
                            unitName = "number of counts",
                            value = "",
                            unitCvRef = "MS",
                            unitAccession = "MS:1000131"
                        };

                        var chromatogram = TraceToChromatogram(trace[i], "BasePeak_" + i.ToString(), chroType, intensType);

                        chromatograms.Add(chromatogram);
                    }
                }
            }

            // Chromatograms from other devices: UV, PDA, Analog, MSAnalog
            if (ParseInput.AllDetectors)
            {
                for (int nrI = 1; nrI < _rawFile.GetInstrumentCountOfType(Device.Pda) + 1; nrI++)
                {
                    _rawFile.SelectInstrument(Device.Pda, nrI);

                    var instData = _rawFile.GetInstrumentData();

                    var settings = new ChromatogramTraceSettings(TraceType.TotalAbsorbance);

                    var data = _rawFile.GetChromatogramData(new IChromatogramSettings[] { settings }, -1, -1);

                    var trace = ChromatogramSignal.FromChromatogramData(data);

                    for (var i = 0; i < trace.Length; i++)
                    {
                        // CV Data for Total Absorbance Chromatogram
                        var chroType = new CVParamType
                        {
                            accession = "MS:1000812",
                            name = "absorption chromatogram",
                            cvRef = "MS",
                            value = ""
                        };

                        var intensType = new CVParamType
                        {
                            accession = "MS:1000515",
                            name = "intensity array",
                            cvRef = "MS",
                            unitName = "absorbance unit",
                            value = instData.Units.ToString(),
                            unitCvRef = "UO",
                            unitAccession = "UO:0000269"
                        };

                        var chromatogram = TraceToChromatogram(trace[i],
                            String.Format("PDA#{0}_TotalAbsorbance_{1}", nrI, i),
                            chroType, intensType);

                        chromatograms.Add(chromatogram);
                    }
                }

                for (int nrI = 1; nrI < _rawFile.GetInstrumentCountOfType(Device.UV) + 1; nrI++)
                {
                    _rawFile.SelectInstrument(Device.UV, nrI);

                    var instData = _rawFile.GetInstrumentData();

                    for (int channel = 0; channel < instData.ChannelLabels.Length; channel++)
                    {
                        var channelName = instData.ChannelLabels[channel];

                        var settings = new ChromatogramTraceSettings(TraceType.StartUVChromatogramTraces + channel + 1);

                        var data = _rawFile.GetChromatogramData(new IChromatogramSettings[] { settings }, -1, -1);

                        var trace = ChromatogramSignal.FromChromatogramData(data);

                        for (var i = 0; i < trace.Length; i++)
                        {
                            // CV Data for Absorbance Chromatogram
                            var chroType = new CVParamType
                            {
                                accession = "MS:1000812",
                                name = "absorption chromatogram",
                                cvRef = "MS",
                                value = ""
                            };

                            var intensType = new CVParamType
                            {
                                accession = "MS:1000515",
                                name = "intensity array",
                                cvRef = "MS",
                                unitName = "absorbance unit",
                                value = instData.Units.ToString(),
                                unitCvRef = "UO",
                                unitAccession = "UO:0000269"
                            };

                            var chromatogram = TraceToChromatogram(trace[i],
                                String.Format("UV#{0}_{1}_{2}", nrI, channelName, i),
                                chroType, intensType);

                            chromatograms.Add(chromatogram);
                        }
                    }
                }

                for (int nrI = 1; nrI < _rawFile.GetInstrumentCountOfType(Device.Analog) + 1; nrI++)
                {
                    _rawFile.SelectInstrument(Device.Analog, nrI);

                    var instData = _rawFile.GetInstrumentData();

                    for (int channel = 0; channel < instData.ChannelLabels.Length; channel++)
                    {
                        var channelName = instData.ChannelLabels[channel];

                        if (channelName.ToLower().Contains("pressure"))
                        {
                            var settings = new ChromatogramTraceSettings(TraceType.StartPCA2DChromatogramTraces + channel +
                                                                     1);

                            var data = _rawFile.GetChromatogramData(new IChromatogramSettings[] { settings }, -1, -1);

                            var trace = ChromatogramSignal.FromChromatogramData(data);

                            for (var i = 0; i < trace.Length; i++)
                            {
                                // CV Data for Pressure Chromatogram
                                var chroType = new CVParamType
                                {
                                    accession = "MS:1003019",
                                    name = "pressure chromatogram",
                                    cvRef = "MS",
                                    value = ""
                                };

                                var intensType = new CVParamType
                                {
                                    accession = "MS:1000821",
                                    name = "pressure array",
                                    cvRef = "MS",
                                    unitName = "pressure unit",
                                    value = "",
                                    unitCvRef = "UO",
                                    unitAccession = "UO:0000109"
                                };

                                var chromatogram = TraceToChromatogram(trace[i],
                                    String.Format("AD#{0}_{1}_{2}", nrI, channelName, i),
                                    chroType, intensType);

                                chromatograms.Add(chromatogram);
                            }
                        }
                    }
                }

                var channelNameIndex = 0;
                for (int nrI = 1; nrI < _rawFile.GetInstrumentCountOfType(Device.MSAnalog) + 1; nrI++)
                {
                    _rawFile.SelectInstrument(Device.MSAnalog, nrI);

                    var instData = _rawFile.GetInstrumentData();

                    for (int channel = 0; channel < instData.ChannelLabels.Length; channel++)
                    {
                        var channelName = instData.ChannelLabels[channel];

                        if (channelName.IsNullOrEmpty())
                        {
                            channelName = "Channel " + channelNameIndex++;
                        }

                        var settings = new ChromatogramTraceSettings(TraceType.StartAnalogChromatogramTraces + channel +
                                                                    1);

                        var data = _rawFile.GetChromatogramData(new IChromatogramSettings[] { settings }, -1, -1);

                        var trace = ChromatogramSignal.FromChromatogramData(data);

                        for (var i = 0; i < trace.Length; i++)
                        {
                            // CV Data for Chromatogram
                            var chroType = new CVParamType
                            {
                                name = channelName + " chromatogram",
                                value = ""
                            };

                            var intensType = new CVParamType();
                            if (instData.Units.ToString().Equals("Volts"))
                            {
                                intensType = new CVParamType
                                {
                                    name = channelName + " array",
                                    unitAccession = "UO:0000218",
                                    unitName = "volt",
                                    unitCvRef = "UO"
                                };
                            }
                            else
                            {
                                intensType = new CVParamType
                                {
                                    name = channelName + " array",
                                    value = instData.Units.ToString(),
                                };
                            }

                            var chromatogram = TraceToChromatogram(trace[i],
                                String.Format("MSAD#{0}_{1}_{2}", nrI, channelName.Replace(" ", "_"), i),
                                chroType, intensType);

                            chromatograms.Add(chromatogram);
                        }
                    }
                }
            }

            return chromatograms;
        }

        private ChromatogramType TraceToChromatogram(ChromatogramSignal trace, string chromatogramId,
            CVParamType chromatogramType, CVParamType intensityType)
        {
            var binaryData = new List<BinaryDataArrayType>();

            var chromatogram = new ChromatogramType
            {
                index = String.Empty, //index will be overwritten during serialization
                id = chromatogramId,
                defaultArrayLength = 0,
                binaryDataArrayList = new BinaryDataArrayListType
                {
                    count = "2",
                    binaryDataArray = new BinaryDataArrayType[2]
                },
                cvParam = new CVParamType[1]
            };

            chromatogram.cvParam[0] = chromatogramType;

            // Chromatogram times
            if (!trace.Times.IsNullOrEmpty())
            {
                // Set the chromatogram default array length
                chromatogram.defaultArrayLength = trace.Times.Count;

                var timesBinaryData =
                    new BinaryDataArrayType
                    {
                        binary = ParseInput.NoZlibCompression
                            ? Get64BitArray(trace.Times)
                            : GetZLib64BitArray(trace.Times)
                    };
                timesBinaryData.encodedLength =
                    (4 * Math.Ceiling((double)timesBinaryData
                        .binary.Length / 3)).ToString(CultureInfo.InvariantCulture);
                var timesBinaryDataCvParams = new List<CVParamType>
                {
                    new CVParamType
                    {
                        accession = "MS:1000595",
                        name = "time array",
                        cvRef = "MS",
                        unitName = "minute",
                        value = "",
                        unitCvRef = "UO",
                        unitAccession = "UO:0000031"
                    },
                    new CVParamType
                    {
                        accession = "MS:1000523", name = "64-bit float", cvRef = "MS", value = ""
                    }
                };
                if (!ParseInput.NoZlibCompression)
                {
                    timesBinaryDataCvParams.Add(
                        new CVParamType
                        {
                            accession = "MS:1000574",
                            name = "zlib compression",
                            cvRef = "MS",
                            value = ""
                        });
                }
                else
                {
                    timesBinaryDataCvParams.Add(
                        new CVParamType
                        {
                            accession = "MS:1000576",
                            name = "no compression",
                            cvRef = "MS",
                            value = ""
                        });
                }

                timesBinaryData.cvParam = timesBinaryDataCvParams.ToArray();

                binaryData.Add(timesBinaryData);
            }

            // Chromatogram intensities                    
            if (!trace.Intensities.IsNullOrEmpty())
            {
                // Set the spectrum default array length if necessary
                // Is it necessary?
                if (chromatogram.defaultArrayLength == 0)
                {
                    chromatogram.defaultArrayLength = trace.Intensities.Count;
                }

                var intensitiesBinaryData =
                    new BinaryDataArrayType
                    {
                        binary = ParseInput.NoZlibCompression
                            ? Get64BitArray(trace.Intensities)
                            : GetZLib64BitArray(trace.Intensities)
                    };
                intensitiesBinaryData.encodedLength =
                    (4 * Math.Ceiling((double)intensitiesBinaryData
                        .binary.Length / 3)).ToString(CultureInfo.InvariantCulture);
                var intensitiesBinaryDataCvParams = new List<CVParamType>
                {
                    intensityType,
                    new CVParamType
                    {
                        accession = "MS:1000523", name = "64-bit float", cvRef = "MS", value = ""
                    }
                };
                if (!ParseInput.NoZlibCompression)
                {
                    intensitiesBinaryDataCvParams.Add(
                        new CVParamType
                        {
                            accession = "MS:1000574",
                            name = "zlib compression",
                            cvRef = "MS",
                            value = ""
                        });
                }
                else
                {
                    intensitiesBinaryDataCvParams.Add(
                        new CVParamType
                        {
                            accession = "MS:1000576",
                            name = "no compression",
                            cvRef = "MS",
                            value = ""
                        });
                }

                intensitiesBinaryData.cvParam = intensitiesBinaryDataCvParams.ToArray();

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

            return chromatogram;
        }

        /// <summary>
        /// Construct a spectrum element for the given scan
        /// </summary>
        /// <param name="scanNumber">the scan number</param>
        /// <returns>The SpectrumType object</returns>
        private SpectrumType ConstructMSSpectrum(int scanNumber)
        {
            // Last precursor scan number for use in MSn spectrum
            int _precursorScanNumber = 0;

            // Get each scan from the RAW file
            var scan = Scan.FromFile(_rawFile, scanNumber);

            // Get the scan filter for this scan number
            var scanFilter = _rawFile.GetFilterForScanNumber(scanNumber);

            // Get scan ms level
            var msLevel = (int)scanFilter.MSOrder;

            // Get the scan event for this scan number
            var scanEvent = _rawFile.GetScanEventForScanNumber(scanNumber);
            var spectrum = new SpectrumType
            {
                id = ConstructSpectrumTitle((int)Device.MS, 1, scanNumber),
                defaultArrayLength = 0
            };

            // Keep the CV params in a list and convert to array afterwards
            var spectrumCvParams = new List<CVParamType>
            {
                new CVParamType
                {
                    name = "ms level",
                    accession = "MS:1000511",
                    value = msLevel.ToString(),
                    cvRef = "MS"
                }
            };

            // Trailer extra data list
            ScanTrailer trailerData;

            try
            {
                trailerData = new ScanTrailer(_rawFile.GetTrailerExtraInformation(scanNumber));
            }
            catch (Exception ex)
            {
                Log.WarnFormat("Cannot load trailer infromation for scan {0} due to following exception\n{1}", scanNumber, ex.Message);
                ParseInput.NewWarn();
                trailerData = new ScanTrailer();
            }

            int? charge = trailerData.AsPositiveInt("Charge State:");
            double? monoisotopicMz = trailerData.AsDouble("Monoisotopic M/Z:");
            double? ionInjectionTime = trailerData.AsDouble("Ion Injection Time (ms):");
            double? isolationWidth = trailerData.AsDouble("MS" + (int)scanFilter.MSOrder + " Isolation Width:");
            double? FAIMSCV = null;
            if (trailerData.AsBool("FAIMS Voltage On:").GetValueOrDefault(false))
                FAIMSCV = trailerData.AsDouble("FAIMS CV:");

            List<double> SPSMasses = new List<double>();
            //tune version < 3
            if (trailerData.Has("SPS Mass 1:"))
            {
                foreach (var label in trailerData.MatchKeys(_spSentry))
                {
                    var mass = trailerData.AsDouble(label).GetValueOrDefault(0);
                    if (mass > 0) SPSMasses.Add((double)mass); //zero means mass does not exist
                }
            }

            //tune version == 3
            if (trailerData.Has("SPS Masses:"))
            {
                foreach (var labelvalue in trailerData.MatchValues(_spSentry3))
                {
                    foreach (var mass in labelvalue.Trim()
                        .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        SPSMasses.Add(double.Parse(mass));
                    }
                }
            }

            // Construct and set the scan list element of the spectrum
            var scanListType = ConstructScanList(scanNumber, scan, scanFilter, scanEvent, monoisotopicMz,
                ionInjectionTime);
            spectrum.scanList = scanListType;


            if (msLevel == 1)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    accession = "MS:1000579",
                    cvRef = "MS",
                    name = "MS1 spectrum",
                    value = ""
                });

                // Keep track of scan number for precursor reference
                _precursorScanNumbers[""] = scanNumber;
                _precursorTree[scanNumber] = new PrecursorInfo();

            }
            else if (msLevel > 1)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    accession = "MS:1000580",
                    cvRef = "MS",
                    name = "MSn spectrum",
                    value = ""
                });

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
                    _precursorScanNumber = GetParentFromScanString(result.Groups[1].Value);
                }

                if (_precursorScanNumber > 0)
                {

                    try
                    {
                        try //since there is no direct way to get the number of reactions available, it is necessary to try and fail
                        {
                            scanEvent.GetReaction(_precursorTree[_precursorScanNumber].ReactionCount);
                        }
                        catch (ArgumentOutOfRangeException ex)
                        {
                            Log.Debug($"Using Tribrid decision tree fix for scan# {scanNumber}");
                            //Is it a decision tree scheduled scan on tribrid?
                            if (msLevel == _precursorTree[_precursorScanNumber].MSLevel)
                            {
                                _precursorScanNumber = GetParentFromScanString(result.Groups[1].Value);
                            }
                            else
                            {
                                throw new RawFileParserException(
                                    $"Tribrid decision tree fix failed - cannot get reaction# {_precursorTree[_precursorScanNumber].ReactionCount} from {scanEvent.ToString()}",
                                    ex);
                            }
                        }

                        // Construct and set the precursor list element of the spectrum
                        spectrum.precursorList =
                            ConstructPrecursorList(_precursorScanNumber, scanEvent, charge, monoisotopicMz, isolationWidth,
                                SPSMasses, out var reactionCount);

                        //save precursor information for later reference
                        _precursorTree[scanNumber] = new PrecursorInfo(_precursorScanNumber, msLevel, reactionCount, spectrum.precursorList.precursor);
                    }
                    catch (Exception e)
                    {
                        var extra = (e.InnerException is null) ? "" : $"\n{e.InnerException.StackTrace}";

                        Log.Warn($"Failed creating precursor list for scan# {scanNumber} - precursor information for this and dependent scans will be empty\nException details:{e.Message}\n{e.StackTrace}\n{extra}");
                        ParseInput.NewWarn();

                        _precursorTree[scanNumber] = new PrecursorInfo(_precursorScanNumber, 1, 0, new PrecursorType[0]);

                    }

                }
                else
                {
                    spectrum.precursorList = new PrecursorListType
                    {
                        count = "0",
                        precursor = new PrecursorType[0]
                    };

                    Log.Error($"Failed finding precursor for {scanNumber}");
                    ParseInput.NewError();
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Unknown msLevel: {msLevel}");
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
                    throw new ArgumentOutOfRangeException("PolarityType is Unknown");
            }

            // Total ion current
            spectrumCvParams.Add(new CVParamType
            {
                name = "total ion current",
                accession = "MS:1000285",
                value = scan.ScanStatistics.TIC.ToString(CultureInfo.InvariantCulture),
                cvRef = "MS"
            });

            // FAIMS
            if (FAIMSCV != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "FAIMS compensation voltage",
                    accession = "MS:1001581",
                    value = FAIMSCV.ToString(),
                    cvRef = "MS"
                });
            }

            double? basePeakMass;
            double? basePeakIntensity;
            double? lowestObservedMz = null;
            double? highestObservedMz = null;
            double[] masses;
            double[] intensities;

            if (!ParseInput.NoPeakPicking.Contains((int)scanFilter.MSOrder))
            {
                //Spectrum will be centroided
                spectrumCvParams.Add(new CVParamType
                {
                    accession = "MS:1000127",
                    cvRef = "MS",
                    name = "centroid spectrum",
                    value = ""
                });

                // Check if the scan has a centroid stream
                if (scan.HasCentroidStream)
                {
                    basePeakMass = scan.CentroidScan.BasePeakMass;
                    basePeakIntensity = scan.CentroidScan.BasePeakIntensity;

                    masses = scan.CentroidScan.Masses;
                    intensities = scan.CentroidScan.Intensities;

                    if (scan.CentroidScan.Length > 0)
                    {
                        lowestObservedMz = scan.CentroidScan.Masses[0];
                        highestObservedMz = scan.CentroidScan.Masses[scan.CentroidScan.Masses.Length - 1];
                    }
                }
                else // otherwise take the segmented (low res) scan
                {
                    basePeakMass = scan.ScanStatistics.BasePeakMass;
                    basePeakIntensity = scan.ScanStatistics.BasePeakIntensity;

                    // If the spectrum is profile perform centroiding
                    var segmentedScan = scanEvent.ScanData == ScanDataType.Profile
                        ? Scan.ToCentroid(scan).SegmentedScan
                        : scan.SegmentedScan;

                    masses = segmentedScan.Positions;
                    intensities = segmentedScan.Intensities;

                    if (segmentedScan.PositionCount > 0)
                    {
                        lowestObservedMz = segmentedScan.Positions[0];
                        highestObservedMz = segmentedScan.Positions[segmentedScan.PositionCount - 1];
                    }
                }
            }
            else // use the segmented data as is
            {
                switch (scanEvent.ScanData) //check if the data centroided already
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

                basePeakMass = scan.ScanStatistics.BasePeakMass;
                basePeakIntensity = scan.ScanStatistics.BasePeakIntensity;
                masses = scan.SegmentedScan.Positions;
                intensities = scan.SegmentedScan.Intensities;

                if (scan.SegmentedScan.Positions.Length > 0)
                {
                    lowestObservedMz = scan.SegmentedScan.Positions[0];
                    highestObservedMz = scan.SegmentedScan.Positions[scan.SegmentedScan.Positions.Length - 1];
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
            if (masses != null)
            {
                //sorting
                Array.Sort(masses, intensities);

                // Set the spectrum default array length
                spectrum.defaultArrayLength = masses.Length;

                var massesBinaryData =
                    new BinaryDataArrayType
                    {
                        binary = ParseInput.NoZlibCompression ? Get64BitArray(masses) : GetZLib64BitArray(masses)
                    };
                massesBinaryData.encodedLength =
                    (4 * Math.Ceiling((double)massesBinaryData
                        .binary.Length / 3)).ToString(CultureInfo.InvariantCulture);
                var massesBinaryDataCvParams = new List<CVParamType>
                {
                    new CVParamType
                    {
                        accession = "MS:1000514",
                        name = "m/z array",
                        cvRef = "MS",
                        unitName = "m/z",
                        value = "",
                        unitCvRef = "MS",
                        unitAccession = "MS:1000040"
                    },
                    new CVParamType {accession = "MS:1000523", name = "64-bit float", cvRef = "MS", value = ""}
                };

                if (!ParseInput.NoZlibCompression)
                {
                    massesBinaryDataCvParams.Add(
                        new CVParamType
                        {
                            accession = "MS:1000574",
                            name = "zlib compression",
                            cvRef = "MS",
                            value = ""
                        });
                }
                else
                {
                    massesBinaryDataCvParams.Add(
                        new CVParamType
                        {
                            accession = "MS:1000576",
                            name = "no compression",
                            cvRef = "MS",
                            value = ""
                        });
                }

                massesBinaryData.cvParam = massesBinaryDataCvParams.ToArray();

                binaryData.Add(massesBinaryData);
            }

            // Intensity Data
            if (intensities != null)
            {
                // Set the spectrum default array length if necessary
                if (spectrum.defaultArrayLength == 0)
                {
                    spectrum.defaultArrayLength = intensities.Length;
                }

                var intensitiesBinaryData =
                    new BinaryDataArrayType
                    {
                        binary = ParseInput.NoZlibCompression
                            ? Get64BitArray(intensities)
                            : GetZLib64BitArray(intensities)
                    };
                intensitiesBinaryData.encodedLength =
                    (4 * Math.Ceiling((double)intensitiesBinaryData
                        .binary.Length / 3)).ToString(CultureInfo.InvariantCulture);
                var intensitiesBinaryDataCvParams = new List<CVParamType>
                {
                    new CVParamType
                    {
                        accession = "MS:1000515",
                        name = "intensity array",
                        cvRef = "MS",
                        unitCvRef = "MS",
                        unitAccession = "MS:1000131",
                        unitName = "number of counts",
                        value = ""
                    },
                    new CVParamType {accession = "MS:1000523", name = "64-bit float", cvRef = "MS", value = ""}
                };
                if (!ParseInput.NoZlibCompression)
                {
                    intensitiesBinaryDataCvParams.Add(
                        new CVParamType
                        {
                            accession = "MS:1000574",
                            name = "zlib compression",
                            cvRef = "MS",
                            value = ""
                        });
                }
                else
                {
                    intensitiesBinaryDataCvParams.Add(
                        new CVParamType
                        {
                            accession = "MS:1000576",
                            name = "no compression",
                            cvRef = "MS",
                            value = ""
                        });
                }

                intensitiesBinaryData.cvParam = intensitiesBinaryDataCvParams.ToArray();

                binaryData.Add(intensitiesBinaryData);
            }

            // Include optional noise data
            if (ParseInput.NoiseData)
            {
                double[] baselineData;
                double[] noiseData;
                double[] massData;

                baselineData = scan.PreferredBaselines;
                noiseData = scan.PreferredNoises;
                massData = scan.PreferredMasses;

                // Noise Data
                if ((baselineData != null) && (noiseData != null) && (massData != null))
                {
                    // Set the spectrum default array length if necessary
                    if (spectrum.defaultArrayLength == 0)
                    {
                        spectrum.defaultArrayLength = noiseData.Length;
                    }

                    var baselineBinaryData =
                    new BinaryDataArrayType
                    {
                        binary = ParseInput.NoZlibCompression
                            ? Get64BitArray(baselineData)
                            : GetZLib64BitArray(baselineData)
                    };
                    baselineBinaryData.encodedLength =
                        (4 * Math.Ceiling((double)baselineBinaryData
                            .binary.Length / 3)).ToString(CultureInfo.InvariantCulture);

                    var baselineBinaryDataCvParams = new List<CVParamType>
                    {
                        new CVParamType
                        {
                            accession = "MS:1002745",
                            name = "sampled noise baseline array",
                            cvRef = "MS",
                            value = ""
                        },
                        new CVParamType {accession = "MS:1000523", name = "64-bit float", cvRef = "MS", value = ""}
                    };

                    if (!ParseInput.NoZlibCompression)
                    {
                        baselineBinaryDataCvParams.Add(
                            new CVParamType
                            {
                                accession = "MS:1000574",
                                name = "zlib compression",
                                cvRef = "MS",
                                value = ""
                            });
                    }
                    else
                    {
                        baselineBinaryDataCvParams.Add(
                            new CVParamType
                            {
                                accession = "MS:1000576",
                                name = "no compression",
                                cvRef = "MS",
                                value = ""
                            });
                    }

                    baselineBinaryData.cvParam = baselineBinaryDataCvParams.ToArray();

                    binaryData.Add(baselineBinaryData);

                    var noiseBinaryData =
                    new BinaryDataArrayType
                    {
                        binary = ParseInput.NoZlibCompression
                            ? Get64BitArray(noiseData)
                            : GetZLib64BitArray(noiseData)
                    };
                    noiseBinaryData.encodedLength =
                        (4 * Math.Ceiling((double)noiseBinaryData
                            .binary.Length / 3)).ToString(CultureInfo.InvariantCulture);

                    var noiseBinaryDataCvParams = new List<CVParamType>
                    {
                        new CVParamType
                        {
                            accession = "MS:1002744",
                            name = "sampled noise intensity array",
                            cvRef = "MS",
                            unitCvRef = "MS",
                            unitAccession = "MS:1000131",
                            unitName = "number of detector counts",
                            value = ""
                        },
                        new CVParamType {accession = "MS:1000523", name = "64-bit float", cvRef = "MS", value = ""}
                    };


                    if (!ParseInput.NoZlibCompression)
                    {
                        noiseBinaryDataCvParams.Add(
                            new CVParamType
                            {
                                accession = "MS:1000574",
                                name = "zlib compression",
                                cvRef = "MS",
                                value = ""
                            });
                    }
                    else
                    {
                        noiseBinaryDataCvParams.Add(
                            new CVParamType
                            {
                                accession = "MS:1000576",
                                name = "no compression",
                                cvRef = "MS",
                                value = ""
                            });
                    }

                    noiseBinaryData.cvParam = noiseBinaryDataCvParams.ToArray();

                    binaryData.Add(noiseBinaryData);

                    var massBinaryData =
                    new BinaryDataArrayType
                    {
                        binary = ParseInput.NoZlibCompression
                            ? Get64BitArray(massData)
                            : GetZLib64BitArray(massData)
                    };
                    massBinaryData.encodedLength =
                        (4 * Math.Ceiling((double)massBinaryData
                            .binary.Length / 3)).ToString(CultureInfo.InvariantCulture);

                    var massBinaryDataCvParams = new List<CVParamType>
                    {
                        new CVParamType
                        {
                            accession = "MS:1002743",
                            name = "sampled noise m/z array",
                            cvRef = "MS",
                            unitCvRef = "MS",
                            unitAccession = "MS:1000040",
                            unitName = "m/z",
                            value = ""
                        },
                        new CVParamType {accession = "MS:1000523", name = "64-bit float", cvRef = "MS", value = ""}
                    };


                    if (!ParseInput.NoZlibCompression)
                    {
                        massBinaryDataCvParams.Add(
                            new CVParamType
                            {
                                accession = "MS:1000574",
                                name = "zlib compression",
                                cvRef = "MS",
                                value = ""
                            });
                    }
                    else
                    {
                        massBinaryDataCvParams.Add(
                            new CVParamType
                            {
                                accession = "MS:1000576",
                                name = "no compression",
                                cvRef = "MS",
                                value = ""
                            });
                    }

                    massBinaryData.cvParam = massBinaryDataCvParams.ToArray();

                    binaryData.Add(massBinaryData);
                }
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

        private SpectrumType ConstructPDASpectrum(int scanNumber, int instrumentNumber)
        {
            // Get each scan from the RAW file
            var scan = Scan.FromFile(_rawFile, scanNumber);

            var spectrum = new SpectrumType
            {
                id = ConstructSpectrumTitle((int)Device.Pda, instrumentNumber, scanNumber),
                defaultArrayLength = 0
            };

            // Keep the CV params in a list and convert to array afterwards
            var spectrumCvParams = new List<CVParamType>
            {
                new CVParamType
                {
                    name = "electromagnetic radiation spectrum",
                    accession = "MS:1000804",
                    value = String.Empty,
                    cvRef = "MS"
                }
            };

            // Construct and set the scan list element of the spectrum
            var scanListType = ConstructScanList(scanNumber, scan);
            spectrum.scanList = scanListType;

            // Scan data
            double? basePeakPosition = null;
            double? basePeakIntensity = null;
            double? lowestPosition = null;
            double? highestPosition = null;
            double[] positions = null;
            double[] intensities = null;


            basePeakPosition = scan.ScanStatistics.BasePeakMass;
            basePeakIntensity = scan.ScanStatistics.BasePeakIntensity;

            if (scan.SegmentedScan.Positions.Length > 0)
            {
                lowestPosition = scan.SegmentedScan.Positions[0];
                highestPosition = scan.SegmentedScan.Positions[scan.SegmentedScan.Positions.Length - 1];
                positions = scan.SegmentedScan.Positions;
                intensities = scan.SegmentedScan.Intensities;
            }


            // Base peak m/z
            if (basePeakPosition != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "base peak m/z",
                    accession = "MS:1000504",
                    value = basePeakPosition.Value.ToString(CultureInfo.InvariantCulture),
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

            // Lowest observed wavelength
            if (lowestPosition != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "lowest observed wavelength",
                    accession = "MS:1000619",
                    value = lowestPosition.Value.ToString(CultureInfo.InvariantCulture),
                    unitCvRef = "MS",
                    unitAccession = "UO:0000018",
                    unitName = "nanometer",
                    cvRef = "UO"
                });
            }

            // Highest observed wavelength
            if (highestPosition != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "highest observed wavelength",
                    accession = "MS:1000618",
                    value = highestPosition.Value.ToString(CultureInfo.InvariantCulture),
                    unitAccession = "UO:0000018",
                    unitName = "nanometer",
                    unitCvRef = "UO",
                    cvRef = "MS"
                });
            }

            // Add the CV params to the spectrum
            spectrum.cvParam = spectrumCvParams.ToArray();

            // Binary data array list
            var binaryData = new List<BinaryDataArrayType>();

            // Spectral data
            if (!positions.IsNullOrEmpty())
            {
                // Set the spectrum default array length
                spectrum.defaultArrayLength = positions.Length;

                var positionsBinaryData =
                    new BinaryDataArrayType
                    {
                        binary = ParseInput.NoZlibCompression ? Get64BitArray(positions) : GetZLib64BitArray(positions)
                    };
                positionsBinaryData.encodedLength =
                    (4 * Math.Ceiling((double)positionsBinaryData
                        .binary.Length / 3)).ToString(CultureInfo.InvariantCulture);
                var positionsBinaryDataCvParams = new List<CVParamType>
                {
                    new CVParamType
                    {
                        accession = "MS:1000617",
                        name = "wavelength array",
                        cvRef = "MS",
                        unitName = "nanometer",
                        value = "",
                        unitCvRef = "UO",
                        unitAccession = "UO:0000018"
                    },
                    new CVParamType {accession = "MS:1000523", name = "64-bit float", cvRef = "MS", value = ""}
                };
                if (!ParseInput.NoZlibCompression)
                {
                    positionsBinaryDataCvParams.Add(
                        new CVParamType
                        {
                            accession = "MS:1000574",
                            name = "zlib compression",
                            cvRef = "MS",
                            value = ""
                        });
                }
                else
                {
                    positionsBinaryDataCvParams.Add(
                        new CVParamType
                        {
                            accession = "MS:1000576",
                            name = "no compression",
                            cvRef = "MS",
                            value = ""
                        });
                }

                positionsBinaryData.cvParam = positionsBinaryDataCvParams.ToArray();

                binaryData.Add(positionsBinaryData);
            }

            // Intensity Data
            if (!intensities.IsNullOrEmpty())
            {
                // Set the spectrum default array length if necessary
                if (spectrum.defaultArrayLength == 0)
                {
                    spectrum.defaultArrayLength = intensities.Length;
                }

                var intensitiesBinaryData =
                    new BinaryDataArrayType
                    {
                        binary = ParseInput.NoZlibCompression
                            ? Get64BitArray(intensities)
                            : GetZLib64BitArray(intensities)
                    };
                intensitiesBinaryData.encodedLength =
                    (4 * Math.Ceiling((double)intensitiesBinaryData
                        .binary.Length / 3)).ToString(CultureInfo.InvariantCulture);
                var intensitiesBinaryDataCvParams = new List<CVParamType>
                {
                    new CVParamType
                    {
                        accession = "MS:1000515",
                        name = "intensity array",
                        cvRef = "MS",
                        unitCvRef = "UO",
                        unitAccession = "UO:0000269",
                        unitName = "absorbance unit",
                        value = ""
                    },
                    new CVParamType {accession = "MS:1000523", name = "64-bit float", cvRef = "MS", value = ""}
                };
                if (!ParseInput.NoZlibCompression)
                {
                    intensitiesBinaryDataCvParams.Add(
                        new CVParamType
                        {
                            accession = "MS:1000574",
                            name = "zlib compression",
                            cvRef = "MS",
                            value = ""
                        });
                }
                else
                {
                    intensitiesBinaryDataCvParams.Add(
                        new CVParamType
                        {
                            accession = "MS:1000576",
                            name = "no compression",
                            cvRef = "MS",
                            value = ""
                        });
                }

                intensitiesBinaryData.cvParam = intensitiesBinaryDataCvParams.ToArray();

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
        /// <param name="precursorScanNumber">scan number for the last precursor</param>
        /// <param name="scanEvent">the scan event</param>
        /// <param name="charge">the charge from trailer</param>
        /// <param name="monoisotopicMz">the monoisotopic m/z value from trailer</param>
        /// <param name="isolationWidth">the isolation width value from trailer</param>
        /// <param name="SPSMasses">List of masses selected for SPS</param>
        /// <param name="reactionCount">Number of activation reactions (see PrecursorInfo for details)</param>
        /// <returns>the precursor list</returns>
        private PrecursorListType ConstructPrecursorList(int precursorScanNumber, IScanEventBase scanEvent, int? charge,
            double? monoisotopicMz, double? isolationWidth, List<double> SPSMasses, out int reactionCount)
        {
            List<PrecursorType> precursors = new List<PrecursorType>();

            // Get precursors from earlier levels
            var prevPrecursors = _precursorTree[precursorScanNumber];

            var spectrumRef = "";
            int msLevel = (int)scanEvent.MSOrder;
            IReaction reaction = null;
            var precursorMz = 0.0;
            reactionCount = prevPrecursors.ReactionCount;

            spectrumRef = ConstructSpectrumTitle((int)Device.MS, 1, precursorScanNumber);
            reaction = scanEvent.GetReaction(reactionCount);

            precursorMz = reaction.PrecursorMass;

            //if isolation width was not found in the trailer, try to get one from the reaction
            if (isolationWidth == null) isolationWidth = reaction.IsolationWidth;

            var precursor = new PrecursorType
            {
                selectedIonList =
                    new SelectedIonListType { count = "1", selectedIon = new ParamGroupType[1] },
                spectrumRef = spectrumRef
            };

            precursor.selectedIonList.selectedIon[0] = new ParamGroupType();

            // Selected ion MZ
            var selectedIonMz = CalculateSelectedIonMz(reaction, monoisotopicMz, isolationWidth);
            var ionCvParams = new List<CVParamType>
            {
                new CVParamType
                {
                    name = "selected ion m/z",
                    value = selectedIonMz.ToString(CultureInfo.InvariantCulture),
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

            if (selectedIonMz > ZeroDelta)
            {
                var selectedIonIntensity = CalculatePrecursorPeakIntensity(_rawFile, precursorScanNumber, reaction.PrecursorMass, isolationWidth,
                    ParseInput.NoPeakPicking.Contains(msLevel - 1));
                if (selectedIonIntensity != null)
                {
                    ionCvParams.Add(new CVParamType
                    {
                        name = "peak intensity",
                        value = selectedIonIntensity.ToString(),
                        accession = "MS:1000042",
                        cvRef = "MS",
                        unitAccession = "MS:1000131",
                        unitCvRef = "MS",
                        unitName = "number of detector counts"
                    });
                }
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
                    value = precursorMz.ToString(CultureInfo.InvariantCulture),
                    cvRef = "MS",
                    unitCvRef = "MS",
                    unitAccession = "MS:1000040",
                    unitName = "m/z"
                };
            if (isolationWidth != null)
            {
                var offset = isolationWidth.Value / 2 + reaction.IsolationWidthOffset;
                precursor.isolationWindow.cvParam[1] =
                    new CVParamType
                    {
                        accession = "MS:1000828",
                        name = "isolation window lower offset",
                        value = (isolationWidth.Value - offset).ToString(CultureInfo.InvariantCulture),
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

            //increase reaction count after successful parsing
            reactionCount++;

            if (scanEvent.SupplementalActivation == TriState.On)
            //the property is On if *at least* one of the levels had SA (i.e. not necissirily the last one), thus we need to try (and posibly fail)
            {
                try
                {
                    reaction = scanEvent.GetReaction(reactionCount);

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

                        // Add the supplemental CV term
                        switch (reaction.ActivationType)
                        {
                            case ActivationType.HigherEnergyCollisionalDissociation:
                                activationCvParams.Add(new CVParamType
                                {
                                    accession = "MS:1002678",
                                    name = "supplemental beam-type collision-induced dissociation",
                                    cvRef = "MS",
                                    value = ""
                                }); break;

                            case ActivationType.CollisionInducedDissociation:
                                activationCvParams.Add(new CVParamType
                                {
                                    accession = "MS:1002679",
                                    name = "supplemental collision-induced dissociation",
                                    cvRef = "MS",
                                    value = ""
                                }); break;

                            default:
                                Log.Warn($"Unknown supplemental activation type: {reaction.ActivationType}");
                                ParseInput.NewWarn();
                                break;

                        }

                        //increase reaction count after successful parsing
                        reactionCount++;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    // If we failed do nothing
                }
            }

            precursor.activation =
                new ParamGroupType
                {
                    cvParam = activationCvParams.ToArray()
                };

            precursors.Add(precursor);

            // The first SPS mass seems to be the same as the one from reaction or scan filter
            for (int n = 1; n < SPSMasses.Count; n++)
            {
                var SPSPrecursor = new PrecursorType
                {
                    selectedIonList =
                        new SelectedIonListType { count = "1", selectedIon = new ParamGroupType[1] },
                    spectrumRef = spectrumRef
                };

                // Selected ion MZ only
                SPSPrecursor.selectedIonList.selectedIon[0] =
                    new ParamGroupType
                    {
                        cvParam = new CVParamType[]
                        {
                            new CVParamType
                            {
                                name = "selected ion m/z",
                                value = SPSMasses[n].ToString(),
                                accession = "MS:1000744",
                                cvRef = "MS",
                                unitCvRef = "MS",
                                unitAccession = "MS:1000040",
                                unitName = "m/z"
                            }
                        }
                    };

                // All SPS masses have the same activation (i.e. it was calculated above)
                SPSPrecursor.activation =
                    new ParamGroupType
                    {
                        cvParam = activationCvParams.ToArray()
                    };

                precursors.Add(SPSPrecursor);
            }

            //Add precursors from previous levels to the end of the list
            precursors.AddRange(prevPrecursors.Precursors);

            return new PrecursorListType
            {
                count = precursors.Count.ToString(),
                precursor = precursors.ToArray()
            };

        }

        private int GetParentFromScanString(string scanString)
        {
            var result = _filterStringIsolationMzPattern.Match(scanString);
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
                return _precursorScanNumbers[parentFilter];
            }

            return -1; //unsuccessful parsing
        }

        /// <summary>
        /// Populate the scan list element. Full version used for mass spectra,
        /// having Scan Event, scan Filter etc
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
            if (monoisotopicMass.HasValue && monoisotopicMass.Value > ZeroDelta)
            {
                scanType.userParam = new UserParamType[1];
                scanType.userParam[0] = new UserParamType
                {
                    name = "[Thermo Trailer Extra]Monoisotopic M/Z:",
                    value = monoisotopicMass.Value.ToString(CultureInfo.InvariantCulture),
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
        /// Populate the scan list element. Simple version used for PDA spectra,
        /// without Scan Event and other parameters
        /// </summary>
        /// <param name="scanNumber">the scan number</param>
        /// <param name="scan">the scan object</param>
        /// <returns></returns>
        private ScanListType ConstructScanList(int scanNumber, Scan scan)
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

            // Scan start time
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
                }
            };

            var scanType = new ScanType
            {
                cvParam = scanTypeCvParams.ToArray()
            };

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
                value = scan.ScanStatistics.ShortWavelength.ToString(CultureInfo.InvariantCulture),
                cvRef = "MS",
                unitAccession = "UO:0000018",
                unitCvRef = "UO",
                unitName = "nanometer"
            };
            scanWindow.cvParam[1] = new CVParamType
            {
                name = "scan window upper limit",
                accession = "MS:1000500",
                value = scan.ScanStatistics.LongWavelength.ToString(CultureInfo.InvariantCulture),
                cvRef = "MS",
                unitAccession = "UO:0000018",
                unitCvRef = "UO",
                unitName = "nanometer"
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
        private static byte[] Get64BitArray(ICollection<double> array)
        {
            Array bytes = Array.CreateInstance(typeof(byte), array.Count * sizeof(double));

            if (BitConverter.IsLittleEndian) //should be for most modern systems, but maybe..
            {
                Buffer.BlockCopy(array.ToArray(), 0, bytes, 0, bytes.Length); //copy the buffer directly
            }
            else //swap byte order of all elements in array
            {
                using (var memoryStream = new MemoryStream())
                {
                    foreach (var doubleValue in array)
                    {
                        var doubleValueByteArray = BitConverter.GetBytes(doubleValue);
                        Array.Reverse(doubleValueByteArray);
                        memoryStream.Write(doubleValueByteArray, 0, doubleValueByteArray.Length);
                    }

                    memoryStream.Position = 0;
                    bytes = memoryStream.ToArray();
                }
            }

            return (byte[])bytes;
        }

        /// <summary>
        /// Convert the double array into a compressed zlib byte array
        /// </summary>
        /// <param name="array">the double collection</param>
        /// <returns>the byte array</returns>
        private static byte[] GetZLib64BitArray(ICollection<double> array)
        {
            // zero length array encoded by GZip produces non-zero length array; some downstream tools do not like it
            if (array.Count == 0) return new byte[0];

            byte[] bytes = Get64BitArray(array);

            using (var memoryStream = new MemoryStream())
            using (var outZStream = new ZOutputStream(memoryStream, zlibConst.Z_DEFAULT_COMPRESSION))
            {
                outZStream.Write(bytes, 0, bytes.Length);

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