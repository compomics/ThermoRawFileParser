using System;
using System.IO;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;
using ThermoFisher.CommonCore.Data;
using log4net;

namespace ThermoRawFileParser.XIC
{
    public static class XicExecutor
    {
        private static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Run(XicParameters parameters)
        {
            Log.InfoFormat("Reading and validating JSON input");
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

            Log.InfoFormat("Input contains {0} XICs", xicData.Content.Count);

            for (int index = 0; index < parameters.rawFileList.Count; index++)
            {
                string rawFile = (string) parameters.rawFileList[index];

                var dataInstance = new XicData(xicData);
                XicReader.ReadXic(rawFile, parameters.base64, dataInstance, ref parameters);

                if (parameters.stdout)
                {
                    StdOutputXicData(dataInstance);
                }
                else
                {
                    var outputFileName = (string) parameters.outputFileList[index];

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