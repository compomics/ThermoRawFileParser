using System;
using System.Collections.Generic;
using System.Globalization;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.XIC
{
    public class XICRetriever
    {
        private IRawDataPlus rawFile;

        public void RetrieveXIC(ParseInput parseInput)
        {
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

                // Get the first and last scan from the RAW file
                var firstScanNumber = rawFile.RunHeaderEx.FirstSpectrum;
                var lastScanNumber = rawFile.RunHeaderEx.LastSpectrum;
                
                var components = new List<Component>();
                // foreach (var searchSetting in mList)
                // {
                //     components.Add(new Component
                //     {
                //         MassRange = new Limit
                //         {
                //             Low = searchSetting.mz - (searchSetting.mz * (searchSetting.tol / 1000000)),
                //             High = searchSetting.mz + (searchSetting.mz * (searchSetting.tol / 1000000))
                //         },
                //         RetentionTimeRange = new Limit {Low = searchSetting.ts, High = searchSetting.te},
                //         Tolerance = new MassOptions {Tolerance = searchSetting.tol, ToleranceUnits = ToleranceUnits.ppm}
                //     });
                // }
            }
        }

        private void CreateMassChromatograms(List<Component> components)
        {
            var generator = new ChromatogramBatchGenerator();
            ParallelChromatogramFactory.FromRawData(generator, rawFile);

            try
            {
                // create the array of chromatogram settings
                for (var componentCount = 0; componentCount < components.Count; componentCount++)
                {
                    IChromatogramSettings[] allSettings =
                    {
                        new ChromatogramTraceSettings(TraceType.MassRange)
                        {
                            Filter = Component.Filter,
                            MassRanges = new[]
                            {
                                new Range(components[componentCount].MassRange.Low,
                                    components[componentCount].MassRange.High)
                            }
                        }
                    };

                    var rtFilteredScans = rawFile.GetFilteredScansListByTimeRange("",
                        components[componentCount].RetentionTimeRange.Low,
                        components[componentCount].RetentionTimeRange.High);

                    var data = rawFile.GetChromatogramData(allSettings, rtFilteredScans[0],
                        rtFilteredScans[rtFilteredScans.Count - 1]);

                    var chromatogramTrace = ChromatogramSignal.FromChromatogramData(data);

                    var returnJson = "{\"request\":" + componentCount + ",\"results\":{";
                    var empty = true;
                    var times = "\"times\":[";
                    var intensities = "\"intensities\":[";

                    for (var i = 0; i < chromatogramTrace[0].Scans.Count; i++)
                    {
                        if (chromatogramTrace[0].Intensities[i] > 0)
                        {
                            empty = false;
                            times = times + chromatogramTrace[0].Times[i].ToString("0.0000000000",
                                        CultureInfo.InvariantCulture) + ",";
                            intensities = intensities + chromatogramTrace[0].Intensities[i].ToString("0.00000",
                                              CultureInfo.InvariantCulture) + ",";
                        }
                    }

                    if (!empty)
                    {
                        times = times.Remove(times.Length - 1) + "],";
                        intensities = intensities.Remove(intensities.Length - 1) + "]}";
                        returnJson = returnJson + times + intensities;
                    }
                    else
                    {
                        returnJson = returnJson + times + "]," + intensities + "]}";
                    }

                    returnJson = returnJson + "}";
                    Console.WriteLine(returnJson);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.InnerException);
            }
        }
    }

    internal class TempComponent
    {
        // Retention time range
        public Limit RetentionTimeRange { get; set; }

        // Mass range
        public Limit MassRange { get; set; }
        public const string Filter = "ms";
        public string Name { get; set; }
        public MassOptions Tolerance { get; set; }
    }
}