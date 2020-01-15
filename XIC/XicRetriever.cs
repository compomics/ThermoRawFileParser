using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.XIC
{
    public class XicRetriever
    {
        private const string MsFilter = "ms";

        public static void RetrieveXic(string rawFilePath, bool base64, XicData xicData)
        {
            IRawDataPlus rawFile;
            using (rawFile = RawFileReaderFactory.ReadFile(rawFilePath))
            {
                if (!rawFile.IsOpen)
                {
                    throw new RawFileParserException("Unable to access the RAW file using the native Thermo library.");
                }

                // Check for any errors in the RAW file
                if (rawFile.IsError)
                {
                    throw new RawFileParserException(
                        $"Error opening ({rawFile.FileError}) - {rawFilePath}");
                }

                // Check if the RAW file is being acquired
                if (rawFile.InAcquisition)
                {
                    throw new RawFileParserException("RAW file still being acquired - " + rawFilePath);
                }

                // Get the number of instruments (controllers) present in the RAW file and set the 
                // selected instrument to the MS instrument, first instance of it
                rawFile.SelectInstrument(Device.MS, 1);

                // Get the first and last scan from the RAW file
                //var firstScanNumber = rawFile.RunHeaderEx.FirstSpectrum;
                //var lastScanNumber = rawFile.RunHeaderEx.LastSpectrum;

                var generator = new ChromatogramBatchGenerator();
                ParallelChromatogramFactory.FromRawData(generator, rawFile);

                foreach (var xicUnit in xicData.content)
                {
                    IChromatogramSettings[] settings =
                    {
                        new ChromatogramTraceSettings(TraceType.MassRange)
                        {
                            Filter = MsFilter,
                            MassRanges = new[]
                            {
                                new Range(xicUnit.Meta.MzStart,
                                    xicUnit.Meta.MzEnd)
                            }
                        }
                    };

                    var rtFilteredScans = rawFile.GetFilteredScansListByTimeRange(string.Empty,
                        xicUnit.Meta.RtStart, xicUnit.Meta.RtEnd);

                    var data = rawFile.GetChromatogramData(settings, rtFilteredScans[0],
                        rtFilteredScans[rtFilteredScans.Count - 1]);

                    var chromatogramTrace = ChromatogramSignal.FromChromatogramData(data);

                    if (chromatogramTrace[0].Scans.Count != 0)
                    {
                        if (!base64)
                        {
                            xicUnit.X = chromatogramTrace[0].Times;
                            xicUnit.Y = chromatogramTrace[0].Intensities;
                        }
                        else
                        {
                            xicUnit.X = GetBase64String(chromatogramTrace[0].Times);
                            xicUnit.Y = GetBase64String(chromatogramTrace[0].Intensities);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Convert the double array into a base64 string
        /// </summary>
        /// <param name="array">the double collection</param>
        /// <returns>the base64 string</returns>
        private static string GetBase64String(IEnumerable<double> array)
        {
            byte[] bytes;

            using (var memoryStream = new MemoryStream())
            {
                foreach (var doubleValue in array)
                {
                    var doubleValueByteArray = BitConverter.GetBytes(doubleValue);
                    memoryStream.Write(doubleValueByteArray, 0, doubleValueByteArray.Length);
                }

                memoryStream.Position = 0;
                bytes = memoryStream.ToArray();
            }

            return BitConverter.ToString(bytes);
        }
    }
}