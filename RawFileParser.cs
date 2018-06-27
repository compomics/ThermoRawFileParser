using System;
using System.IO;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParser
{
    public class RawFileParser
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ParseInput _parseInput;

        public RawFileParser(ParseInput parseInput)
        {
            _parseInput = parseInput;
        }

        /// <summary>
        /// Extract the RAW file metadata and spectra in MGF format. 
        /// </summary>
        public void Parse()
        {
            // Check to see if the RAW file name was supplied as an argument to the program
            if (string.IsNullOrEmpty(_parseInput.RawFilePath))
            {
                Log.Error("No RAW file specified!");

                return;
            }

            // Check to see if the specified RAW file exists
            if (!File.Exists(_parseInput.RawFilePath))
            {
                Log.Error(@"The file doesn't exist in the specified location - " + _parseInput.RawFilePath);

                return;
            }

            Log.Info("Started parsing " + _parseInput.RawFilePath);

            // Create the IRawDataPlus object for accessing the RAW file
            //var rawFile = RawFileReaderAdapter.FileFactory(rawFilePath);
            IRawDataPlus rawFile;
            using (rawFile = RawFileReaderFactory.ReadFile(_parseInput.RawFilePath))
            {
                if (!rawFile.IsOpen)
                {
                    Log.Error("Unable to access the RAW file using the RawFileReader class!");

                    return;
                }

                // Check for any errors in the RAW file
                if (rawFile.IsError)
                {
                    Log.Error($"Error opening ({rawFile.FileError}) - {_parseInput.RawFilePath}");

                    return;
                }

                // Check if the RAW file is being acquired
                if (rawFile.InAcquisition)
                {
                    Log.Error("RAW file still being acquired - " + _parseInput.RawFilePath);

                    return;
                }

                // Get the number of instruments (controllers) present in the RAW file and set the 
                // selected instrument to the MS instrument, first instance of it
                rawFile.SelectInstrument(Device.MS, 1);

                // Get the first and last scan from the RAW file
                int firstScanNumber = rawFile.RunHeaderEx.FirstSpectrum;
                int lastScanNumber = rawFile.RunHeaderEx.LastSpectrum;

                if (_parseInput.OutputMetadata)
                {
                    MetadataWriter metadataWriter = new MetadataWriter(_parseInput.OutputDirectory,
                        _parseInput.RawFileNameWithoutExtension);
                    metadataWriter.WriteMetada(rawFile, firstScanNumber, lastScanNumber);
                }

                SpectrumWriter spectrumWriter = null;
                switch (_parseInput.OutputFormat)
                {
                    case OutputFormat.Mgf:
                        spectrumWriter = new MgfSpectrumWriter(_parseInput);
                        break;
                    case OutputFormat.Mzml:
                        spectrumWriter = new MzMLSpectrumWriter(_parseInput);
                        break;
                }
                spectrumWriter.WriteSpectra(rawFile, firstScanNumber, lastScanNumber);

                Log.Info("Finished parsing " + _parseInput.RawFilePath);
            }
        }
    }
}