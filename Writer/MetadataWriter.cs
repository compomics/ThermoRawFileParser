using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParser.Writer
{
    public class MetadataWriter
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private readonly string _outputDirectory;
        private readonly string _rawFileNameWithoutExtension;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="outputDirectory"></param>
        /// <param name="rawFileNameWithoutExtension"></param>
        public MetadataWriter(string outputDirectory, string rawFileNameWithoutExtension)
        {
            _outputDirectory = outputDirectory;
            _rawFileNameWithoutExtension = rawFileNameWithoutExtension;
        }

        /// <summary>
        /// Write the RAW file metadata to file.
        /// <param name="rawFile">the RAW file object</param>
        /// <param name="firstScanNumber">the first scan number</param>
        /// <param name="lastScanNumber">the last scan number</param>
        /// </summary>
        public void WriteMetada(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
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
            File.WriteAllLines(_outputDirectory + "/" + _rawFileNameWithoutExtension + "_metadata", output.ToArray());
           
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
            metadata.addFileProperty("path", rawFile.FileName);
            metadata.addFileProperty("version", rawFile.FileHeader.Revision.ToString());
            metadata.addFileProperty("creation-date", rawFile.FileHeader.CreationDate.ToString());
            metadata.addFileProperty("number-instruments", rawFile.InstrumentCount.ToString());
            metadata.addFileProperty("description", rawFile.FileHeader.FileDescription);
            
            /** Sample Properties **/
            metadata.addSampleProperty("name", rawFile.SampleInformation.SampleName);
            metadata.addSampleProperty("id", rawFile.SampleInformation.SampleId);
            metadata.addSampleProperty("type", rawFile.SampleInformation.SampleType.ToString());
            metadata.addSampleProperty("comment", rawFile.SampleInformation.Comment);
            metadata.addSampleProperty("vial", rawFile.SampleInformation.Vial);
            metadata.addSampleProperty("volume", rawFile.SampleInformation.SampleVolume.ToString()); 
            metadata.addSampleProperty("injection-volume", rawFile.SampleInformation.InjectionVolume.ToString());
            metadata.addSampleProperty("row-number", rawFile.SampleInformation.RowNumber.ToString());
            metadata.addSampleProperty("dilution-factor", rawFile.SampleInformation.DilutionFactor.ToString());
            
            metadata.addScanSetting("start-time", new CVTerm("MS:1000016", "MS", "scan start time",startTime.ToString()));
            metadata.addScanSetting("resolution", new CVTerm("MS:1000011", "MS", "mass resolution", rawFile.RunHeaderEx.MassResolution.ToString()));
            metadata.addScanSetting("tolerance-unit", new CVTerm("UO:0000002", "MS", "mass unit", rawFile.GetInstrumentData().Units.ToString()));
            metadata.addScanSetting("number-scans", new CVTerm("", "", "", rawFile.RunHeaderEx.SpectraCount.ToString()));
            metadata.addScanSetting("scan-range", new CVTerm("", "", "", firstScanNumber + ":" + lastScanNumber));
            metadata.addScanSetting("time-range", new CVTerm("","","", startTime + ":" + endTime));
            metadata.addScanSetting("mass-range", new CVTerm("", "", "", rawFile.RunHeaderEx.LowMass + ":" + rawFile.RunHeaderEx.HighMass)); 
            
            metadata.addInstrumentProperty("model", new CVTerm("MS:1000494", "MS","Thermo Scientific instrument model", rawFile.GetInstrumentData().Model));
            metadata.addInstrumentProperty("name", new CVTerm("MS:1000496", "MS","instrument attribute", rawFile.GetInstrumentData().Name));
            metadata.addInstrumentProperty("serial", new CVTerm("MS:1000529", "MS", "instrument serial number", rawFile.GetInstrumentData().SerialNumber));
            
            metadata.addMSData("ms-number", new CVTerm("","","", ""));
            
            var msTypes = new Dictionary<string, int>();
            double minTime = 1000000000000000;
            double maxTime = 0;
            double minMz = 1000000000000000000;
            double maxMz = 0;
            double minCharge = 100000000000000;
            double maxCharge = 0;

            for (var scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
            {
                var scan = Scan.FromFile(rawFile, scanNumber);
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
                }else 
                    msTypes.Add(scanFilter.MSOrder.ToString(), 1);

                if (time > maxTime)
                    maxTime = time;
                if (time < minTime)
                    minTime = time; 
                
                if (scanFilter.MSOrder == MSOrderType.Ms2)
                   {
                       if (scanEvent.ScanData == ScanDataType.Centroid || (scanEvent.ScanData == ScanDataType.Profile)){
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

                                    if (Int32.Parse(trailerData.Values[i]) > maxCharge)
                                        maxCharge = Int32.Parse(trailerData.Values[i]); 
                                    
                                    if (Int32.Parse(trailerData.Values[i]) < minCharge)
                                        maxCharge = Int32.Parse(trailerData.Values[i]); 

                                }
                            }
                           
                       }
                    }
            }

            if (minCharge == 100000000000000)
            {
                minCharge = 0;
            }
            
            
            // Write the meta data to file
            var json = JsonConvert.SerializeObject(metadata);
            json.Replace("\r\n", "\n");
            File.WriteAllText(_outputDirectory + "/" + _rawFileNameWithoutExtension + "-metadata.json", json);

        }
    }
}