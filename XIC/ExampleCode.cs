using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Mono.Options;
using Newtonsoft.Json;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.XIC
{
    public class ExampleCode
    {
        private static IRawDataPlus _rawFile;
        private static string _json;
        private static string _fileName;
        private static string _jsonFile;

        private static void Example(string[] args)
        {
            var optionSet = new OptionSet
            {
                {
                    "f=|file=", "The raw file.",
                    v => _fileName = v
                },
                {
                    "jf=|jsonfile=", "The json file.",
                    v => _jsonFile = v
                },
                {
                    "j=|json=", "Json.",
                    v => _json = v
                }
            };

            // parse the command line arguments
            var extra = optionSet.Parse(args);

            List<SearchSettings> mList;
            if (_jsonFile != null)
            {
                mList = JsonConvert.DeserializeObject<List<SearchSettings>>(File.ReadAllText(_jsonFile));
            }
            else if (_json != null)
            {
                mList = JsonConvert.DeserializeObject<List<SearchSettings>>(_json);
            }
            else
            {
                Console.WriteLine("no json or jsonfile was passed, aborting");
                return;
            }

            var myThreadManager = RawFileReaderFactory.CreateThreadManager(_fileName);
            _rawFile = myThreadManager.CreateThreadAccessor();

            var components = new List<Component>(mList.Count);
            foreach (var searchSetting in mList)
            {
                components.Add(new Component
                {
                    MassRange = new Limit
                    {
                        Low = searchSetting.mz - (searchSetting.mz * (searchSetting.tol / 1000000)),
                        High = searchSetting.mz + (searchSetting.mz * (searchSetting.tol / 1000000))
                    },
                    RetentionTimeRange = new Limit {Low = searchSetting.ts, High = searchSetting.te},
                    Tolerance = new MassOptions {Tolerance = searchSetting.tol, ToleranceUnits = ToleranceUnits.ppm}
                });
            }

            CreateMassChromatograms(components);
        }

        private static void CreateMassChromatograms(List<Component> components)
        {
            _rawFile.SelectInstrument(Device.MS, 1);

            var generator = new ChromatogramBatchGenerator();
            ParallelChromatogramFactory.FromRawData(generator, _rawFile);

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

                    var rtFilteredScans = _rawFile.GetFilteredScansListByTimeRange("",
                        components[componentCount].RetentionTimeRange.Low,
                        components[componentCount].RetentionTimeRange.High);

                    var data = _rawFile.GetChromatogramData(allSettings, rtFilteredScans[0],
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

    internal class Limit
    {
        public double Low { get; set; }
        public double High { get; set; }
        
        public bool help = false;
        public string rawFilePath = null;
        public string rawDirectoryPath = null;
        public string outputFile = null;
        public string outputDirectory = null;
        public bool base64 = false;
        
        
        
    }

    internal class Component
    {
        // Retention time range
        public Limit RetentionTimeRange { get; set; }

        // Mass range
        public Limit MassRange { get; set; }
        public const string Filter = "ms";
        public string Name { get; set; }
        public MassOptions Tolerance { get; set; }
    }

    internal class SearchSettings
    {
        // Mz value
        public float mz { get; set; }

        // Start time in minutes
        public float ts { get; set; }

        // End time minutes
        public float te { get; set; }

        // Mass tolerance in PPM
        public float tol { get; set; }
    }

    internal class LocalChromatogramDelivery : IChromatogramDelivery
    {
        //
        // Summary:
        //     Gets or sets the chromatogram data
        public ChromatogramSignal DeliveredSignal { get; set; }

        //
        // Summary:
        //     Gets or sets the "request" which determines what kind of chromatogram is needed.
        public IChromatogramRequest Request { get; set; }

        //
        // Summary:
        //     Implements the "Process" interface, as just saving a reference to the data.
        //
        // Parameters:
        //   signal:
        //     The chromatogram which has been generated
        public void Process(ChromatogramSignal signal)
        {
            signal.Intensities.Where(x => x != 0).ToList();
        }
    }
}
