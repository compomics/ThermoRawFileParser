using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using log4net;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Util;
using Range = ThermoFisher.CommonCore.Data.Business.Range;

namespace ThermoRawFileParser.XIC
{
    public class XicReader
    {
        private static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string MsFilter = "ms";

        public static void ReadXic(string rawFilePath, bool base64, XicData xicData, ref XicParameters parameters)
        {
            IRawDataPlus rawFile;
            int _xicCount = 0;
            string _userProvidedPath = rawFilePath;

            //checking for symlinks
            var fileInfo = new FileInfo(rawFilePath);
            if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint)) //detected path is a symlink
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var realPath = NativeMethods.GetFinalPathName(rawFilePath);
                    Log.DebugFormat("Detected reparse point, real path: {0}", realPath);
                    rawFilePath = realPath;
                }
                else //Mono should handle all non-windows platforms
                {
                    var realPath = Path.Combine(Path.GetDirectoryName(rawFilePath), Mono.Unix.UnixPath.ReadLink(rawFilePath));
                    Log.DebugFormat("Detected reparse point, real path: {0}", realPath);
                    rawFilePath = realPath;
                }
            }

            using (rawFile = RawFileReaderFactory.ReadFile(rawFilePath))
            {
                Log.Info($"Started parsing {_userProvidedPath}");

                if (!rawFile.IsOpen)
                {
                    throw new RawFileParserException("Unable to access the RAW file using the native Thermo library.");
                }

                // Check for any errors in the RAW file
                if (rawFile.IsError)
                {
                    throw new RawFileParserException(
                        $"RAW file cannot be processed because of an error - {rawFile.FileError}");
                }

                // Check if the RAW file is being acquired
                if (rawFile.InAcquisition)
                {
                    throw new RawFileParserException("RAW file cannot be processed since it is still being acquired");
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

                // Update global metadata
                xicData.OutputMeta.base64 = base64;
                xicData.OutputMeta.timeunit = "minutes";


                System.Timers.Timer tick = new System.Timers.Timer(2000);
                if (!parameters.stdout)
                {
                    tick.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) => Console.Out.Write("\rCompleted XIC {0} of {1}", _xicCount, xicData.Content.Count);
                    tick.Start();
                }

                    foreach (var xicUnit in xicData.Content)
                {
                    IChromatogramSettings settings = null;
                    if (!xicUnit.Meta.MzStart.HasValue && !xicUnit.Meta.MzEnd.HasValue)
                    {
                        settings = new ChromatogramTraceSettings()
                        {
                            Filter = xicUnit.Meta.Filter ?? "ms"
                        };
                    }

                    if (!xicUnit.Meta.MzStart.HasValue)
                    {
                        xicUnit.Meta.MzStart = minMass;
                    }

                    if (!xicUnit.Meta.MzEnd.HasValue)
                    {
                        xicUnit.Meta.MzEnd = maxMass;
                    }

                    if (settings == null)
                    {
                        settings = new ChromatogramTraceSettings(TraceType.MassRange)
                        {
                            Filter = xicUnit.Meta.Filter ?? "ms",
                            MassRanges = new[]
                            {
                                new Range(xicUnit.Meta.MzStart.Value,
                                    xicUnit.Meta.MzEnd.Value)
                            }
                        };
                    }

                    List<int> rtFilteredScans = null;
                    if (!xicUnit.Meta.RtStart.HasValue && !xicUnit.Meta.RtEnd.HasValue)
                    {
                        rtFilteredScans = new List<int>();
                    }

                    if (!xicUnit.Meta.RtStart.HasValue)
                    {
                        xicUnit.Meta.RtStart = startTime;
                    }

                    if (!xicUnit.Meta.RtEnd.HasValue)
                    {
                        xicUnit.Meta.RtEnd = endTime;
                    }

                    IChromatogramData data = null;
                    if (rtFilteredScans == null)
                    {
                        rtFilteredScans = rawFile.GetFilteredScansListByTimeRange(MsFilter,
                            xicUnit.Meta.RtStart.Value, xicUnit.Meta.RtEnd.Value);

                        if (rtFilteredScans.Count != 0)
                        {
                            data = GetChromatogramData(rawFile, settings, rtFilteredScans[0],
                                rtFilteredScans[rtFilteredScans.Count - 1]);
                            if (data != null && data.PositionsArray.Length == 1 && data.PositionsArray[0].Length == 1 &&
                                (Math.Abs(data.PositionsArray[0][0] - startTime) < 0.001 ||
                                 Math.Abs(data.PositionsArray[0][0] - endTime) < 0.001))
                            {
                                Log.Warn(
                                    $"Only the minimum or maximum retention time was returned. " +
                                    $"Does the provided retention time range [{xicUnit.Meta.RtStart}-{xicUnit.Meta.RtEnd}] lies outside the max. window [{startTime}-{endTime}]?");
                                parameters.NewWarn();
                            }
                        }
                        else
                        {
                            Log.Warn(
                                $"No scans found in retention time range [{xicUnit.Meta.RtStart}-{xicUnit.Meta.RtEnd}]. " +
                                $"Does the provided retention time window lies outside the max. window [{startTime}-{endTime}]");
                            parameters.NewWarn();
                        }
                    }
                    else
                    {
                        try
                        {
                            data = GetChromatogramData(rawFile, settings, firstScanNumber, lastScanNumber);
                        }

                        catch (Exception ex)
                        {
                            Log.Error($"Cannot produce XIC using {xicUnit.GetMeta()} - {ex.Message}\nDetails:\n{ex.StackTrace}");
                            parameters.NewError();
                        }
                    }

                    if (data != null)
                    {
                        var chromatogramTrace = ChromatogramSignal.FromChromatogramData(data);
                        if (chromatogramTrace[0].Scans.Count != 0)
                        {
                            if (!base64)
                            {
                                xicUnit.RetentionTimes = chromatogramTrace[0].Times;
                                xicUnit.Intensities = chromatogramTrace[0].Intensities;
                            }
                            else
                            {
                                xicUnit.RetentionTimes = GetBase64String(chromatogramTrace[0].Times);
                                xicUnit.Intensities = GetBase64String(chromatogramTrace[0].Intensities);
                            }
                        }
                        else
                        {
                            Log.Warn($"Empty XIC returned by {xicUnit.GetMeta()}");
                            parameters.NewWarn();
                        }
                    }

                    _xicCount++;
                }

                if (!parameters.stdout)
                {
                    tick.Stop();
                    Console.Out.Write("\r");
                }

                Log.Info($"Finished parsing {_userProvidedPath}");
            }
        }

        private static IChromatogramData GetChromatogramData(IRawDataPlus rawFile, IChromatogramSettings settings,
            int firstScanNumber, int lastScanNumber)
        {
            IChromatogramData data = null;
            try
            {
                data = rawFile.GetChromatogramData(new IChromatogramSettings[] {settings}, firstScanNumber,
                    lastScanNumber);
            }
            catch (InvalidFilterFormatException)
            {
                throw new RawFileParserException($"Invalid filter string \"{settings.Filter}\"");
                
            }
            catch (InvalidFilterCriteriaException)
            {
                throw new RawFileParserException($"Invalid filter string \"{settings.Filter}\"");
            }

            return data;
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