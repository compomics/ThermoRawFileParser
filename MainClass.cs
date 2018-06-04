using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Options;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;

namespace ThermoRawFileParser
{
    public class MainClass
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args)
        {
            string rawFilePath = null;
            string outputDirectory = null;
            bool outputMetadata = false;
            string collection = null;
            string msRun = null;
            string subFolder = null;
            bool help = false;

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
                const string usageMessage =
                    "ThermoRawFileParser.exe usage (use -option=value for the optional arguments)";
                ShowHelp(usageMessage, optionSet);
            }
            else
            {
                try
                {
                    RawFileParser rawFileParser
                        = new RawFileParser(rawFilePath, outputDirectory, outputMetadata, collection, msRun,
                            subFolder);
                    rawFileParser.Parse();
                }
                catch (Exception ex)
                {
                    Log.Error("An unexpected error occured:");
                    Log.Error(ex.ToString());

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