namespace ThermoRawFileParser.XIC
{
    public class XicParameters
    {
        public double Low { get; set; }
        public double High { get; set; }
        
        public bool help { get; set; }
        public string rawFilePath { get; set; }
        public string jsonFilePath { get; set; }
        public string rawDirectoryPath { get; set; }
        public string outputDirectory { get; set; }
        public bool printJsonExample { get; set; }
        public string outputFileName { get; set; }
        public bool base64 { get; set; }
        
        
        
        public XicParameters(){
            help = false;
            rawFilePath = null;
            jsonFilePath = null;
            rawDirectoryPath = null;
            outputDirectory = null;
            printJsonExample = false;
            outputFileName = null;
            base64 = false;
        }
        
        
        public XicParameters(XicParameters copy){
            help = copy.help;
            rawFilePath = copy.rawFilePath;
            jsonFilePath = copy.jsonFilePath;
            rawDirectoryPath = copy.rawDirectoryPath;
            outputDirectory = copy.outputDirectory;
            printJsonExample = copy.printJsonExample;
            base64 = copy.base64;
        }
    }
}
