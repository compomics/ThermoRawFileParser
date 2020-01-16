using System.Collections;

namespace ThermoRawFileParser.Query
{
    public class QueryParameters
    {
        public bool help { get; set; }
        public string rawFile { get; set; }
        public string scans { get; set; }
        public bool noPeakPicking { get; set; }
    }
}