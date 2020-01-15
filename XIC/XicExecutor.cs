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
                XicRetriever.RetrieveXic(file, parameters.base64, dataInstance);
                // edit filename
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
                string outputFileName = directory + Path.GetFileNameWithoutExtension(file) + ".JSON";
                
                OutputXicData(dataInstance, outputFileName);

            }
            return 0;
        }
        
        public void RetrieveXicData(){
            
        }
        
        public void OutputXicData(XicData outputData, string outputFileName){
            Console.WriteLine(outputFileName);
            string outputString = JsonConvert.SerializeObject(outputData);
            File.WriteAllText(outputFileName, outputString);
        }

        public double PepseqToMass(String pep_seq){

            throw new NotImplementedException();
        }
    }
}
