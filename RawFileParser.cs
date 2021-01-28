using System;
using System.IO;
using System.Linq;
using ThermoFisher.CommonCore.Data;
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
        /// Process and extract the RAW file(s). 
        /// </summary>
        /// <param name="parseInput">the parse input object</param>
        public static void Parse(ParseInput parseInput)
        {
            // Input raw folder mode
            if (parseInput.RawDirectoryPath != null)
            {
                Log.Info("Started analyzing folder " + parseInput.RawDirectoryPath);

                var rawFilesPath = Directory
                    .EnumerateFiles(parseInput.RawDirectoryPath, "*", SearchOption.TopDirectoryOnly)
                    .Where(s => s.ToLower().EndsWith("raw")).ToArray();
                Log.Info(String.Format("The folder contains {0} RAW files", rawFilesPath.Length));

                if (rawFilesPath.Length == 0)
                {
                    Log.Debug("No raw files found in folder");
                    throw new RawFileParserException("No raw files found in folder!");
                }

                foreach (var filePath in rawFilesPath)
                {
                    parseInput.RawFilePath = filePath;
                    Log.Info("Started parsing " + parseInput.RawFilePath);
                    TryProcessFile(parseInput);                    
                }
            }
            // Input raw file mode
            else
            {
                Log.Info("Started parsing " + parseInput.RawFilePath);

                TryProcessFile(parseInput);
            }
        }

        /// <summary>
        /// Process and extract the given RAW file and catch IO exceptions.
        /// </summary>
        /// <param name="parseInput">the parse input object</param>
        private static void TryProcessFile(ParseInput parseInput)
        {
            try
            {
                ProcessFile(parseInput);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(!ex.Message.IsNullOrEmpty()
                    ? ex.Message
                    : "Attempting to write to an unauthorized location.");
            }
            catch (Exception ex)
            {
                if (ex is RawFileParserException)
                {
                    Log.Error(ex.Message);
                }
                else
                {
                    Log.Error("An unexpected error occured while parsing file:" + parseInput.RawFilePath);
                    Log.Error(ex.ToString());
                }
            }
        }

        /// <summary>
        /// Process and extract the given RAW file. 
        /// </summary>
        /// <param name="parseInput">the parse input object</param>
        private static void ProcessFile(ParseInput parseInput)
        {
            // Create the IRawDataPlus object for accessing the RAW file
            IRawDataPlus rawFile;
            using (rawFile = RawFileReaderFactory.ReadFile(parseInput.RawFilePath))
            {
                if (!rawFile.IsOpen)
                {
                    throw new RawFileParserException("Unable to access the RAW file using the native Thermo library.");
                }

                // Check for any errors in the RAW file
                if (rawFile.IsError)
                {
                    throw new RawFileParserException($"Error opening ({rawFile.FileError}) - {parseInput.RawFilePath}");
                }

                // Check if the RAW file is being acquired
                if (rawFile.InAcquisition)
                {
                    throw new RawFileParserException("RAW file still being acquired - " + parseInput.RawFilePath);
                }

                // Get the number of instruments (controllers) present in the RAW file and set the 
                // selected instrument to the MS instrument, first instance of it
                rawFile.SelectInstrument(Device.MS, 1);

                rawFile.IncludeReferenceAndExceptionData = parseInput.ExData;

                // Get the first and last scan from the RAW file
                var firstScanNumber = rawFile.RunHeaderEx.FirstSpectrum;
                var lastScanNumber = rawFile.RunHeaderEx.LastSpectrum;

                if (parseInput.MetadataFormat != MetadataFormat.NONE)
                {
                    MetadataWriter metadataWriter;
                    if (parseInput.MetadataOutputFile != null)
                    {
                        metadataWriter = new MetadataWriter(null, parseInput.MetadataOutputFile);
                    }
                    else
                    {
                        metadataWriter = new MetadataWriter(parseInput.OutputDirectory,
                            parseInput.RawFileNameWithoutExtension);
                    }

                    switch (parseInput.MetadataFormat)
                    {
                        case MetadataFormat.JSON:
                            metadataWriter.WriteJsonMetada(rawFile, firstScanNumber, lastScanNumber);
                            break;
                        case MetadataFormat.TXT:
                            metadataWriter.WriteMetadata(rawFile, firstScanNumber, lastScanNumber);
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