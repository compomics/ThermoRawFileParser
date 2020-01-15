using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ThermoRawFileParser.XIC
{
    public class JSONInputUnit
    {
        [JsonProperty("mz_start", NullValueHandling = NullValueHandling.Ignore)]
        public double MzStart { get; set; }
        [JsonProperty("mz_end", NullValueHandling = NullValueHandling.Ignore)] 
        public double MzEnd { get; set; }
        [JsonProperty("mz", NullValueHandling = NullValueHandling.Ignore)] 
        public double Mz { get; set; }
        [JsonProperty("sequence", NullValueHandling = NullValueHandling.Ignore)] 
        public string Sequence { get; set; }
        [JsonProperty("tolerance", NullValueHandling = NullValueHandling.Ignore)] 
        public double Tolerance { get; set; }
        [JsonProperty("tolerance_unit", NullValueHandling = NullValueHandling.Ignore)] 
        public string ToleranceUnit { get; set; }
        [JsonProperty("charge", NullValueHandling = NullValueHandling.Ignore)] 
        public int Charge { get; set; }
        [JsonProperty("rt_start", NullValueHandling = NullValueHandling.Ignore)] 
        public double RtStart { get; set; }
        [JsonProperty("rt_end", NullValueHandling = NullValueHandling.Ignore)] 
        public double RtEnd { get; set; }
    }
}
