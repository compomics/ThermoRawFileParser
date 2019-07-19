using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using Newtonsoft.Json;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.Writer
{
    public class MetadataWriter
    {
        private static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _outputDirectory;
        private readonly string _metadataFileName;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="outputDirectory"></param>
        /// <param name="metadataFileName"></param>
        public MetadataWriter(string outputDirectory, string metadataFileName)
        {
            _outputDirectory = outputDirectory;
            _metadataFileName = metadataFileName;
        }

        /// <summary>
        /// Write the RAW file metadata to file.
        /// <param name="rawFile">the RAW file object</param>
        /// <param name="firstScanNumber">the first scan number</param>
        /// <param name="lastScanNumber">the last scan number</param>
        /// </summary>
        public void WriteMetadata(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            // Get the start and end time from the RAW file
            var startTime = rawFile.RunHeaderEx.StartTime;
            var endTime = rawFile.RunHeaderEx.EndTime;

            // Collect the metadata
            var output = new List<string>
            {
                "#General information",
                "RAW file path=" + rawFile.FileName,
                "RAW file version=" + rawFile.FileHeader.Revision,
                "Creation date=" + rawFile.FileHeader.CreationDate,
                $"Created by=[MS, MS:1000529, created_by, {rawFile.FileHeader.WhoCreatedId}]",
                "Number of instruments=" + rawFile.InstrumentCount,
                "Description=" + rawFile.FileHeader.FileDescription,
                $"Instrument model=[MS, MS:1000494, Thermo Scientific instrument model, {rawFile.GetInstrumentData().Model}]",
                "Instrument name=" + rawFile.GetInstrumentData().Name,
                $"Instrument serial number=[MS, MS:1000529, instrument serial number, {rawFile.GetInstrumentData().SerialNumber}]",
                $"Software version=[NCIT, NCIT:C111093, Software Version, {rawFile.GetInstrumentData().SoftwareVersion}]",
                "Firmware version=" + rawFile.GetInstrumentData().HardwareVersion,
                "Units=" + rawFile.GetInstrumentData().Units,
                $"Mass resolution=[MS, MS:1000011, mass resolution, {rawFile.RunHeaderEx.MassResolution:F3}]",
                $"Number of scans={rawFile.RunHeaderEx.SpectraCount}",
                $"Scan range={firstScanNumber};{lastScanNumber}",
                $"Scan start time=[MS, MS:1000016, scan start time, {startTime:F2}]",
                $"Time range={startTime:F2};{endTime:F2}",
                $"Mass range={rawFile.RunHeaderEx.LowMass:F4};{rawFile.RunHeaderEx.HighMass:F4}",
                "",
                "#Sample information",
                "Sample name=" + rawFile.SampleInformation.SampleName,
                "Sample id=" + rawFile.SampleInformation.SampleId,
                "Sample type=" + rawFile.SampleInformation.SampleType,
                "Sample comment=" + rawFile.SampleInformation.Comment,
                "Sample vial=" + rawFile.SampleInformation.Vial,
                "Sample volume=" + rawFile.SampleInformation.SampleVolume,
                "Sample injection volume=" + rawFile.SampleInformation.InjectionVolume,
                "Sample row number=" + rawFile.SampleInformation.RowNumber,
                "Sample dilution factor=" + rawFile.SampleInformation.DilutionFactor
            };

            // Write the meta data to file
            string metadataOutputPath;
            if (_outputDirectory == null)
            {
                metadataOutputPath = _metadataFileName;
            }
            else
            {
                metadataOutputPath = _outputDirectory + "/" + _metadataFileName + "-metadata.txt";
            }

            File.WriteAllLines(metadataOutputPath, output.ToArray());

            // Write the string array to a new file named "WriteLines.txt".
            //using (var outputFile = new StreamWriter(metadataOutputPath))
            //{
            //    foreach (var line in output)
            //        outputFile.WriteLine(line);
            //}
        }

        /// <summary>
        /// Write the RAW file metadata to file.
        /// <param name="rawFile">the RAW file object</param>
        /// <param name="firstScanNumber">the first scan number</param>
        /// <param name="lastScanNumber">the last scan number</param>
        /// </summary>
        public void WriteJsonMetada(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            // Get the start and end time from the RAW file
            var startTime = rawFile.RunHeaderEx.StartTime;
            var endTime = rawFile.RunHeaderEx.EndTime;

            var metadata = new Metadata();

            /** File Properties **/
            metadata.addFileProperty(new CVTerm("NCIT:C47922", "NCIT", "Pathname", rawFile.FileName));
            metadata.addFileProperty(new CVTerm("NCIT:C25714", "NCIT", "Version",
                rawFile.FileHeader.Revision.ToString()));
            metadata.addFileProperty(new CVTerm("NCIT:C69199", "NCIT", "Content Creation Date",
                rawFile.FileHeader.CreationDate.ToString()));
            metadata.addFileProperty(new CVTerm("NCIT:C25365", "NCIT", "Description",
                rawFile.FileHeader.FileDescription));


            metadata.addScanSetting(new CVTerm("MS:1000016", "MS", "scan start time", startTime.ToString()));
            metadata.addScanSetting(new CVTerm("MS:1000011", "MS", "mass resolution",
                rawFile.RunHeaderEx.MassResolution.ToString()));
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

            metadata.addInstrumentProperty(new CVTerm("MS:1000494", "MS", "Thermo Scientific instrument model",
                rawFile.GetInstrumentData().Model));
            metadata.addInstrumentProperty(new CVTerm("MS:1000496", "MS", "instrument attribute",
                rawFile.GetInstrumentData().Name));
            metadata.addInstrumentProperty(new CVTerm("MS:1000529", "MS", "instrument serial number",
                rawFile.GetInstrumentData().SerialNumber));

            var msTypes = new Dictionary<string, int>();
            double minTime = 1000000000000000;
            double maxTime = 0;
            double minMz = 1000000000000000000;
            double maxMz = 0;
            double minCharge = 100000000000000;
            double maxCharge = 0;

            ICollection<CVTerm> fragmentationType = new HashSet<CVTerm>(CVTerm.CvTermComparer);

            for (var scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
            {
                var time = rawFile.RetentionTimeFromScanNumber(scanNumber);

                // Get the scan filter for this scan number
                var scanFilter = rawFile.GetFilterForScanNumber(scanNumber);

                // Get the scan event for this scan number
                var scanEvent = rawFile.GetScanEventForScanNumber(scanNumber);

                // Only consider MS2 spectra
                if (msTypes.ContainsKey(scanFilter.MSOrder.ToString()))
                {
                    var value = msTypes[scanFilter.MSOrder.ToString()];
                    value = value + 1;
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
                    fragmentationType.Add(parseActivationType(scanFilter.GetActivation(0)));

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
                        catch (ArgumentOutOfRangeException exception)
                        {
                            Log.Warn("No reaction found for scan " + scanNumber);
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

            if (minCharge == 100000000000000)
            {
                minCharge = 0;
            }

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

            metadata.addScanSetting(fragmentationType);

            metadata.addMSData(new CVTerm("PRIDE:0000472", "PRIDE", "MS min charge", minCharge.ToString()));
            metadata.addMSData(new CVTerm("PRIDE:0000473", "PRIDE", "MS max charge", maxCharge.ToString()));

            metadata.addMSData(new CVTerm("PRIDE:0000474", "PRIDE", "MS min RT", minTime.ToString()));
            metadata.addMSData(new CVTerm("PRIDE:0000475", "PRIDE", "MS max RT", maxTime.ToString()));

            metadata.addMSData(new CVTerm("PRIDE:0000476", "PRIDE", "MS min MZ", minMz.ToString()));
            metadata.addMSData(new CVTerm("PRIDE:0000477", "PRIDE", "MS min MZ", maxMz.ToString()));


            // Write the meta data to file
            var json = JsonConvert.SerializeObject(metadata);
            json.Replace("\r\n", "\n");

            string metadataOutputPath;
            if (_outputDirectory == null)
            {
                metadataOutputPath = _metadataFileName;
            }
            else
            {
                metadataOutputPath = _outputDirectory + "/" + _metadataFileName + "-metadata.json";
            }

            File.WriteAllText(metadataOutputPath, json);
        }

        public CVTerm parseActivationType(ActivationType activation)
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