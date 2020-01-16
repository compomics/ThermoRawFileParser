using System.Collections;

namespace ThermoRawFileParser.Query
{
    public class QueryParameters
    {
        public bool help { get; set; }
        public ArrayList rawFileList { get; set; }
        public string jsonFilePath { get; set; }
        public string outputDirectory { get; set; }
        public bool printJsonExample { get; set; }
        public string outputFileName { get; set; }
        public bool base64 { get; set; }
    }
}