using System;
using System.IO;
using Newtonsoft.Json;

namespace ThermoRawFileParser.XIC
{
    public class XicExecutor
    {
        public XicParameters parameters;
        public XicData data;
        
        public XicExecutor(XicParameters _parameters){
            this.parameters = _parameters;
            this.data = JSONParser.ParseJSON(parameters.jsonFilePath);
        }

        public int run(){
            foreach (string file in parameters.rawFileList)
            {
                //do stuff
                XicData dataInstance = new XicData(data);
                XicReader.ReadXic(file, parameters.base64, dataInstance);
                
                if (parameters.stdout)
                {
                    StdOutputXicData(dataInstance);
                }
                else
                {
                    string directory;
                    // if outputDirectory has been defined, put output there.
                    if (parameters.outputDirectory!=null)
                    {
                        directory = parameters.outputDirectory;
                    }
                    // otherwise put output files into the same directory as the raw file input
                    else
                    {
                        directory = Path.GetDirectoryName(file);
                    }
                    string outputFileName = Path.Combine(directory, Path.GetFileNameWithoutExtension(file) + ".JSON");
                    OutputXicData(dataInstance, outputFileName);
                }

            }
            return 0;
        }
        
        public void StdOutputXicData(XicData outputData){
            string outputString = JsonConvert.SerializeObject(outputData);
            Console.WriteLine(outputString);
        }
        
        public void OutputXicData(XicData outputData, string outputFileName){
            string outputString = JsonConvert.SerializeObject(outputData);
            File.WriteAllText(outputFileName, outputString);
        }

    }
}
