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
            this.data = JSONParser(parameters.jsonFilePath);
        }

        public int run(){
            foreach (var file in parameters.rawFileList)
            {
                //do stuff
                XicRetriever.RetrieveXic()
            }
            return 0;
        }
        
        public void RetrieveXicData(){
            
        }
        
        public void OutputXicData(){
            string OutputFileName = parameters.outputFileName;
            string outputString = JsonConvert.SerializeObject(data);

            File.WriteAllText(OutputFileName, outputString);
        }

        public double PepseqToMass(String pep_seq){

            throw new NotImplementedException();
        }
    }
}
