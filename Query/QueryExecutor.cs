using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Mono.Options;
using Newtonsoft.Json;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParser.Query
{
    public class QueryExecutor
    {
        public static void Run(QueryParameters parameters)
        {
            // parse the scans string
            var scanIds = ParseScanIds(parameters.scans);
            parameters.scanNumbers = scanIds;

            var reader = new ProxiSpectrumReader(parameters);
            var results = reader.Retrieve();

            if (parameters.stdout)
            {
                StdOutputQueryData(results);
            }
            else
            {
                string outputFileName;

                // if outputFile has been defined, put output there.
                if (parameters.outputFile != null)
                {
                    outputFileName = Path.GetFullPath(parameters.outputFile);
                }
                // otherwise put output files into the same directory as the raw file input
                else
                {
                    outputFileName = Path.GetFullPath(parameters.rawFilePath);
                }

                var directory = Path.GetDirectoryName(outputFileName);

                outputFileName = Path.Combine(directory ?? throw new NoNullAllowedException(),
                    Path.GetFileNameWithoutExtension(outputFileName) + ".json");

                OutputQueryData(results, outputFileName);
            }
        }

        private static void OutputQueryData(List<ProxiSpectrum> outputData, string outputFileName)
        {
            var outputString = JsonConvert.SerializeObject(outputData, Formatting.Indented);
            File.WriteAllText(outputFileName, outputString);
        }


        private static void StdOutputQueryData(List<ProxiSpectrum> outputData)
        {
            var outputString = JsonConvert.SerializeObject(outputData, Formatting.Indented);
            Console.Write(outputString);
        }


        private static HashSet<int> ParseScanIds(string text)
        {
            if (text.Length == 0) throw new OptionException("Scan ID string invalid, nothing specified", null);
            foreach (var c in text)
            {
                int ic = c;
                if (!((ic == ',') || (ic == '-') || (ic == ' ') || ('0' <= ic && ic <= '9')))
                {
                    throw new OptionException("Scan ID string contains invalid character", null);
                }
            }

            var tokens = text.Split(new[] {','}, StringSplitOptions.None);

            var container = new HashSet<int>();

            for (var i = 0; i < tokens.Length; ++i)
            {
                if (tokens[i].Length == 0) throw new OptionException("Scan ID string has invalid format", null);
                var rangeBoundaries = tokens[i].Split(new[] {'-'}, StringSplitOptions.None);
                if (rangeBoundaries.Length == 1)
                {
                    int rangeStart;
                    try
                    {
                        rangeStart = Convert.ToInt32(rangeBoundaries[0]);
                    }
                    catch (Exception)
                    {
                        throw new OptionException("Scan ID string has invalid format", null);
                    }

                    container.Add(rangeStart);
                }
                else if (rangeBoundaries.Length == 2)
                {
                    int rangeStart;
                    int rangeEnd;
                    try
                    {
                        rangeStart = Convert.ToInt32(rangeBoundaries[0]);
                        rangeEnd = Convert.ToInt32(rangeBoundaries[1]);
                    }
                    catch (Exception)
                    {
                        throw new OptionException("Scan ID string has invalid format", null);
                    }

                    for (var l = rangeStart; l <= rangeEnd; ++l)
                    {
                        container.Add(l);
                    }
                }
                else throw new OptionException("Scan ID string has invalid format", null);
            }

            return container;
        }
    }
}