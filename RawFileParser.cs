using System;
using System.IO;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser
{
    public class RawFileParser
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

        public RawFileParser(string rawFilePath, string outputDirectory, bool outputMetadata,
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
        public void Parse()
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
                    MetadataWriter metadataWriter = new MetadataWriter(_outputDirectory, _rawFileNameWithoutExtension);
                    metadataWriter.WriteMetada(rawFile, firstScanNumber, lastScanNumber);
                }

                rawFile.SelectInstrument(Device.MS, 2);

                SpectrumWriter spectrumWriter = new MzMlSpectrumWriter(_rawFilePath, _outputDirectory,
                    _collection, _msRun, _subFolder);
                spectrumWriter.WriteSpectra(rawFile, firstScanNumber, lastScanNumber);

                Log.Info("Finished parsing " + _rawFilePath);
            }
        }
    }
}