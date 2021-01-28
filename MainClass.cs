using System;
using System.Reflection;
using System.IO;
using log4net;
using log4net.Core;
using Mono.Options;
using ThermoFisher.CommonCore.Data;
using System.Linq;
using ThermoRawFileParser.Query;
using ThermoRawFileParser.XIC;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ThermoRawFileParser
{
    public static class MainClass
    {
        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string Version = "1.3.2";

        public static void Main(string[] args)
        {
            // Set Invariant culture as default for all further processing
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            // Introduce subcommand for xics and spectra query
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "xic":
                        XicParametersParsing(args.Skip(1).ToArray()); // skip first command
                        break;
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
                    "i=|input=", "The raw file input (Required). Specify this or an input directory -d",
                    v => singleFile = v
                },
                {
                    "d=|input_directory=",
                    "The directory containing the raw files (Required). Specify this or an input file -i.",
                    v => fileDirectory = v
                },
                {
                    "j=|json=",
                    "The json input file (Required).",
                    v => parameters.jsonFilePath = v
                },
                {
                    "p|print_example",
                    "Show a json input file example.",
                    v => parameters.printJsonExample = v != null
                },
                {
                    "o=|output=",
                    "The output directory. If not specified, the output is written to the input directory",
                    v => parameters.outputDirectory = v
                },
                {
                    "b|base64",
                    "Encodes the content of the xic vectors as base 64 encoded string.",
                    v => parameters.base64 = v != null
                },
                {
                    "s|stdout",
                    "Pipes the output into standard output. Logging is being turned off.",
                    v => parameters.stdout = v != null
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
                    ShowHelp("usage is:", null,
                        optionSet);
                    return;
                }

                if (parameters.printJsonExample)
                {
                    var exampleJson =
                        "[\n  {\n    \"mz\":673.363,\n    \"tolerance\":10,\n    \"tolerance_unit\": \"ppm\",\n  },\n  {\n    \"mz\":867.345,\n    \"tolerance\": 0.02,\n    \"tolerance_unit\": \"da\",\n    \"rt_start\":87.56,\n    \"rt_end\":99.56\n  }\n]";

                    Console.WriteLine(exampleJson);
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
                    var directoryInfo = new DirectoryInfo(Path.GetFullPath(fileDirectory));
                    var files = directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly)
                        .Where(f => f.Extension.ToLower() == ".raw").ToArray<FileInfo>();
                    foreach (var file in files)
                    {
                        parameters.rawFileList.Add(file.FullName);
                    }
                }
            }
            catch (OptionException optionException)
            {
                ShowHelp("Error - usage is:", optionException,
                    optionSet);
            }
            catch (ArgumentNullException)
            {
                if (parameters.help)
                {
                    ShowHelp("usage is:", null,
                        optionSet);
                }
                else
                {
                    ShowHelp("Error - usage is:", null,
                        optionSet);
                }
            }

            var exitCode = 1;
            try
            {
                // execute the xic commands
                XicExecutor.run(parameters);

                exitCode = 0;
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
                    v => parameters.rawFilePath = v
                },
                {
                    "n=|scans=",
                    "The scan numbers. e.g. \"1-5, 20, 25-30\"",
                    v => parameters.scans = v
                },
                {
                    "b=|output_file",
                    "The output file. Specifying none writes the output file to the input file parent directory.",
                    v => parameters.outputFile = v
                },
                {
                    "p|noPeakPicking",
                    "Don't use the peak picking provided by the native Thermo library. By default peak picking is enabled.",
                    v => parameters.noPeakPicking = v != null
                },
                {
                    "s|stdout",
                    "Pipes the output into standard output. Logging is being turned off",
                    v => parameters.stdout = v != null
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
                    ShowHelp("usage is:", null,
                        optionSet);
                    return;
                }

                if (parameters.rawFilePath == null)
                {
                    throw new OptionException(
                        "specify an input file",
                        "-i, --input ");
                }

                if (parameters.rawFilePath != null && !File.Exists(parameters.rawFilePath))
                {
                    throw new OptionException(
                        "specify a valid RAW file location",
                        "-i, --input");
                }

                if (parameters.scans.IsNullOrEmpty())
                {
                    throw new OptionException(
                        "specify a valid scan range",
                        "-s, --scans");
                }
            }
            catch (OptionException optionException)
            {
                ShowHelp("Error - usage is:", optionException,
                    optionSet);
            }
            catch (ArgumentNullException)
            {
                if (parameters.help)
                {
                    ShowHelp("usage is:", null,
                        optionSet);
                }
                else
                {
                    ShowHelp("Error - usage is:", null,
                        optionSet);
                }
            }

            var exitCode = 1;
            try
            {
                QueryExecutor.Run(parameters);
                exitCode = 0;
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

        private static void RegularParametersParsing(string[] args)
        {
            var help = false;
            var version = false;
            string outputFormatString = null;
            string metadataFormatString = null;
            string logFormatString = null;
            var parseInput = new ParseInput();

            var optionSet = new OptionSet
            {
                {
                    "h|help", "Prints out the options.",
                    h => help = h != null
                },
                {
                    "version", "Prints out the version of the executable.",
                    v => version = v != null
                },
                {
                    "i=|input=", "The raw file input (Required). Specify this or an input directory -d.",
                    v => parseInput.RawFilePath = v
                },
                {
                    "d=|input_directory=",
                    "The directory containing the raw files (Required). Specify this or an input raw file -i.",
                    v => parseInput.RawDirectoryPath = v
                },
                {
                    "o=|output=",
                    "The output directory. Specify this or an output file -b. Specifying neither writes to the input directory.",
                    v => parseInput.OutputDirectory = v
                },
                {
                    "b=|output_file",
                    "The output file. Specify this or an output directory -o. Specifying neither writes to the input directory.",
                    v => parseInput.OutputFile = v
                },
                {
                    "s|stdout",
                    "Write to standard output. Cannot be combined with file or directory output. Implies silent logging, i.e. logging level 0",
                    v => parseInput.StdOut = v != null
                },
                {
                    "f=|format=",
                    "The spectra output format: 0 for MGF, 1 for mzML, 2 for indexed mzML, 3 for Parquet. Defaults to mzML if no format is specified.",
                    v => outputFormatString = v
                },
                {
                    "m=|metadata=", "The metadata output format: 0 for JSON, 1 for TXT.",
                    v => metadataFormatString = v
                },
                {
                    "c=|metadata_output_file",
                    "The metadata output file. By default the metadata file is written to the output directory.",
                    v => parseInput.MetadataOutputFile = v
                },
                {
                    "g|gzip", "GZip the output file.",
                    v => parseInput.Gzip = v != null
                },
                {
                    "p|noPeakPicking",
                    "Don't use the peak picking provided by the native Thermo library. By default peak picking is enabled.",
                    v => parseInput.NoPeakPicking = v != null
                },
                {
                    "z|noZlibCompression",
                    "Don't use zlib compression for the m/z ratios and intensities. By default zlib compression is enabled.",
                    v => parseInput.NoZlibCompression = v != null
                },
                {
                    "a|allDetectors",
                    "Extract additional detector data: UV/PDA etc",
                    v => parseInput.AllDetectors = v != null
                },
                {
                    "l=|logging=", "Optional logging level: 0 for silent, 1 for verbose.",
                    v => logFormatString = v
                },
                {
                    "e|ignoreInstrumentErrors", "Ignore missing properties by the instrument.",
                    v => parseInput.IgnoreInstrumentErrors = v != null
                },
                {
                    "x|includeExceptionData", "Include reference and exception data",
                    v => parseInput.ExData = v != null
                },
                {
                    "L=|msLevel=",
                    "Select MS levels (MS1, MS2, etc) included in the output, should be a comma-separated list of integers ( 1,2,3 ) and/or intervals ( 1-3 ), open-end intervals ( 1- ) are allowed",
                    v => parseInput.MsLevel = ParseMsLevel(v)
                },
                {
                    "P|mgfPrecursor",
                    "Include precursor scan number in MGF file TITLE",
                    v => parseInput.MGFPrecursor = v != null
                },
                {
                    "u:|s3_url:",
                    "Optional property to write directly the data into S3 Storage.",
                    v => parseInput.S3Url = v
                },
                {
                    "k:|s3_accesskeyid:",
                    "Optional key for the S3 bucket to write the file output.",
                    v => parseInput.S3AccessKeyId = v
                },
                {
                    "t:|s3_secretaccesskey:",
                    "Optional key for the S3 bucket to write the file output.",
                    v => parseInput.S3SecretAccessKey = v
                },
                {
                    "n:|s3_bucketName:",
                    "S3 bucket name",
                    v => parseInput.BucketName = v
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
                    var helpMessage =
                        $"usage is {Assembly.GetExecutingAssembly().GetName().Name}.exe [subcommand] [options]\noptional subcommands are xic|query (use [subcommand] -h for more info]):";
                    ShowHelp(helpMessage, null, optionSet);
                    return;
                }

                if (version)
                {
                    Console.WriteLine(Version);
                    return;
                }

                if (parseInput.RawFilePath == null && parseInput.RawDirectoryPath == null)
                {
                    throw new OptionException(
                        "specify an input file or an input directory",
                        "-i, --input or -d, --input_directory");
                }

                if (parseInput.RawFilePath != null)
                {
                    if (parseInput.RawDirectoryPath != null)
                    {
                        throw new OptionException(
                            "specify an input file or an input directory, not both",
                            "-i, --input or -d, --input_directory");
                    }

                    if (!File.Exists(parseInput.RawFilePath))
                    {
                        throw new OptionException(
                            "specify a valid RAW file location",
                            "-i, --input");
                    }
                }

                if (parseInput.RawDirectoryPath != null && !Directory.Exists(parseInput.RawDirectoryPath))
                {
                    throw new OptionException(
                        "specify a valid input directory",
                        "-d, --input_directory");
                }

                if (parseInput.StdOut && (parseInput.OutputFile != null || parseInput.OutputDirectory != null))
                {
                    throw new OptionException(
                        "standard output cannot be combined with file or directory output",
                        "-s, --stdout");
                }

                if (parseInput.OutputFile == null && parseInput.OutputDirectory == null)
                {
                    if (parseInput.RawFilePath != null)
                    {
                        parseInput.OutputDirectory = Path.GetDirectoryName(Path.GetFullPath(parseInput.RawFilePath));
                    }
                    else if (parseInput.RawDirectoryPath != null)
                    {
                        parseInput.OutputDirectory = parseInput.RawDirectoryPath;
                    }
                }

                if (parseInput.OutputFile != null)
                {
                    if (parseInput.OutputDirectory == null)
                    {
                        parseInput.OutputDirectory = Path.GetDirectoryName(parseInput.OutputFile);
                    }
                    else
                    {
                        throw new OptionException(
                            "specify an output directory or an output file, not both",
                            "-o, --output or -b, --output_file");
                    }

                    if (Directory.Exists(parseInput.OutputFile))
                    {
                        throw new OptionException(
                            "specify a valid output file, not a directory",
                            "-b, --output_file");
                    }

                    if (parseInput.RawDirectoryPath != null)
                    {
                        throw new OptionException(
                            "when using an input directory, specify an output directory instead of an output file",
                            "-o, --output instead of -b, --output_file");
                    }
                }

                if (!parseInput.OutputDirectory.IsNullOrEmpty() && !Directory.Exists(parseInput.OutputDirectory))
                {
                    throw new OptionException(
                        "specify a valid output directory",
                        "-o, --output");
                }

                if (metadataFormatString == null && outputFormatString == null)
                {
                    parseInput.OutputFormat = OutputFormat.MzML;
                }

                if (outputFormatString != null)
                {
                    int outPutFormatInt;
                    try
                    {
                        outPutFormatInt = int.Parse(outputFormatString);
                    }
                    catch (FormatException)
                    {
                        throw new OptionException(
                            "unknown output format value (0 for MGF, 1 for mzML, 2 for indexed mzML, 3 for Parquet)",
                            "-f, --format");
                    }

                    if (Enum.IsDefined(typeof(OutputFormat), outPutFormatInt) &&
                        ((OutputFormat) outPutFormatInt) != OutputFormat.NONE)
                    {
                        parseInput.OutputFormat = (OutputFormat) outPutFormatInt;
                    }
                    else
                    {
                        throw new OptionException(
                            "unknown output format value (0 for MGF, 1 for mzML, 2 for indexed mzML, 3 for Parquet)",
                            "-f, --format");
                    }
                }

                if (metadataFormatString != null)
                {
                    int metadataInt;
                    try
                    {
                        metadataInt = int.Parse(metadataFormatString);
                    }
                    catch (FormatException)
                    {
                        throw new OptionException("unknown metadata format value (0 for JSON, 1 for TXT)",
                            "-m, --metadata");
                    }

                    if (Enum.IsDefined(typeof(MetadataFormat), metadataInt) &&
                        ((MetadataFormat) metadataInt) != MetadataFormat.NONE)
                    {
                        parseInput.MetadataFormat = (MetadataFormat) metadataInt;
                    }
                    else
                    {
                        throw new OptionException("unknown metadata format value (0 for JSON, 1 for TXT)",
                            "-m, --metadata");
                    }
                }

                if (parseInput.MetadataOutputFile != null && Directory.Exists(parseInput.MetadataOutputFile))
                {
                    throw new OptionException(
                        "specify a valid metadata output file, not a directory",
                        "-c, --metadata_output_file");
                }

                if (parseInput.MetadataOutputFile != null && parseInput.MetadataFormat == MetadataFormat.NONE)
                {
                    throw new OptionException("specify a metadata format (0 for JSON, 1 for TXT)",
                        "-m, --metadata");
                }

                if (parseInput.MetadataOutputFile != null && parseInput.RawDirectoryPath != null)
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
                    catch (FormatException)
                    {
                        throw new OptionException("unknown log format value (0 for silent, 1 for verbose)",
                            "-l, --logging");
                    }

                    if (Enum.IsDefined(typeof(LogFormat), logFormatInt))
                    {
                        if ((LogFormat) logFormatInt != LogFormat.NONE)
                        {
                            parseInput.LogFormat = (LogFormat) logFormatInt;
                        }
                    }
                    else
                    {
                        throw new OptionException("unknown log format value (0 for silent, 1 for verbose)",
                            "-l, --logging");
                    }
                }

                if (parseInput.StdOut)
                {
                    parseInput.LogFormat = LogFormat.SILENT;
                }

                if (parseInput.S3Url != null && parseInput.S3AccessKeyId != null &&
                    parseInput.S3SecretAccessKey != null && parseInput.BucketName != null)
                    if (Uri.IsWellFormedUriString(parseInput.S3Url, UriKind.Absolute))
                    {
                        parseInput.InitializeS3Bucket();
                    }
                    else
                    {
                        throw new RawFileParserException("Invalid S3 url: " + parseInput.S3Url);
                    }
            }
            catch (OptionException optionException)
            {
                ShowHelp("Error - usage is:", optionException,
                    optionSet);
            }
            catch (ArgumentNullException)
            {
                if (help)
                {
                    ShowHelp("usage is:", null,
                        optionSet);
                }
                else
                {
                    ShowHelp("Error - usage is:", null,
                        optionSet);
                }
            }

            var exitCode = 1;
            try
            {
                switch (parseInput.LogFormat)
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

        private static HashSet<int> ParseMsLevel(string inputString)
        {
            HashSet<int> result = new HashSet<int>();
            Regex valid = new Regex(@"^[\d,\-\s]+$");
            Regex interval = new Regex(@"^\s*(\d+)?\s*(-)?\s*(\d+)?\s*$");

            if (!valid.IsMatch(inputString))
                throw new OptionException("Invalid characters in msLevel key", "msLevel");

            foreach (var piece in inputString.Split(new char[] {','}))
            {
                try
                {
                    int start;
                    int end;

                    var intervalMatch = interval.Match(piece);

                    if (!intervalMatch.Success)
                        throw new OptionException();

                    if (intervalMatch.Groups[2].Success) //it is interval
                    {
                        if (intervalMatch.Groups[1].Success)
                            start = Math.Max(1, int.Parse(intervalMatch.Groups[1].Value));
                        else
                            start = 1;

                        if (intervalMatch.Groups[3].Success)
                            end = Math.Min(10, int.Parse(intervalMatch.Groups[3].Value));
                        else
                            end = 10;
                    }
                    else
                    {
                        if (intervalMatch.Groups[1].Success)
                            end = start = int.Parse(intervalMatch.Groups[1].Value);
                        else
                            throw new OptionException();

                        if (intervalMatch.Groups[3].Success)
                            throw new OptionException();
                    }

                    for (int l = start; l <= end; l++)
                    {
                        result.Add(l);
                    }
                }

                catch (Exception ex)
                {
                    throw new OptionException(String.Format("Cannot parse part of msLevel input: '{0}'", piece),
                        "msLevel", ex);
                }
            }

            return result;
        }
    }
}