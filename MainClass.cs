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
                    "f=|format=", "The output format for the spectra (0 for MGF, 1 for MzMl)",
                    v => outputFormatString = v
                },
                {
                    "g|gzip", "GZip the output file if this flag is specified (without value).",
                    v => gzip = v != null
                },
                {
                    "m|metadata=", "Write the metadata output file if this flag is specified (0 for JSON, 1 for TXT).",
                    v => outputMetadataString = v 
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
                
                if(outputMetadataString == null && outputFormatString == null)
                    throw new OptionException("The parameter -f or -m should be provided", "-f|--format , -m|--format");

                if (outputFormatString != null )
                {
                    var outPutFormatInt = int.Parse(outputFormatString);
              
                    if (Enum.IsDefined(typeof(OutputFormat), outPutFormatInt))
                        outputFormat = (OutputFormat) outPutFormatInt;
                    else
                        throw new OptionException("unknown output format", "-f, --format");
                    
                    if (Enum.IsDefined(typeof(OutputFormat), outPutFormatInt))
                        outputFormat = (OutputFormat) outPutFormatInt;
                    else
                        throw new OptionException("unknown output format", "-f, --format");
                }
                
                if (outputMetadataString != null)
                {
                    var metadataInt = int.Parse(outputMetadataString);
               
                    if (Enum.IsDefined(typeof(MetadataFormat), metadataInt))
                        outputMetadataFormat = (MetadataFormat) metadataInt;
                    else
                        throw new OptionException("unknown output format", "-m, --metadata");
                   
                    if (Enum.IsDefined(typeof(MetadataFormat), metadataInt))
                        outputMetadataFormat = (MetadataFormat) metadataInt;
                    else
                        throw new OptionException("unknown output format", "-f, --metadata");
                }

                if (!extra.IsNullOrEmpty())
                {
                    throw new OptionException("unexpected extra arguments", null);
                }
            }
            catch (OptionException optionException)
            {
                ShowHelp("Error - usage is (use -option=value for the optional arguments):", optionException,
                    optionSet);
            }
            catch (ArgumentNullException argumentNullException)
            {
                if(help)
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

            if (help)
            {
                const string usageMessage =
                    "ThermoRawFileParser.exe usage (use -option=value for the optional arguments)";
                ShowHelp(usageMessage, null, optionSet);
            }
            else
            {
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

                    Environment.Exit(-1);
                }
            }
        }

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