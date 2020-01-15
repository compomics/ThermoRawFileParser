using System;
using System.IO;
using Newtonsoft.Json;

namespace ThermoRawFileParser.XIC
{
    public class XicExecutor
    {
        public XicParameters _parameters;
        public XicData _data;
        
        public XicExecutor(XicParameters parameters){
            this._parameters = parameters;
        }
        
        public void RetrieveXicData(){
            
        }
        
        public void OutputXicData(){
            string OutputFileName = _parameters.outputFileName;
            string outputString = JsonConvert.SerializeObject(_data);
            File.WriteAllText(OutputFileName, outputString);
        
        }


        public double PepseqToMass(String pep_seq){
            throw new NotImplementedException();
        }
    }
}
