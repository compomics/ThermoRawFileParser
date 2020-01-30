using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.XIC
{
    public class XicReader
    {
        public static void ReadXic(string rawFilePath, bool base64, XicData xicData)
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
                var firstScanNumber = rawFile.RunHeaderEx.FirstSpectrum;
                var lastScanNumber = rawFile.RunHeaderEx.LastSpectrum;

                // Get the start and end time from the RAW file
                var startTime = rawFile.RunHeaderEx.StartTime;
                var endTime = rawFile.RunHeaderEx.EndTime;

                // Get the mass range from the RAW file
                var minMass = rawFile.RunHeaderEx.LowMass;
                var maxMass = rawFile.RunHeaderEx.HighMass;

                //var generator = new ChromatogramBatchGenerator();
                //ParallelChromatogramFactory.FromRawData(generator, rawFile);

                //update global metadata
                xicData.outputmeta.base64 = base64;
                xicData.outputmeta.timeunit = "minutes";

                foreach (var xicUnit in xicData.content)
                {
                    IChromatogramSettings settings;
                    if (xicUnit.Meta.MzStart >= 0.0 && xicUnit.Meta.MzEnd >= 0.0)
                    {
                        settings = new ChromatogramTraceSettings(TraceType.MassRange)
                        {
                            Filter = xicUnit.Meta.Filter ?? "ms",
                            MassRanges = new[]
                            {
                                new Range(xicUnit.Meta.MzStart,
                                    xicUnit.Meta.MzEnd)
                            }
                        };
                    }
                    else
                    {
                        if (xicUnit.Meta.MzStart < 0.0 && xicUnit.Meta.MzEnd < 0.0)
                        {
                            settings = new ChromatogramTraceSettings();
                        }
                        else if (xicUnit.Meta.MzStart < 0.0)
                        {
                            settings = new ChromatogramTraceSettings(TraceType.MassRange)
                            {
                                Filter = xicUnit.Meta.Filter ?? "ms",
                                MassRanges = new[]
                                {
                                    new Range(minMass,
                                        xicUnit.Meta.MzEnd)
                                }
                            };
                        }
                        else
                        {
                            settings = new ChromatogramTraceSettings(TraceType.MassRange)
                            {
                                Filter = xicUnit.Meta.Filter ?? "ms",
                                MassRanges = new[]
                                {
                                    new Range(xicUnit.Meta.MzStart,
                                        maxMass)
                                }
                            };
                        }
                    }

                    List<int> rtFilteredScans;
                    if (xicUnit.Meta.RtStart >= 0.0 && xicUnit.Meta.RtEnd >= 0.0)
                    {
                        rtFilteredScans = rawFile.GetFilteredScansListByTimeRange(string.Empty,
                            xicUnit.Meta.RtStart, xicUnit.Meta.RtEnd);
                    }
                    else
                    {
                        if (xicUnit.Meta.RtStart < 0.0 && xicUnit.Meta.RtEnd < 0.0)
                        {
                            rtFilteredScans = new List<int>();
                        }
                        else if (xicUnit.Meta.RtStart < 0.0)
                        {
                            rtFilteredScans = rawFile.GetFilteredScansListByTimeRange(string.Empty,
                                startTime, xicUnit.Meta.RtEnd);
                        }
                        else
                        {
                            rtFilteredScans = rawFile.GetFilteredScansListByTimeRange(string.Empty,
                                xicUnit.Meta.RtStart, endTime);
                        }
                    }

                    IChromatogramData data;
                    if (!rtFilteredScans.IsNullOrEmpty())
                    {
                        data = rawFile.GetChromatogramData(new IChromatogramSettings[] {settings}, rtFilteredScans[0],
                            rtFilteredScans[rtFilteredScans.Count - 1]);
                    }
                    else
                    {
                        data = rawFile.GetChromatogramData(new IChromatogramSettings[] {settings}, firstScanNumber,
                            lastScanNumber);
                    }

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

            return Convert.ToBase64String(bytes);
        }
    }
}
