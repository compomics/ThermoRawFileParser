using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.Options;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoFisher.CommonCore.RawFileReader;

namespace ThermoRawFileParser
{
    internal class CentroidedMgfExtractor
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _rawFilePath;
        private readonly string _outputDirectory;
        private readonly string _collection;
        private readonly bool _outputMetadata;
        private readonly string _msRun;
        private readonly string _subFolder;
        private static string _rawFileName;
        private static string _rawFileNameWithoutExtension;

        public CentroidedMgfExtractor(string rawFilePath, string outputDirectory, Boolean outputMetadata,
            string collection, string msRun,
            string subFolder)
        {
            _rawFilePath = rawFilePath;
            _outputDirectory = outputDirectory;
            _outputMetadata = outputMetadata;
            _collection = collection;
            _msRun = msRun;
            _subFolder = subFolder;
        }

        /// <summary>
        /// Extract the RAW file metadata and spectra in MGF format. 
        /// </summary>
        public void Extract()
        {
            // Check to see if the RAW file name was supplied as an argument to the program
            if (string.IsNullOrEmpty(_rawFilePath))
            {
                Log.Error("No RAW file specified!");

                return;
            }

            // Check to see if the specified RAW file exists
            if (!File.Exists(_rawFilePath))
            {
                Log.Error(@"The file doesn't exist in the specified location - " + _rawFilePath);

                return;
            }
            else
            {
                string[] splittedPath = _rawFilePath.Split('/');
                _rawFileName = splittedPath[splittedPath.Length - 1];
                _rawFileNameWithoutExtension = Path.GetFileNameWithoutExtension(_rawFileName);
            }

            Log.Info("Started parsing " + _rawFilePath);

            // Create the IRawDataPlus object for accessing the RAW file
            //var rawFile = RawFileReaderAdapter.FileFactory(rawFilePath);
            IRawDataPlus rawFile;
            using (rawFile = RawFileReaderFactory.ReadFile(_rawFilePath))
            {
                if (!rawFile.IsOpen)
                {
                    Log.Error("Unable to access the RAW file using the RawFileReader class!");

                    return;
                }

                // Check for any errors in the RAW file
                if (rawFile.IsError)
                {
                    Log.Error($"Error opening ({rawFile.FileError}) - {_rawFilePath}");

                    return;
                }

                // Check if the RAW file is being acquired
                if (rawFile.InAcquisition)
                {
                    Log.Error("RAW file still being acquired - " + _rawFilePath);

                    return;
                }

                // Get the number of instruments (controllers) present in the RAW file and set the 
                // selected instrument to the MS instrument, first instance of it
                rawFile.SelectInstrument(Device.MS, 1);

                // Get the first and last scan from the RAW file
                int firstScanNumber = rawFile.RunHeaderEx.FirstSpectrum;
                int lastScanNumber = rawFile.RunHeaderEx.LastSpectrum;

                if (_outputMetadata)
                {
                    WriteMetada(rawFile, firstScanNumber, lastScanNumber);
                }

                rawFile.SelectInstrument(Device.MS, 2);

                WriteSpectraToMgf(rawFile, firstScanNumber, lastScanNumber);

                Log.Info("Finished parsing " + _rawFilePath);
            }
        }

        /// <summary>
        /// Write the RAW file metadata to file.
        /// <param name="rawFile">the RAW file object</param>
        /// <param name="firstScanNumber">the first scan number</param>
        /// <param name="lastScanNumber">the last scan number</param>
        /// </summary>
        private void WriteMetada(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            // Get the start and end time from the RAW file
            double startTime = rawFile.RunHeaderEx.StartTime;
            double endTime = rawFile.RunHeaderEx.EndTime;

            // Collect the metadata
            List<string> output = new List<string>
            {
                "RAW file=" + rawFile.FileName,
                "RAWfileversion=" + rawFile.FileHeader.Revision,
                "Creationdate=" + rawFile.FileHeader.CreationDate,
                "Operator=" + rawFile.FileHeader.WhoCreatedId,
                "Numberofinstruments=" + rawFile.InstrumentCount,
                "Description=" + rawFile.FileHeader.FileDescription,
                "Instrumentmode=" + rawFile.GetInstrumentData().Model,
                "Instrumentname=" + rawFile.GetInstrumentData().Name,
                "Serialnumber=" + rawFile.GetInstrumentData().SerialNumber,
                "Softwareversion=" + rawFile.GetInstrumentData().SoftwareVersion,
                "Firmwareversion=" + rawFile.GetInstrumentData().HardwareVersion,
                "Units=" + rawFile.GetInstrumentData().Units,
                $"Massresolution={rawFile.RunHeaderEx.MassResolution:F3}",
                $"Numberofscans={rawFile.RunHeaderEx.SpectraCount}",
                $"Scan range={firstScanNumber},{lastScanNumber}",
                $"Time range={startTime:F2},{endTime:F2}",
                $"Mass range={rawFile.RunHeaderEx.LowMass:F4},{rawFile.RunHeaderEx.HighMass:F4}"
            };

            // Write the meta data to file
            File.WriteAllLines(_outputDirectory + "/" + _rawFileNameWithoutExtension + "_metadata", output.ToArray());
        }

