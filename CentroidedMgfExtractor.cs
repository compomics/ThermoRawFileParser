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
        private readonly string rawFilePath;
        private readonly string outputDirectory;
        private readonly string collection;
        private readonly bool outputMetadata;
        private readonly string msRun;
        private readonly string subFolder;
        private static string rawFileName;
        private static string rawFileNameWithoutExtension;

        public CentroidedMgfExtractor(string rawFilePath, string outputDirectory, Boolean outputMetadata,
            string collection, string msRun,
            string subFolder)
        {
            this.rawFilePath = rawFilePath;
            this.outputDirectory = outputDirectory;
            this.outputMetadata = outputMetadata;
            this.collection = collection;
            this.msRun = msRun;
            this.subFolder = subFolder;
        }

        /// <summary>
        /// Extract the RAW file metadata and spectra in MGF format. 
        /// </summary>
        public void Extract()
        {
            // Check to see if the RAW file name was supplied as an argument to the program
            if (string.IsNullOrEmpty(rawFilePath))
            {
                Console.WriteLine("No RAW file specified!");

                return;
            }

            // Check to see if the specified RAW file exists
            if (!File.Exists(rawFilePath))
            {
                Console.WriteLine(@"The file doesn't exist in the specified location - " + rawFilePath);

                return;
            }
            else
            {
                string[] splittedPath = rawFilePath.Split('/');
                rawFileName = splittedPath[splittedPath.Length - 1];
                rawFileNameWithoutExtension = Path.GetFileNameWithoutExtension(rawFileName);
            }

            Console.WriteLine("Started parsing " + rawFilePath);

            // Create the IRawDataPlus object for accessing the RAW file
            //var rawFile = RawFileReaderAdapter.FileFactory(rawFilePath);
            IRawDataPlus rawFile;
            using (rawFile = RawFileReaderFactory.ReadFile(rawFilePath))
            {
                if (!rawFile.IsOpen)
                {
                    Console.WriteLine("Unable to access the RAW file using the RawFileReader class!");

                    return;
                }

                // Check for any errors in the RAW file
                if (rawFile.IsError)
                {
                    Console.WriteLine($"Error opening ({rawFile.FileError}) - {rawFilePath}");

                    return;
                }

                // Check if the RAW file is being acquired
                if (rawFile.InAcquisition)
                {
                    Console.WriteLine("RAW file still being acquired - " + rawFilePath);

                    return;
                }

                // Get the number of instruments (controllers) present in the RAW file and set the 
                // selected instrument to the MS instrument, first instance of it
                //Console.WriteLine("The RAW file has data from {0} instruments", rawFile.InstrumentCount);

                rawFile.SelectInstrument(Device.MS, 1);

                // Get the first and last scan from the RAW file
                int firstScanNumber = rawFile.RunHeaderEx.FirstSpectrum;
                int lastScanNumber = rawFile.RunHeaderEx.LastSpectrum;

                if (outputMetadata)
                {
                    WriteMetada(rawFile, firstScanNumber, lastScanNumber);
                }

                rawFile.SelectInstrument(Device.MS, 2);

                WriteSpectraToMgf(rawFile, firstScanNumber, lastScanNumber);

                Console.WriteLine("Finished parsing " + rawFilePath);
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
                "Units=" + rawFile.GetInstrumentData().Units
            };

            output.Add($"Massresolution={rawFile.RunHeaderEx.MassResolution:F3}");
            output.Add($"Numberofscans={rawFile.RunHeaderEx.SpectraCount}");
            output.Add($"Scan range={firstScanNumber},{lastScanNumber}");
            output.Add($"Time range={startTime:F2},{endTime:F2}");
            output.Add($"Mass range={rawFile.RunHeaderEx.LowMass:F4},{rawFile.RunHeaderEx.HighMass:F4}");

            // Write the meta data to file
            File.WriteAllLines(outputDirectory + "/" + rawFileNameWithoutExtension + "_metadata", output.ToArray());
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
                File.CreateText(outputDirectory + "//" + rawFileNameWithoutExtension + ".mgf"))
            {
                for (int scanNumber = firstScanNumber; scanNumber <= lastScanNumber; scanNumber++)
                {
                    // Get each scan from the RAW file
                    var scan = Scan.FromFile(rawFile, scanNumber);

                    if (scan.HasCentroidStream)
                    {
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

                            // Get the reaction information for the first precursor
                            var reaction = scanEvent.GetReaction(0);
                            double precursorMass = reaction.PrecursorMass;
                            mgfFile.WriteLine(
                                $"PEPMASS={precursorMass:F4}");
                            //$"PEPMASS={precursorMass:F2} {GetPrecursorIntensity(rawFile, scanNumber)}");
                            double collisionEnergy = reaction.CollisionEnergy;
                            mgfFile.WriteLine($"COLLISIONENERGY={collisionEnergy}");
                            var ionizationMode = scanFilter.IonizationMode;
                            mgfFile.WriteLine($"IONMODE={ionizationMode}");

                            var centroidStream = rawFile.GetCentroidStream(scanNumber, false);
                            if (scan.CentroidScan.Length > 0)
                            {
                                for (int i = 0; i < centroidStream.Length; i++)
                                {
                                    mgfFile.WriteLine(
                                        $"{centroidStream.Masses[i]:F7} {centroidStream.Intensities[i]:F10}");
                                }
                            }

                            mgfFile.WriteLine("END IONS");
                        }
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

            if (collection != null)
            {
                spectrumTitle.Append(collection).Append(":");
            }

            if (subFolder != null)
            {
                spectrumTitle.Append(subFolder).Append(":");
            }

            if (msRun != null)
            {
                spectrumTitle.Append(msRun).Append(":");
            }
            else
            {
                spectrumTitle.Append(rawFileName).Append(":");
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