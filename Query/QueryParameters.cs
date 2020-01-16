using System.Collections;
using System.Collections.Generic;
using System.Configuration;

namespace ThermoRawFileParser.Query
{
    public class QueryParameters
    {
        public bool help { get; set; }
        public string rawFilePath { get; set; }
        public string scans { get; set; }
        public string outputFile { get; set; }
        public bool noPeakPicking { get; set; }
        public HashSet<int> scanNumbers { get; set; }
    }
}