        /// <summary>
        /// Write the RAW files' spectra to a MGF file.
        /// </summary>
        /// <param name="rawFile">the RAW file interface</param>
        /// <param name="firstScanNumber">the first scan number</param>
        /// <param name="lastScanNumber">the last scan number</param>
        private void WriteSpectraToMgf(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber)
        {
            // Test centroid (high resolution/label) data
            using (var mgfFile =
                File.CreateText(_outputDirectory + "//" + _rawFileNameWithoutExtension + ".mgf"))
            {
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

                    // Get the ionizationMode, MS2 precursor mass, collision energy, and isolation width for each scan
                    if (scanFilter.MSOrder == ThermoFisher.CommonCore.Data.FilterEnums.MSOrderType.Ms2)
                    {
                        mgfFile.WriteLine("BEGIN IONS");
                        mgfFile.WriteLine($"TITLE={ConstructSpectrumTitle(scanNumber)}");
                        mgfFile.WriteLine($"SCAN={scanNumber}");
                        mgfFile.WriteLine($"RTINSECONDS={time * 60}");
                        // Get the reaction information for the first precursor
                        var reaction = scanEvent.GetReaction(0);
                        double precursorMass = reaction.PrecursorMass;
                        mgfFile.WriteLine($"PEPMASS={precursorMass:F7}");

                        // trailer extra data list
                        var trailerData = rawFile.GetTrailerExtraInformation(scanNumber);

                        for (int i = 0; i < trailerData.Length; i++)
                        {
                            if ((trailerData.Labels[i] == "Charge State:"))
                            {
                                if (Convert.ToInt32(trailerData.Values[i]) > 0)
                                {
                                    mgfFile.WriteLine($"CHARGE={trailerData.Values[i]}+");
                                }
                            }
                        }

                        //$"PEPMASS={precursorMass:F2} {GetPrecursorIntensity(rawFile, scanNumber)}");
                        //double collisionEnergy = reaction.CollisionEnergy;
                        //mgfFile.WriteLine($"COLLISIONENERGY={collisionEnergy}");
                        //var ionizationMode = scanFilter.IonizationMode;
                        //mgfFile.WriteLine($"IONMODE={ionizationMode}");

                        if (scan.HasCentroidStream)
                        {
                            var centroidStream = rawFile.GetCentroidStream(scanNumber, false);
                            if (scan.CentroidScan.Length > 0)
                            {
                                for (int i = 0; i < centroidStream.Length; i++)
                                {
                                    mgfFile.WriteLine(
                                        $"{centroidStream.Masses[i]:F7} {centroidStream.Intensities[i]:F10}");
                                }
                            }
                        }
                        else
                        {
                            // Get the scan statistics from the RAW file for this scan number
                            var scanStatistics = rawFile.GetScanStatsForScanNumber(scanNumber);

                            // Get the segmented (low res and profile) scan data
                            var segmentedScan = rawFile.GetSegmentedScanFromScanNumber(scanNumber, scanStatistics);
                            for (int i = 0; i < segmentedScan.Positions.Length; i++)
                            {
                                mgfFile.WriteLine(
                                    $"{segmentedScan.Positions[i]:F7} {segmentedScan.Intensities[i]:F10}");
                            }
                        }

                        mgfFile.WriteLine("END IONS");
                    }
                }
            }
        }

        /// <summary>
        /// Construct the spectrum title.
        /// </summary>
        /// <param name="scanNumber">the spectrum scan number</param>
        private String ConstructSpectrumTitle(int scanNumber)
        {
            StringBuilder spectrumTitle = new StringBuilder("mzspec:");

            if (_collection != null)
            {
                spectrumTitle.Append(_collection).Append(":");
            }

            if (_subFolder != null)
            {
                spectrumTitle.Append(_subFolder).Append(":");
            }

            if (_msRun != null)
            {
                spectrumTitle.Append(_msRun).Append(":");
            }
            else
            {
                spectrumTitle.Append(_rawFileName).Append(":");
            }

            spectrumTitle.Append("scan:");
            spectrumTitle.Append(scanNumber);

            return spectrumTitle.ToString();
        }

        /// <summary>
        /// Get the spectrum intensity.
        /// </summary>
        /// <param name="rawFile">the RAW file object</param>
        /// <param name="scanNumber">the scan number</param>
        private double GetPrecursorIntensity(IRawDataPlus rawFile, int scanNumber)
        {
            // Define the settings for getting the Base Peak chromatogram            
            ChromatogramTraceSettings settings = new ChromatogramTraceSettings(TraceType.BasePeak);

            // Get the chromatogram from the RAW file. 
            var data = rawFile.GetChromatogramData(new IChromatogramSettings[] {settings}, scanNumber - 1,
                scanNumber - 1);

            // Split the data into the chromatograms
            var trace = ChromatogramSignal.FromChromatogramData(data);

            return trace[0].Intensities[0];
        }
    }
}