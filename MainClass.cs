using System;
using System.IO;
using log4net;
using log4net.Core;
using System.Collections.Generic;
using Mono.Options;
using ThermoFisher.CommonCore.Data;
using System.Linq;
using ThermoRawFileParser.Query;
using ThermoRawFileParser.XIC;

namespace ThermoRawFileParser
{
    public static class MainClass
    {
        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string Version = "1.1.11 ";

        public static void Main(string[] args)
        {
            // introduce subcommand for xics and spectra query
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "xic":
                        XicParametersParsing(args.Skip(1).ToArray()); // skip first command
                        break;

                    // if we want more subcommands, we can introduce here different cases
                    // case "subdomain whatever": break;

                    case "query":
                        SpectrumQueryParametersParsing(args.Skip(1).ToArray());
                        break;
                        
                        
                    default:
                        RegularParametersParsing(args);
                        break;
                }
            }
            else
            {
                RegularParametersParsing(args);
            }
        }
        
        private static void XicParametersParsing(string[] args)
        {
            XicParameters parameters = new XicParameters();
            string singleFile = null;
            string fileDirectory = null;
            
            var optionSet = new OptionSet
            {
                {
                    "h|help", "Prints out the options.",
                    h => parameters.help = h != null
                },
                {
                    "i=|input=", "The raw file input (Required).",
                    v => singleFile = v
                },
                {
                    "d=|input_directory=",
                    "The directory containing the raw files (Required). Specify this or an input raw file -i.",
                    v => fileDirectory = v
                },
                {
                    "j=|json=",
                    "The json input file (Required).",
                    v => parameters.jsonFilePath = v
                },
                {
                    "p|print_example",
                    "Printing an examplarily json input file.",
                    v => parameters.printJsonExample = v != null
                },
                {
                    "o=|output=",
                    "The output directory. Specify this or an output file -b. Specifying neither writes to the input directory.",
                    v => parameters.outputDirectory = v
                },
                {
                    "b|base64",
                    "Encodes the content of the xic vectors as base 64 encoded string.",
                    v => parameters.base64 = v != null
                }
            };

            try
            {
                // parse the command line
                var extra = optionSet.Parse(args);

                if (!extra.IsNullOrEmpty())
                {
                    throw new OptionException("unexpected extra arguments", null);
                }


                if (parameters.help)
                {
                    ShowHelp("usage is (use -option=value for the optional arguments):", null,
                        optionSet);
                    return;
                }

                if (parameters.printJsonExample)
                {
                    string example_json =
                        "[\n  {\n    \"mz\":673.363,\n    \"tolerance\":10,\n    \"tolerance_unit\": \"ppm\",\n  },\n  {\n    \"mz\":867.345,\n    \"tolerance\": 0.02,\n    \"tolerance_unit\": \"da\",\n    \"rt_start\":87.56,\n    \"rt_end\":99.56\n  }\n]";

                    Console.WriteLine(example_json);
                    return;
                }


                if (singleFile != null && !File.Exists(singleFile))
                {
                    throw new OptionException(
                        "specify a valid RAW file location",
                        "-i, --input");
                }

                if (fileDirectory != null && !Directory.Exists(fileDirectory))
                {
                    throw new OptionException(
                        "specify a valid input directory",
                        "-d, --input_directory");
                }


                if (parameters.jsonFilePath == null)
                {
                    throw new OptionException(
                        "specify an json input file. If you are not sure about the structure of the json file, use -p for printing an examplarily json input file",
                        "-j, --json");
                }


                if (parameters.jsonFilePath != null && !File.Exists(parameters.jsonFilePath))
                {
                    throw new OptionException(
                        "specify a valid json file location",
                        "-j, --json");
                }


                if (parameters.outputDirectory != null && !Directory.Exists(parameters.outputDirectory))
                {
                    throw new OptionException(
                        "specify a valid output location",
                        "-o, --output");
                }

                if ((singleFile == null && fileDirectory == null) || (singleFile != null && fileDirectory != null))
                {
                    throw new OptionException(
                        "specify either an input file or an input directory",
                        "-i, --input xor -d, --input_directory");
                }

                if (singleFile != null)
                {
                    parameters.rawFileList.Add(Path.GetFullPath(singleFile));
                }
                else
                {
                    DirectoryInfo d = new DirectoryInfo(Path.GetFullPath(fileDirectory));
                    FileInfo[] Files = d.GetFiles("*", SearchOption.TopDirectoryOnly)
                        .Where(f => f.Extension.ToLower() == ".raw").ToArray<FileInfo>();
                    foreach (FileInfo file in Files)
                    {
                        parameters.rawFileList.Add(Path.GetFullPath(file.Name));
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
                if (parameters.help)
                {
                    ShowHelp("usage is (use -option=value for the optional arguments):", null,
                        optionSet);
                }
                else
                {
                    ShowHelp("Error - usage is (use -option=value for the optional arguments):", null,
                        optionSet);
                }
            }

            var exitCode = 1;
            try
            {
                // execute the xic commands
                XicExecutor executor = new XicExecutor(parameters);
                exitCode = executor.run();
            }
            catch (Exception ex)
            {
                if (ex is RawFileParserException)
                {
                    Log.Error(ex.Message);
                }
                else
                {
                    Log.Error("An unexpected error occured:");
                    Log.Error(ex.ToString());
                }
            }
            finally
            {
                Environment.Exit(exitCode);
            }
        }

        private static void SpectrumQueryParametersParsing(string[] args)
        {
            
            QueryParameters parameters = new QueryParameters();
            var optionSet = new OptionSet
            {
                {
                    "h|help", "Prints out the options.",
                    h => parameters.help = h != null
                },
                {
                    "i=|input=", "The raw file input (Required).",
                    v => parameters.rawFile = v
                },
                {
                    "s=|scans=",
                    "The scan ",
                    v => parameters.scans = v
                },
                {
                    "p|noPeakPicking",
                    "Don't use the peak picking provided by the native Thermo library. By default peak picking is enabled.",
                    v => parameters.noPeakPicking = v != null
                },
            };
        }

        private static void RegularParametersParsing(string[] args)
        {
            var help = false;
            var version = false;
            string rawFilePath = null;
            string rawDirectoryPath = null;
            string outputDirectory = null;
            string outputFile = null;
            string outputFormatString = null;
            var outputFormat = OutputFormat.NONE;
            string outputMetadataString = null;
            var outputMetadataFormat = MetadataFormat.NONE;
            string metadataOutputFile = null;
            var gzip = false;
            var noPeakPicking = false;
            var noZlibCompression = false;
            var logFormat = LogFormat.DEFAULT;
            var ignoreInstrumentErrors = false;
            string s3url = null;
            string s3AccessKeyId = null;
            string s3SecretAccessKey = null;
            string logFormatString = null;
            string bucketName = null;


            var optionSet = new OptionSet
            {
                {
                    "h|help", "Prints out the options.",
                    h => help = h != null
                },
                {
                    "version", "Prints out the library version.",
                    v => version = v != null
                },
                {
                    "i=|input=", "The raw file input (Required). Specify this or an input directory -d.",
                    v => rawFilePath = v
                },
                {
                    "d=|input_directory=",
                    "The directory containing the raw files (Required). Specify this or an input raw file -i.",
                    v => rawDirectoryPath = v
                },
                {
                    "o=|output=",
                    "The output directory. Specify this or an output file -b. Specifying neither writes to the input directory.",
                    v => outputDirectory = v
                },
                {
                    "b=|output_file",
                    "The output file. Specify this or an output directory -o. Specifying neither writes to the input directory.",
                    v => outputFile = v
                },
                {
                    "f=|format=",
                    "The spectra output format: 0 for MGF, 1 for mzML, 2 for indexed mzML, 3 for Parquet. Defaults to mzML if no format is specified.",
                    v => outputFormatString = v
                },
                {
                    "m=|metadata=", "The metadata output format: 0 for JSON, 1 for TXT.",
                    v => outputMetadataString = v
                },
                {
                    "c=|metadata_output_file",
                    "The metadata output file. By default the metadata file is written to the output directory.",
                    v => metadataOutputFile = v
                },
                {
                    "g|gzip", "GZip the output file.",
                    v => gzip = v != null
                },
                {
                    "p|noPeakPicking",
                    "Don't use the peak picking provided by the native Thermo library. By default peak picking is enabled.",
                    v => noPeakPicking = v != null
                },
                {
                    "z|noZlibCompression",
                    "Don't use zlib compression for the m/z ratios and intensities. By default zlib compression is enabled.",
                    v => noZlibCompression = v != null
                },
                {
                    "l=|logging=", "Optional logging level: 0 for silent, 1 for verbose.",
                    v => logFormatString = v
                },
                {
                    "e|ignoreInstrumentErrors", "Ignore missing properties by the instrument.",
                    v => ignoreInstrumentErrors = v != null
                },
                {
                    "u:|s3_url:",
                    "Optional property to write directly the data into S3 Storage.",
                    v => s3url = v
                },
                {
                    "k:|s3_accesskeyid:",
                    "Optional key for the S3 bucket to write the file output.",
                    v => s3AccessKeyId = v
                },
                {
                    "t:|s3_secretaccesskey:",
                    "Optional key for the S3 bucket to write the file output.",
                    v => s3SecretAccessKey = v
                },
                {
                    "n:|s3_bucketName:",
                    "S3 bucket name",
                    v => bucketName = v
                }
            };

            try
            {
                // parse the command line
                var extra = optionSet.Parse(args);

                if (!extra.IsNullOrEmpty())
                {
                    throw new OptionException("unexpected extra arguments", null);
                }

                if (help)
                {
                    ShowHelp("usage is (use -option=value for the optional arguments):", null,
                        optionSet);
                    return;
                }

                if (version)
                {
                    Console.WriteLine(Version);
                    return;
                }

                if (rawFilePath == null && rawDirectoryPath == null)
                {
                    throw new OptionException(
                        "specify an input file or an input directory",
                        "-i, --input or -d, --input_directory");
                }

                if (rawFilePath != null && rawDirectoryPath != null)
                {
                    throw new OptionException(
                        "specify an input file or an input directory, not both",
                        "-i, --input or -d, --input_directory");
                }

                if (rawFilePath != null && !File.Exists(rawFilePath))
                {
                    throw new OptionException(
                        "specify a valid RAW file location",
                        "-i, --input");
                }

                if (rawDirectoryPath != null && !Directory.Exists(rawDirectoryPath))
                {
                    throw new OptionException(
                        "specify a valid input directory",
                        "-d, --input_directory");
                }

                if (outputFile == null && outputDirectory == null)
                {
                    if (rawFilePath != null)
                    {
                        outputDirectory = Path.GetDirectoryName(rawFilePath);
                    }
                    else if (rawDirectoryPath != null)
                    {
                        outputDirectory = rawDirectoryPath;
                    }
                }

                if (outputFile != null && outputDirectory != null)
                {
                    throw new OptionException(
                        "specify an output directory or an output file, not both",
                        "-o, --output or -b, --output_file");
                }

                if (outputFile != null && Directory.Exists(outputFile))
                {
                    throw new OptionException(
                        "specify a valid output file, not a directory",
                        "-b, --output_file");
                }

                if (outputFile != null && rawDirectoryPath != null)
                {
                    throw new OptionException(
                        "when using an input directory, specify an output directory instead of an output file",
                        "-o, --output instead of -b, --output_file");
                }

                if (outputDirectory != null && !Directory.Exists(outputDirectory))
                {
                    throw new OptionException(
                        "specify a valid output directory",
                        "-o, --output");
                }

                if (outputMetadataString == null && outputFormatString == null)
                {
                    outputFormat = OutputFormat.MzML;
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
                        throw new OptionException(
                            "unknown output format value (0 for MGF, 1 for mzML, 2 for indexed mzML, 3 for Parquet)",
                            "-f, --format");
                    }

                    if (Enum.IsDefined(typeof(OutputFormat), outPutFormatInt) &&
                        ((OutputFormat) outPutFormatInt) != OutputFormat.NONE)
                    {
                        outputFormat = (OutputFormat) outPutFormatInt;
                    }
                    else
                    {
                        throw new OptionException(
                            "unknown output format value (0 for MGF, 1 for mzML, 2 for indexed mzML, 3 for Parquet)",
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
                        ((MetadataFormat) metadataInt) != MetadataFormat.NONE)
                    {
                        outputMetadataFormat = (MetadataFormat) metadataInt;
                    }
                    else
                    {
                        throw new OptionException("unknown metadata format value (0 for JSON, 1 for TXT)",
                            "-m, --metadata");
                    }
                }

                if (metadataOutputFile != null && Directory.Exists(metadataOutputFile))
                {
                    throw new OptionException(
                        "specify a valid metadata output file, not a directory",
                        "-c, --metadata_output_file");
                }

                if (metadataOutputFile != null && outputMetadataFormat == MetadataFormat.NONE)
                {
                    throw new OptionException("specify a metadata format (0 for JSON, 1 for TXT)",
                        "-m, --metadata");
                }

                if (metadataOutputFile != null && rawDirectoryPath != null)
                {
                    throw new OptionException(
                        "when using an input directory, specify an output directory instead of a metadata output file",
                        "-o, --output instead of -c, --metadata_output_file");
                }

                if (logFormatString != null)
                {
                    int logFormatInt;
                    try
                    {
                        logFormatInt = int.Parse(logFormatString);
                    }
                    catch (FormatException e)
                    {
                        throw new OptionException("unknown log format value (0 for silent, 1 for verbose)",
                            "-l, --logging");
                    }

                    if (Enum.IsDefined(typeof(LogFormat), logFormatInt))
                    {
                        if ((LogFormat) logFormatInt != LogFormat.NONE)
                        {
                            logFormat = (LogFormat) logFormatInt;
                        }
                    }
                    else
                    {
                        throw new OptionException("unknown log format value (0 for silent, 1 for verbose)",
                            "-l, --logging");
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
                    ShowHelp("usage is (use -option=value for the optional arguments):", null,
                        optionSet);
                }
                else
                {
                    ShowHelp("Error - usage is (use -option=value for the optional arguments):", null,
                        optionSet);
                }
            }

            var exitCode = 1;
            try
            {
                switch (logFormat)
                {
                    case LogFormat.VERBOSE:
                        ((log4net.Repository.Hierarchy.Hierarchy) LogManager.GetRepository()).Root.Level =
                            Level.Debug;
                        ((log4net.Repository.Hierarchy.Hierarchy) LogManager.GetRepository())
                            .RaiseConfigurationChanged(EventArgs.Empty);
                        break;
                    case LogFormat.SILENT:
                        ((log4net.Repository.Hierarchy.Hierarchy) LogManager.GetRepository()).Root.Level =
                            Level.Off;
                        ((log4net.Repository.Hierarchy.Hierarchy) LogManager.GetRepository())
                            .RaiseConfigurationChanged(EventArgs.Empty);
                        break;
                }

                var parseInput = new ParseInput(rawFilePath, rawDirectoryPath, outputDirectory, outputFile,
                    outputFormat, outputMetadataFormat, metadataOutputFile, gzip, noPeakPicking, noZlibCompression,
                    logFormat, ignoreInstrumentErrors, s3url, s3AccessKeyId, s3SecretAccessKey, bucketName);
                RawFileParser.Parse(parseInput);

                exitCode = 0;
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(!ex.Message.IsNullOrEmpty()
                    ? ex.Message
                    : "Attempting to write to an unauthorized location.");
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                Log.Error(!ex.Message.IsNullOrEmpty()
                    ? "An Amazon S3 exception occured: " + ex.Message
                    : "An Amazon S3 exception occured: " + ex);
            }
            catch (Exception ex)
            {
                if (ex is RawFileParserException)
                {
                    Log.Error(ex.Message);
                }
                else
                {
                    Log.Error("An unexpected error occured:");
                    Log.Error(ex.ToString());
                }
            }
            finally
            {
                Environment.Exit(exitCode);
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
