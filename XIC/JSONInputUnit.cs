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
        [JsonProperty("mz_start")]
        public double MzStart { get; set; }
        [JsonProperty("mz_end")] 
        public double MzEnd { get; set; }
        [JsonProperty("mz")] 
        public double Mz { get; set; }
        [JsonProperty("sequence")] 
        public string Sequence { get; set; }
        [JsonProperty("tolerance")] 
        public double Tolerance { get; set; }
        [JsonProperty("tolerance_unit")] 
        public string ToleranceUnit { get; set; }
        [JsonProperty("charge")] 
        public int Charge { get; set; }
        [JsonProperty("rt_start")] 
        public double RtStart { get; set; }
        [JsonProperty("rt_end")] 
        public double RtEnd { get; set; }
    }
}
