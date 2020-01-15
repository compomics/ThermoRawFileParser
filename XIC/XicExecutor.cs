using System;
using System.IO;
using Newtonsoft.Json;

namespace ThermoRawFileParser.XIC
{
    public class XicExecutor
    {
        public XicParameters parameters;
        public XicData data;


        public void RetrieveXicData()
        {
        }

        public void OutputXicData()
        {
            string OutputFileName = parameters.outputFileName;
            string outputString = JsonConvert.SerializeObject(data);
            File.WriteAllText(OutputFileName, outputString);
        }

        public double PepseqToMass(String pep_seq)
        {
            throw new NotImplementedException();
        }
    }
}