using System;
using System.Data;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using ThermoFisher.CommonCore.Data;

namespace ThermoRawFileParser.XIC
{
    public static class XicExecutor
    {
        public static void run(XicParameters parameters)
        {
            var jsonString = File.ReadAllText(parameters.jsonFilePath, Encoding.UTF8);
            var validationErrors = JSONParser.ValidateJson(jsonString);
            if (!validationErrors.IsNullOrEmpty())
            {
                var validationMessage = new StringBuilder("JSON validation error(s):\n");
                foreach (var validationError in validationErrors)
                {
                    if (validationError.ToString().Contains("ExcludedSchemaValidates"))
                    {
                        validationMessage.Append(
                            "Use M/Z and tolerance, M/Z start and M/Z end or sequence and tolerance, not a combination (with optional RT start and/or end).\n");
                    }

                    validationMessage.Append(
                        $"element start line number: {validationError.LineNumber}\n{validationError.ToString()}");
                }

                throw new RawFileParserException(validationMessage.ToString());
            }

            var xicData = JSONParser.ParseJSON(jsonString);
            foreach (string rawFile in parameters.rawFileList)
            {
                var dataInstance = new XicData(xicData);
                XicReader.ReadXic(rawFile, parameters.base64, dataInstance);

                if (parameters.stdout)
                {
                    StdOutputXicData(dataInstance);
                }
                else
                {
                    // if outputDirectory has been defined, put output there.
                    string directory;
                    if (parameters.outputDirectory != null)
                    {
                        directory = parameters.outputDirectory;
                    }
                    // otherwise put output files into the same directory as the raw file input
                    else
                    {
                        directory = Path.GetDirectoryName(rawFile);
                    }

                    var outputFileName = Path.Combine(directory ?? throw new NoNullAllowedException(),
                        Path.GetFileNameWithoutExtension(rawFile) + ".json");

                    OutputXicData(dataInstance, outputFileName);
                }
            }
        }

        private static void StdOutputXicData(XicData outputData)
        {
            var outputString = JsonConvert.SerializeObject(outputData, Formatting.Indented);
            Console.WriteLine(outputString);
        }

        private static void OutputXicData(XicData outputData, string outputFileName)
        {
            var outputString = JsonConvert.SerializeObject(outputData, Formatting.Indented);
            File.WriteAllText(outputFileName, outputString);
        }
    }
}