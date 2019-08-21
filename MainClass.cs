using System;
using System.IO;
using log4net;
using log4net.Core;
using Mono.Options;
using ThermoFisher.CommonCore.Data;

namespace ThermoRawFileParser
{
    public static class MainClass
    {
        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string Version = "1.1.10 ";

        public static void Main(string[] args)
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
            var precursorIntensity = false;
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
                    "i=|input=", "The raw file input. Specify this or an input directory.",
                    v => rawFilePath = v
                },
                {
                    "d=|input_directory=",
                    "The directory containing input raw files. Specify this or an input raw file.",
                    v => rawDirectoryPath = v
                },
                {
                    "o=|output=", "The output directory. Specify this or an output file" +
                                  " (specifying neither writes to the input directory).",
                    v => outputDirectory = v
                },
                {
                    "b=|output_file", "The output file. Specify this or an output directory" +
                                      " (specifying neither writes to the input directory).",
                    v => outputFile = v
                },
                {
                    "f=|format=",
                    "The output format for the spectra (0 for MGF, 1 for mzML, 2 for indexed mzML, 3 for Parquet," +
                    "defaults to mzML if not specified).",
                    v => outputFormatString = v
                },
                {
                    "m=|metadata=", "The metadata output format (0 for JSON, 1 for TXT).",
                    v => outputMetadataString = v
                },
                {
                    "c=|metadata_output_file",
                    "The metadata output file (by default the metadata file is written to the output directory)",
                    v => metadataOutputFile = v
                },
                {
                    "g|gzip", "GZip the output file if this flag is specified (without value).",
                    v => gzip = v != null
                },
                {
                    "p|noPeakPicking",
                    "Don't use the peak picking provided by the native Thermo library (by default peak picking is enabled).",
                    v => noPeakPicking = v != null
                },
                {
                    "j|precursorIntensity",
                    "Report the precursor peak intensity (by default the precursor peak intensity (MS:1000042) is not reported).",
                    v => precursorIntensity = v != null
                },
                {
                    "z|noZlibCompression",
                    "Don't use zlib compression for the m/z ratios and intensities (by default zlib compression is enabled).",
                    v => noZlibCompression = v != null
                },
                {
                    "l=|logging=", "Optional logging level (0 for silent, 1 for verbose).",
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
                    ShowHelp(" usage is (use -option=value for the optional arguments):", null,
                        optionSet);
                    return;
                }

                if (version)
                {
                    Console.WriteLine(Version);
                    return;
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
                    outputFormat, outputMetadataFormat, metadataOutputFile, gzip, noPeakPicking, precursorIntensity,
                    noZlibCompression, logFormat, ignoreInstrumentErrors, s3url, s3AccessKeyId, s3SecretAccessKey,
                    bucketName);
                RawFileParser.Parse(parseInput);
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                Log.Error(!ex.Message.IsNullOrEmpty()
                    ? "An Amazon S3 exception occured: " + ex.Message
                    : "An Amazon S3 exception occured: " + ex);

                Environment.Exit(1);
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