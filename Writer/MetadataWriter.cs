using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.WebSockets;
using log4net;
using Newtonsoft.Json;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.Writer
{
    public class MetadataWriter
    {
        private static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ParseInput _parseInput;
        private string _metadataFileName;
        private double minTime = 1000000000000000;
        private double maxTime = 0;
        private double minMz = 1000000000000000000;
        private double maxMz = 0;
        private double minCharge = 100000000000000;
        private double maxCharge = 0;
        private readonly Dictionary<string, int> msTypes = new Dictionary<string, int>();
        private readonly ICollection<CVTerm> fragmentationTypes = new HashSet<CVTerm>(CVTerm.CvTermComparer);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parseInput"></param>
        public MetadataWriter(ParseInput parseInput)
        {
            _parseInput = parseInput;
        }

        /// <summary>
        /// Write the RAW file metadata to file.
        /// <param name="rawFile">the RAW file object</param>
        /// <param name="firstScanNumber">the first scan number</param>
        /// <param name="lastScanNumber">the last scan number</param>
        /// </summary>
        public void WriteMetadata(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            if (rawFile.SelectMsData())
            {
                for (var scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
                {
                    var time = rawFile.RetentionTimeFromScanNumber(scanNumber);

                    // Get the scan filter for this scan number
                    var scanFilter = rawFile.GetFilterForScanNumber(scanNumber);

                    // Get the scan event for this scan number
                    var scanEvent = rawFile.GetScanEventForScanNumber(scanNumber);

                    // Keep track of the number of MS<MS level> spectra
                    if (msTypes.ContainsKey(scanFilter.MSOrder.ToString()))
                    {
                        var value = msTypes[scanFilter.MSOrder.ToString()];
                        value += 1;
                        msTypes[scanFilter.MSOrder.ToString()] = value;
                    }
                    else
                        msTypes.Add(scanFilter.MSOrder.ToString(), 1);

                    if (time > maxTime)
                        maxTime = time;
                    if (time < minTime)
                        minTime = time;

                    if (scanFilter.MSOrder == MSOrderType.Ms2)
                    {
                        fragmentationTypes.Add(ParseActivationType(scanFilter.GetActivation(0)));

                        if (scanEvent.ScanData == ScanDataType.Centroid || (scanEvent.ScanData == ScanDataType.Profile))
                        {
                            try
                            {
                                var reaction = scanEvent.GetReaction(0);
                                var precursorMass = reaction.PrecursorMass;
                                if (precursorMass > maxMz)
                                    maxMz = precursorMass;
                                if (precursorMass < minMz)
                                    minMz = precursorMass;
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                Log.Warn("No reaction found for scan " + scanNumber);
                                _parseInput.NewWarn();
                            }

                            // trailer extra data list
                            var trailerData = rawFile.GetTrailerExtraInformation(scanNumber);
                            for (var i = 0; i < trailerData.Length; i++)
                            {
                                if (trailerData.Labels[i] == "Charge State:")
                                {
                                    if (int.Parse(trailerData.Values[i]) > maxCharge)
                                        maxCharge = int.Parse(trailerData.Values[i]);

                                    if (int.Parse(trailerData.Values[i]) < minCharge)
                                        minCharge = int.Parse(trailerData.Values[i]);
                                }
                            }
                        }
                    }
                }
            }

            

            if (minCharge == 100000000000000)
            {
                minCharge = 0;
            }

            switch (_parseInput.MetadataFormat)
            {
                case MetadataFormat.JSON:
                    _metadataFileName = _parseInput.MetadataOutputFile != null
                        ? _parseInput.MetadataOutputFile
                        : Path.Combine(_parseInput.OutputDirectory, _parseInput.RawFileNameWithoutExtension) +
                          "-metadata.json";
                    WriteJsonMetada(rawFile, firstScanNumber, lastScanNumber);
                    break;
                case MetadataFormat.TXT:
                    _metadataFileName = _parseInput.MetadataOutputFile != null
                        ? _parseInput.MetadataOutputFile
                        : Path.Combine(_parseInput.OutputDirectory, _parseInput.RawFileNameWithoutExtension) +
                          "-metadata.txt";
                    WriteTextMetadata(rawFile, firstScanNumber, lastScanNumber);
                    break;
            }
        }

        /// <summary>
        /// Write the RAW file metadata to file.
        /// <param name="rawFile">the RAW file object</param>
        /// <param name="firstScanNumber">the first scan number</param>
        /// <param name="lastScanNumber">the last scan number</param>
        /// </summary>
        private void WriteJsonMetada(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            var metadata = new Metadata();

            // File Properties
            metadata.addFileProperty(new CVTerm("NCIT:C47922", "NCIT", "Pathname", rawFile.FileName));
            metadata.addFileProperty(new CVTerm("NCIT:C25714", "NCIT", "Version",
                rawFile.FileHeader.Revision.ToString()));
            metadata.addFileProperty(new CVTerm("NCIT:C69199", "NCIT", "Content Creation Date",
                rawFile.FileHeader.CreationDate.ToString()));
            if (!rawFile.FileHeader.FileDescription.IsNullOrEmpty())
            {
                metadata.addFileProperty(new CVTerm("NCIT:C25365", "NCIT", "Description",
                    rawFile.FileHeader.FileDescription));
            }

            // Instrument Properties
            if (rawFile.SelectMsData())
            {
                metadata.addInstrumentProperty(new CVTerm("MS:1000494", "MS", "Thermo Scientific instrument model",
                rawFile.GetInstrumentData().Model));
                metadata.addInstrumentProperty(new CVTerm("MS:1000496", "MS", "instrument attribute",
                    rawFile.GetInstrumentData().Name));
                metadata.addInstrumentProperty(new CVTerm("MS:1000529", "MS", "instrument serial number",
                    rawFile.GetInstrumentData().SerialNumber));
                metadata.addInstrumentProperty(new CVTerm("NCIT:C111093", "NCIT", "Software Version",
                    rawFile.GetInstrumentData().SoftwareVersion));
                if (!rawFile.GetInstrumentData().HardwareVersion.IsNullOrEmpty())
                {
                    metadata.addInstrumentProperty(new CVTerm("AFR:0001259", "AFO", "firmware version",
                        rawFile.GetInstrumentData().HardwareVersion));
                }
            }

            // MS Data
            if (rawFile.SelectMsData())
            {
                foreach (KeyValuePair<string, int> entry in msTypes)
                {
                    if (entry.Key.Equals(MSOrderType.Ms.ToString()))
                        metadata.addMSData(new CVTerm("PRIDE:0000481", "PRIDE", "Number of MS1 spectra",
                            entry.Value.ToString()));
                    if (entry.Key.Equals(MSOrderType.Ms2.ToString()))
                        metadata.addMSData(new CVTerm("PRIDE:0000482", "PRIDE", "Number of MS2 spectra",
                            entry.Value.ToString()));
                    if (entry.Key.Equals(MSOrderType.Ms3.ToString()))
                        metadata.addMSData(new CVTerm("PRIDE:0000483", "PRIDE", "Number of MS3 spectra",
                            entry.Value.ToString()));
                }

                metadata.addMSData(new CVTerm("PRIDE:0000472", "PRIDE", "MS min charge",
                    minCharge.ToString(CultureInfo.InvariantCulture)));
                metadata.addMSData(new CVTerm("PRIDE:0000473", "PRIDE", "MS max charge",
                    maxCharge.ToString(CultureInfo.InvariantCulture)));

                metadata.addMSData(new CVTerm("PRIDE:0000474", "PRIDE", "MS min RT",
                    minTime.ToString(CultureInfo.InvariantCulture)));
                metadata.addMSData(new CVTerm("PRIDE:0000475", "PRIDE", "MS max RT",
                    maxTime.ToString(CultureInfo.InvariantCulture)));

                metadata.addMSData(new CVTerm("PRIDE:0000476", "PRIDE", "MS min MZ",
                    minMz.ToString(CultureInfo.InvariantCulture)));
                metadata.addMSData(new CVTerm("PRIDE:0000477", "PRIDE", "MS max MZ",
                    maxMz.ToString(CultureInfo.InvariantCulture)));

                // Scan Settings
                // Get the start and end time from the RAW file
                var startTime = rawFile.RunHeaderEx.StartTime;
                var endTime = rawFile.RunHeaderEx.EndTime;
                metadata.addScanSetting(new CVTerm("MS:1000016", "MS", "scan start time",
                    startTime.ToString(CultureInfo.InvariantCulture)));
                metadata.addScanSetting(new CVTerm("MS:1000011", "MS", "mass resolution",
                    rawFile.RunHeaderEx.MassResolution.ToString(CultureInfo.InvariantCulture)));
                metadata.addScanSetting(new CVTerm("UO:0000002", "MS", "mass unit",
                    rawFile.GetInstrumentData().Units.ToString()));
                metadata.addScanSetting(new CVTerm("PRIDE:0000478", "PRIDE", "Number of scans",
                    rawFile.RunHeaderEx.SpectraCount.ToString()));
                metadata.addScanSetting(new CVTerm("PRIDE:0000479", "PRIDE", "MS scan range",
                    firstScanNumber + ":" + lastScanNumber));
                metadata.addScanSetting(new CVTerm("PRIDE:0000484", "PRIDE", "Retention time range",
                    startTime + ":" + endTime));
                metadata.addScanSetting(new CVTerm("PRIDE:0000485", "PRIDE", "Mz range",
                    rawFile.RunHeaderEx.LowMass + ":" + rawFile.RunHeaderEx.HighMass));
                metadata.addScanSetting(fragmentationTypes);
            }

            // Sample Data
            if (!rawFile.SampleInformation.SampleName.IsNullOrEmpty())
            {
                metadata.addSampleProperty(new CVTerm("MS:1000002", "MS", "sample name",
                    rawFile.SampleInformation.SampleName));
            }

            if (!rawFile.SampleInformation.SampleId.IsNullOrEmpty())
            {
                metadata.addSampleProperty(new CVTerm("MS:1000001", "MS", "sample number",
                    rawFile.SampleInformation.SampleId));
            }

            if (!rawFile.SampleInformation.SampleType.ToString().IsNullOrEmpty() &&
                !rawFile.SampleInformation.SampleType.ToString().Equals("Unknown"))
            {
                metadata.addSampleProperty(new CVTerm("NCIT:C25284", "NCIT", "Type",
                    rawFile.SampleInformation.SampleType.ToString()));
            }

            if (!rawFile.SampleInformation.Comment.IsNullOrEmpty())
            {
                metadata.addSampleProperty(new CVTerm("NCIT:C25393", "NCIT", "Comment",
                    rawFile.SampleInformation.Comment));
            }

            if (!rawFile.SampleInformation.Vial.IsNullOrEmpty())
            {
                metadata.addSampleProperty(new CVTerm("NCIT:C41275", "NCIT", "Vial",
                    rawFile.SampleInformation.Vial));
            }

            if (rawFile.SampleInformation.SampleVolume != 0)
            {
                metadata.addSampleProperty(new CVTerm("MS:1000005", "MS", "sample volume",
                    rawFile.SampleInformation.SampleVolume.ToString(CultureInfo.InvariantCulture)));
            }

            if (rawFile.SampleInformation.InjectionVolume != 0)
            {
                metadata.addSampleProperty(new CVTerm("AFR:0001577", "AFO", "injection volume setting",
                    rawFile.SampleInformation.InjectionVolume.ToString(CultureInfo.InvariantCulture)));
            }

            if (rawFile.SampleInformation.RowNumber != 0)
            {
                metadata.addSampleProperty(new CVTerm("NCIT:C43378", "NCIT", "Row",
                    rawFile.SampleInformation.RowNumber.ToString()));
            }

            if (rawFile.SampleInformation.DilutionFactor != 0)
            {
                metadata.addSampleProperty(new CVTerm("AFQ:0000178", "AFO", "dilution factor",
                    rawFile.SampleInformation.DilutionFactor.ToString(CultureInfo.InvariantCulture)));
            }

            if (!rawFile.SampleInformation.InstrumentMethodFile.IsNullOrEmpty())
            {
                metadata.addSampleProperty(new CVTerm("AFR:0002045", "AFO", "device acquisition method", rawFile.SampleInformation.InstrumentMethodFile));
            }

            if (rawFile.SampleInformation.IstdAmount != 0)
            {
                metadata.addSampleProperty(new CVTerm("", "", "internal standard amount", rawFile.SampleInformation.IstdAmount.ToString()));
            }

            if (!rawFile.SampleInformation.CalibrationLevel.IsNullOrEmpty())
            {
                metadata.addSampleProperty(new CVTerm("AFR:0001849", "AFO", "calibration level", rawFile.SampleInformation.CalibrationLevel));
            }

            if (!rawFile.SampleInformation.ProcessingMethodFile.IsNullOrEmpty())
            {
                metadata.addSampleProperty(new CVTerm("AFR:0002175", "AFO", "data processing method", rawFile.SampleInformation.ProcessingMethodFile));
            }

            if (rawFile.SampleInformation.SampleWeight != 0)
            {
                metadata.addSampleProperty(new CVTerm("AFR:0001982", "AFO", "sample weight", rawFile.SampleInformation.SampleWeight.ToString()));
            }

            string[] userLabels = rawFile.UserLabel;
            string[] userTexts = rawFile.SampleInformation.UserText;
            if (!userLabels.IsNullOrEmpty() && !userTexts.IsNullOrEmpty())
            {
                for (int i = 0; i < userLabels.Length; i++)
                {
                    if (i < userTexts.Length && !userTexts[i].IsNullOrEmpty())
                    {
                        metadata.addSampleProperty(new CVTerm("", "", userLabels[i], userTexts[i]));
                    }
                }
            }

            // Write the meta data to file
            var json = JsonConvert.SerializeObject(metadata);
            json.Replace("\r\n", "\n");

            File.WriteAllText(_metadataFileName, json);
        }

        /// <summary>
        /// Write the RAW file metadata to file.
        /// <param name="rawFile">the RAW file object</param>
        /// <param name="firstScanNumber">the first scan number</param>
        /// <param name="lastScanNumber">the last scan number</param>
        /// </summary>
        private void WriteTextMetadata(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            // File Properties
            var output = new List<string>
            {
                "#FileProperties",
                "RAW file path=" + rawFile.FileName,
                "RAW file version=" + rawFile.FileHeader.Revision,
                "Creation date=" + rawFile.FileHeader.CreationDate
            };
            if (!rawFile.FileHeader.FileDescription.IsNullOrEmpty())
            {
                output.Add("Description=" + rawFile.FileHeader.FileDescription);
            }

            // Instrument Properties
            if (rawFile.SelectMsData())
            {
                output.Add("#InstrumentProperties");
                output.AddRange(new List<string>
                {
                    $"Instrument model=[MS, MS:1000494, Thermo Scientific instrument model, {rawFile.GetInstrumentData().Model}]",
                    "Instrument name=" + rawFile.GetInstrumentData().Name,
                    $"Instrument serial number=[MS, MS:1000529, instrument serial number, {rawFile.GetInstrumentData().SerialNumber}]",
                    $"Software version=[NCIT, NCIT:C111093, Software Version, {rawFile.GetInstrumentData().SoftwareVersion}]",
                }
                );
                if (!rawFile.GetInstrumentData().HardwareVersion.IsNullOrEmpty())
                {
                    output.Add("Firmware version=" + rawFile.GetInstrumentData().HardwareVersion);
                }
            }

            // MS Data
            if (rawFile.SelectMsData())
            {
                output.Add("#MsData");
                foreach (KeyValuePair<string, int> entry in msTypes)
                {
                    if (entry.Key.Equals(MSOrderType.Ms.ToString()))
                        output.Add("Number of MS1 spectra=" + entry.Value);
                    if (entry.Key.Equals(MSOrderType.Ms2.ToString()))
                        output.Add("Number of MS2 spectra=" + entry.Value);
                    if (entry.Key.Equals(MSOrderType.Ms3.ToString()))
                        output.Add("Number of MS3 spectra=" + entry.Value);
                }

                output.AddRange(new List<string>
                    {
                        "MS min charge=" + minCharge.ToString(CultureInfo.InvariantCulture),
                        "MS max charge=" + maxCharge.ToString(CultureInfo.InvariantCulture),
                        $"MS min RT={minTime.ToString(CultureInfo.InvariantCulture)}",
                        $"MS max RT={maxTime.ToString(CultureInfo.InvariantCulture)}",
                        $"MS min MZ={minMz.ToString(CultureInfo.InvariantCulture)}",
                        $"MS max MZ={maxMz.ToString(CultureInfo.InvariantCulture)}"
                    }
                );

                // Scan Settings
                // Get the start and end time from the RAW file
                var startTime = rawFile.RunHeaderEx.StartTime;
                var endTime = rawFile.RunHeaderEx.EndTime;
                output.AddRange(new List<string>
                    {
                        "#ScanSettings",
                        $"Scan start time={startTime.ToString(CultureInfo.InvariantCulture)}",
                        $"Mass resolution=[MS, MS:1000011, mass resolution, {rawFile.RunHeaderEx.MassResolution.ToString(CultureInfo.InvariantCulture)}]",
                        "Units=" + rawFile.GetInstrumentData().Units,
                        $"Number of scans={rawFile.RunHeaderEx.SpectraCount}",
                        $"Scan range={firstScanNumber};{lastScanNumber}",
                        $"Time range={startTime.ToString(CultureInfo.InvariantCulture)};{endTime.ToString(CultureInfo.InvariantCulture)}",
                        $"Mass range={rawFile.RunHeaderEx.LowMass.ToString(CultureInfo.InvariantCulture)};{rawFile.RunHeaderEx.HighMass.ToString(CultureInfo.InvariantCulture)}",
                        "Fragmentation types=" + String.Join(", ", fragmentationTypes.Select(f => f.value))
                    }
                );
            }

            // Sample Data
            output.Add("#SampleData");

            if (!rawFile.SampleInformation.SampleName.IsNullOrEmpty())
            {
                output.Add("Sample name=" + rawFile.SampleInformation.SampleName);
            }

            if (!rawFile.SampleInformation.SampleId.IsNullOrEmpty())
            {
                output.Add("Sample id=" + rawFile.SampleInformation.SampleId);
            }

            if (!rawFile.SampleInformation.SampleType.ToString().IsNullOrEmpty() &&
                !rawFile.SampleInformation.SampleType.ToString().Equals("Unknown"))
            {
                output.Add("Sample type=" + rawFile.SampleInformation.SampleType);
            }

            if (!rawFile.SampleInformation.Comment.IsNullOrEmpty())
            {
                output.Add("Sample comment=" + rawFile.SampleInformation.Comment);
            }

            if (!rawFile.SampleInformation.Vial.IsNullOrEmpty())
            {
                output.Add("Sample vial=" + rawFile.SampleInformation.Vial);
            }

            if (rawFile.SampleInformation.SampleVolume != 0)
            {
                output.Add("Sample volume=" + rawFile.SampleInformation.SampleVolume);
            }

            if (rawFile.SampleInformation.InjectionVolume != 0)
            {
                output.Add("Sample injection volume=" + rawFile.SampleInformation.InjectionVolume);
            }

            if (rawFile.SampleInformation.RowNumber != 0)
            {
                output.Add("Sample row number=" + rawFile.SampleInformation.RowNumber);
            }

            if (rawFile.SampleInformation.DilutionFactor != 0)
            {
                output.Add("Sample dilution factor=" + rawFile.SampleInformation.DilutionFactor);
            }

            if (rawFile.SampleInformation.IstdAmount != 0)
            {
                output.Add("Internal standard amount=" + rawFile.SampleInformation.IstdAmount);
            }

            if (!rawFile.SampleInformation.CalibrationLevel.IsNullOrEmpty())
            {
                output.Add("Calibration level=" + rawFile.SampleInformation.CalibrationLevel);
            }

            if (!rawFile.SampleInformation.InstrumentMethodFile.IsNullOrEmpty())
            {
                output.Add("Device acquisition method=" + rawFile.SampleInformation.InstrumentMethodFile);
            }

            if (rawFile.SampleInformation.SampleWeight != 0)
            {
                output.Add("Sample weight=" + rawFile.SampleInformation.SampleWeight);
            }

            if (!rawFile.SampleInformation.ProcessingMethodFile.IsNullOrEmpty())
            {
                output.Add("Data processing method=" + rawFile.SampleInformation.ProcessingMethodFile);
            }

            string[] userLabels = rawFile.UserLabel;
            string[] userTexts = rawFile.SampleInformation.UserText;
            if (!userLabels.IsNullOrEmpty() && !userTexts.IsNullOrEmpty())
            {
                for (int i = 0; i < userLabels.Length; i++)
                {
                    if (i < userTexts.Length && !userTexts[i].IsNullOrEmpty())
                    {
                        output.Add(userLabels[i] + "=" + userTexts[i]);
                    }
                }
            }

            // Write the meta data to file
            File.WriteAllLines(_metadataFileName, output.ToArray());
        }

        private static CVTerm ParseActivationType(ActivationType activation)
        {
            string word = activation.ToString();
            if (word == "CollisionInducedDissociation")
                return new CVTerm("MS:1000133", "MS", "collision-induced dissociation", "CID");
            if (word == "MultiPhotonDissociation")
                return new CVTerm("MS:1000435", "MS", "photodissociation", "MPD");
            if (word == "ElectronCaptureDissociation")
                return new CVTerm("MS:1000250", "MS", "electron capture dissociation", "ECD");
            if (word == "ElectronTransferDissociation" || word == "NegativeElectronTransferDissociation")
                return new CVTerm("MS:1000598", "MS", "electron transfer dissociation", "ETD");
            if (word == "HigherEnergyCollisionalDissociation")
                return new CVTerm("MS:1000422", "MS", "beam-type collision-induced dissociation", "HCD");
            if (word == "PQD")
                return new CVTerm("MS:1000599", "MS", "pulsed q dissociation", "PQD");
            return new CVTerm("MS:1000044", "MS", "dissociation method", word);
        }
    }
}