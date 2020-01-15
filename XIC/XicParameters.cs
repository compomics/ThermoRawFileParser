using System;

namespace ThermoRawFileParser.XIC
{
    public class XicParameters
    {
        public bool help { get; set; }
        public string rawFilePath { get; set; }
        public ArrayList jsonFileList { get; set; }
        public string outputDirectory { get; set; }
        public bool printJsonExample { get; set; }
        public string outputFileName { get; set; }
        public bool base64 { get; set; }


        public XicParameters()
        {
            help = false;
            rawFileList = new ArrayList();
            jsonFilePath = null;
            outputDirectory = null;
            printJsonExample = false;
            outputFileName = null;
            base64 = false;
        }


        public XicParameters(XicParameters copy)
        {
            help = copy.help;
            rawFileList = new ArrayList();
            foreach (string fileName : copy.rawFileList)
            {
                rawFileList.Add(fileName);
            }
            jsonFilePath = copy.jsonFilePath;
            outputDirectory = copy.outputDirectory;
            printJsonExample = copy.printJsonExample;
            outputFileName = copy.outputFileName;
            base64 = copy.base64;
        }
    }
}
