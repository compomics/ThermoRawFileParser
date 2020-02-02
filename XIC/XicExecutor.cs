using System;
using System.IO;
using Newtonsoft.Json;

namespace ThermoRawFileParser.XIC
{
    public class XicExecutor
    {
        private readonly XicParameters parameters;
        private readonly XicData data;

        public XicExecutor(XicParameters parameters)
        {
            this.parameters = parameters;
            this.data = JSONParser.ParseJSON(this.parameters.jsonFilePath);
            Console.WriteLine();
        }

        public int run()
        {
            foreach (string file in parameters.rawFileList)
            {
                XicData dataInstance = new XicData(data);
                XicReader.ReadXic(file, parameters.base64, dataInstance);

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
                        directory = Path.GetDirectoryName(file);
                    }

                    var outputFileName = Path.Combine(directory, Path.GetFileNameWithoutExtension(file) + ".JSON");
                    OutputXicData(dataInstance, outputFileName);
                }
            }

            return 0;
        }

        private void StdOutputXicData(XicData outputData)
        {
            var outputString = JsonConvert.SerializeObject(outputData);
            Console.WriteLine(outputString);
        }

        private void OutputXicData(XicData outputData, string outputFileName)
        {
            var outputString = JsonConvert.SerializeObject(outputData);
            File.WriteAllText(outputFileName, outputString);
        }
    }
}