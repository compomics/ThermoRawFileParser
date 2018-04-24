using System;
using System.Collections.Generic;
using Mono.Options;
using ThermoFisher.CommonCore.Data;

namespace ThermoRawFileParser
{
    public class MainClass
    {       
        public static void Main(string[] args)
        {
            string rawFilePath = null;
            string outputDirectory = null;
            Boolean outputMetadata = false;
            string collection = null;
            string msRun = null;
            string subFolder = null;
            Boolean help = false;

            var optionSet = new OptionSet()
            {
                {
                    "h|help", "Prints out the options.",
                    h => help = h != null
                },
                {
                    "i=|input=", "The raw file input.",
                    v => rawFilePath = v
                },
                {
                    "o=|output=", "The metadata and mgf output directory.",
                    v => outputDirectory = v
                },
                {
                    "m|metadata", "Write the metadata output file if this flag is specified (without value).",
                    v => outputMetadata = v != null
                },                
                {
                    "c:|collection", "The optional collection identifier (PXD identifier for example).",
                    v => collection = v
                },
                {
                    "r:|run:",
                    "The optional mass spectrometry run name used in the spectrum title. The RAW file name will be used if not specified.",
                    v => msRun = v
                },
                {
                    "s:|subfolder:",
                    "Optional, to disambiguate instances where the same collection has 2 or more MS runs with the same name.",
                    v => subFolder = v
                },
            };
            
            try
            {
                List<string> extra;
                //parse the command line
                extra = optionSet.Parse(args);

                if (!extra.IsNullOrEmpty())
                {
                    throw new OptionException("unexpected extra arguments", "N/A");
                }
            }
            catch (OptionException)
            {
                ShowHelp("Error - usage is (use -option=value for the optional arguments):", optionSet);
            }

            if (help)
            {
                const string usageMessage = "ThermoRawFileParser.exe usage (use -option=value for the optional arguments)";
                ShowHelp(usageMessage, optionSet);
            }
            else
            {
                try
                {
                    CentroidedMgfExtractor centroidedMgfExtractor =
                        new CentroidedMgfExtractor(rawFilePath, outputDirectory, outputMetadata, collection, msRun, subFolder);
                    centroidedMgfExtractor.Extract();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("An unexpected error occured: " + ex.Message);
                    Environment.Exit(-1);
                }
            }
        }

        private static void ShowHelp(string message, OptionSet optionSet)
        {
            Console.Error.WriteLine(message);
            optionSet.WriteOptionDescriptions(Console.Error);
            Environment.Exit(-1);
        }
    }
}