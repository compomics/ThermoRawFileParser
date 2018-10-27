using System;
using Mono.Options;
using ThermoFisher.CommonCore.Data;

namespace ThermoRawFileParser
{
    public static class MainClass
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args)
        {
            string rawFilePath = null;
            string outputDirectory = null;
            string outputFormatString = null;
            var outputFormat = OutputFormat.NON;
            var gzip = false;
            string outputMetadataString = null;
            var outputMetadataFormat = MetadataFormat.NON;
            var includeProfileData = false;
            string collection = null;
            string msRun = null;
            string subFolder = null;
            var help = false;

            var optionSet = new OptionSet
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
                    "o=|output=", "The output directory.",
                    v => outputDirectory = v
                },
                {
                    "f=|format=", "The output format for the spectra (0 for MGF, 1 for MzMl, 2 for Parquet)",
                    v => outputFormatString = v
                },
                {
                    "m=|metadata=", "The metadata output format (0 for JSON, 1 for TXT).",
                    v => outputMetadataString = v
                },
                {
                    "g|gzip", "GZip the output file if this flag is specified (without value).",
                    v => gzip = v != null
                },
                {
                    "p|profiledata",
                    "Exclude MS2 profile data if this flag is specified (without value). Only for MGF format!",
                    v => includeProfileData = v != null
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
                }
            };

            try
            {
                //parse the command line
                var extra = optionSet.Parse(args);

                if (!extra.IsNullOrEmpty())
                {
                    throw new OptionException("unexpected extra arguments", null);
                }

                if (help)
                {
                    ShowHelp(" usage is (use -option=value for the optional arguments):", null,
                        optionSet);
                    return;
                }

                if (outputMetadataString == null && outputFormatString == null)
                {
                    throw new OptionException("The parameter -f or -m should be provided",
                        "-f|--format , -m|--format");
                }

                if (outputFormatString != null)
                {
                    int outPutFormatInt;
                    try
                    {
                        outPutFormatInt = int.Parse(outputFormatString);
                    }
                    catch (FormatException e)
                    {
                        throw new OptionException("unknown output format value (0 for MGF, 1 for MzMl, 2 for Parquet)",
                            "-f, --format");
                    }

                    if (Enum.IsDefined(typeof(OutputFormat), outPutFormatInt) &&
                        ((OutputFormat) outPutFormatInt) != OutputFormat.NON)
                    {
                        outputFormat = (OutputFormat) outPutFormatInt;
                    }
                    else
                    {
                        throw new OptionException("unknown output format value (0 for MGF, 1 for MzMl, 2 for Parquet)",
                            "-f, --format");
                    }
                }

                if (outputMetadataString != null)
                {
                    int metadataInt;
                    try
                    {
                        metadataInt = int.Parse(outputMetadataString);
                    }
                    catch (FormatException e)
                    {
                        throw new OptionException("unknown metadata format value (0 for JSON, 1 for TXT)",
                            "-m, --metadata");
                    }

                    if (Enum.IsDefined(typeof(MetadataFormat), metadataInt) &&
                        ((MetadataFormat) metadataInt) != MetadataFormat.NON)
                    {
                        outputMetadataFormat = (MetadataFormat) metadataInt;
                    }
                    else
                    {
                        throw new OptionException("unknown metadata format value (0 for JSON, 1 for TXT)",
                            "-m, --metadata");
                    }
                }
            }
            catch (OptionException optionException)
            {
                ShowHelp("Error - usage is (use -option=value for the optional arguments):", optionException,
                    optionSet);
            }
            catch (ArgumentNullException argumentNullException)
            {
                if (help)
                {
                    ShowHelp(" usage is (use -option=value for the optional arguments):", null,
                        optionSet);
                }
                else
                {
                    ShowHelp("Error - usage is (use -option=value for the optional arguments):", null,
                        optionSet);
                }
            }

            try
            {
                var parseInput = new ParseInput(rawFilePath, outputDirectory, outputFormat, gzip,
                    outputMetadataFormat,
                    includeProfileData, collection, msRun, subFolder);
                RawFileParser.Parse(parseInput);
            }
            catch (Exception ex)
            {
                Log.Error("An unexpected error occured:");
                Log.Error(ex.ToString());

                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Show the help message
        /// </summary>
        /// <param name="message">the help message</param>
        /// <param name="optionException">the option exception, can be null</param>
        /// <param name="optionSet">the option set object</param>
        private static void ShowHelp(string message, OptionException optionException, OptionSet optionSet)
        {
            if (optionException != null)
            {
                if (!optionException.OptionName.IsNullOrEmpty())
                {
                    Console.Error.Write(optionException.OptionName + ": ");
                }

                Console.Error.WriteLine(optionException.Message);
            }

            Console.Error.WriteLine(message);
            optionSet.WriteOptionDescriptions(Console.Error);
            Environment.Exit(-1);
        }
    }
}