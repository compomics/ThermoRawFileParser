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
        public bool stdout { get; set; }
        
        
        public QueryParameters()
        {
            help = false;
            rawFilePath = null;
            scans = "";
            outputFile = null;
            noPeakPicking = false;
            scanNumbers = new HashSet<int>();
            stdout = false;
        }
        
        public QueryParameters(QueryParameters copy)
        {
            help = copy.help;
            rawFilePath = copy.rawFilePath;
            scans = copy.scans;
            outputFile = copy.outputFile;
            noPeakPicking = copy.noPeakPicking;
            scanNumbers = new HashSet<int>();
            foreach (int s in copy.scanNumbers) scanNumbers.Add(s);
            stdout = copy.stdout;
        }
    }
}
