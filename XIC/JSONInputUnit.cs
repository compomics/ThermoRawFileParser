using System.ComponentModel;
using Newtonsoft.Json;

namespace ThermoRawFileParser.XIC
{
    public class JSONInputUnit
    {
        [JsonProperty("mz_start", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(-1)]
        public double MzStart { get; set; }
        [JsonProperty("mz_end", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(-1)]
        public double MzEnd { get; set; }
        [JsonProperty("mz", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(-1)]
        public double Mz { get; set; }
        [JsonProperty("sequence", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue("")]
        public string Sequence { get; set; }
        [JsonProperty("tolerance", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(-1)]
        public double Tolerance { get; set; }
        [JsonProperty("tolerance_unit", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue("")]
        public string ToleranceUnit { get; set; }
        [JsonProperty("charge", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(1)]
        public int Charge { get; set; }
        [JsonProperty("rt_start", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(-1)]
        public double RtStart { get; set; }
        [JsonProperty("rt_end", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(-1)]
        public double RtEnd { get; set; }
        [JsonProperty("scan_filter", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(null)]
        public string Filter { get; set; }

        public bool HasMzRange()
        {
            return MzStart != -1 && MzEnd != -1;
        }

        public bool HasMzTol()
        {
            return Mz != -1 && Tolerance != -1 && ToleranceUnit != "";
        }

        public bool HasSequence()
        {
            return Sequence != "" && Tolerance != -1 && ToleranceUnit != "";
        }

        public bool IsAmbigous()
        {
            return (this.HasMzTol() && this.HasMzRange()) || 
                (this.HasMzTol() && this.HasSequence()) || 
                (this.HasMzRange() && this.HasSequence());
        }
    }
}
