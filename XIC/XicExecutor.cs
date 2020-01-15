using System;
using System.IO;
using Newtonsoft.Json;

namespace ThermoRawFileParser
{
    public class XicExecutor
    {
        
        
        
        
        public void RetrieveXicData(){
            
        }
        
        public void OutputXicData(XicData data, XicParameters parameters){
            string OutputFileName = parameters.outputFileName;
            string outputString = JsonConvert.SerializeObject(data);
            File.WriteAllText(OutputFileName, outputString);
        
        }

        public double PepseqToMass(String pep_seq){
            throw new NotImplementedException();
        }
    }
}
