using System;
using System.IO;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParser
{
    public static class RawFileParser
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Extract the RAW file metadata and spectra in MGF format. 
        /// </summary>
        /// <param name="parseInput">the parse input object</param>
        public static void Parse(ParseInput parseInput)
        {
            // Check to see if the RAW file name was supplied as an argument to the program
            if (string.IsNullOrEmpty(parseInput.RawFilePath))
            {
                Log.Debug("No raw file specified or found in path");
                throw new Exception("No RAW file specified!");
            }

            // Check to see if the specified RAW file exists
            if (!File.Exists(parseInput.RawFilePath))
            {
                throw new Exception(@"The file doesn't exist in the specified location - " + parseInput.RawFilePath);
            }

            Log.Info("Started parsing " + parseInput.RawFilePath);

            // Create the IRawDataPlus object for accessing the RAW file
            IRawDataPlus rawFile;
            using (rawFile = RawFileReaderFactory.ReadFile(parseInput.RawFilePath))
            {
                if (!rawFile.IsOpen)
                {
                    throw new Exception("Unable to access the RAW file using the RawFileReader class!");
                }

                // Check for any errors in the RAW file
                if (rawFile.IsError)
                {
                    throw new Exception($"Error opening ({rawFile.FileError}) - {parseInput.RawFilePath}");
                }

                // Check if the RAW file is being acquired
                if (rawFile.InAcquisition)
                {
                    throw new Exception("RAW file still being acquired - " + parseInput.RawFilePath);
                }

                // Get the number of instruments (controllers) present in the RAW file and set the 
                // selected instrument to the MS instrument, first instance of it
                rawFile.SelectInstrument(Device.MS, 1);

                // Get the first and last scan from the RAW file
                var firstScanNumber = rawFile.RunHeaderEx.FirstSpectrum;
                var lastScanNumber = rawFile.RunHeaderEx.LastSpectrum;

                if (parseInput.OutputMetadata != MetadataFormat.NONE)
                {
                    var metadataWriter = new MetadataWriter(parseInput.OutputDirectory, parseInput.RawFileNameWithoutExtension);
                    switch (parseInput.OutputMetadata)
                    {
                        case MetadataFormat.JSON:
                            metadataWriter.WriteJsonMetada(rawFile, firstScanNumber, lastScanNumber);
                            break;
                        case MetadataFormat.TXT:
                            metadataWriter.WriteMetada(rawFile, firstScanNumber, lastScanNumber);
                            break;
                    }
                }

                if (parseInput.OutputFormat != OutputFormat.NONE)
                {
                    SpectrumWriter spectrumWriter;
                    switch (parseInput.OutputFormat)
                    {
                        case OutputFormat.MGF:
                            spectrumWriter = new MgfSpectrumWriter(parseInput);
                            spectrumWriter.Write(rawFile, firstScanNumber, lastScanNumber);
                            break;
                        case OutputFormat.MzML:
                        case OutputFormat.IndexMzML:
                            spectrumWriter = new MzMlSpectrumWriter(parseInput);
                            spectrumWriter.Write(rawFile, firstScanNumber, lastScanNumber);
                            break;
                        case OutputFormat.Parquet:
                            spectrumWriter = new ParquetSpectrumWriter(parseInput);
                            spectrumWriter.Write(rawFile, firstScanNumber, lastScanNumber);
                            break;
                    }
                }
                
                Log.Info("Finished parsing " + parseInput.RawFilePath);
            }
        }
    }
}