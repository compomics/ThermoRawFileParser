using System.ComponentModel;
using Newtonsoft.Json;

namespace ThermoRawFileParser.XIC
{
    public class XicMeta
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(-1.0)]
        public double MzStart { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(-1.0)] 
        public double MzEnd { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(-1.0)] 
        public double RtStart { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(-1.0)] 
        public double RtEnd { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string Filter { get; set; }


        public XicMeta()
        {
            MzStart = -1;
            MzEnd = -1;
            RtStart = -1;
            RtEnd = -1;
            Filter = null;
        }
        
        public XicMeta(XicMeta copy){
            MzStart = copy.MzStart;
            MzEnd = copy.MzEnd;
            RtStart = copy.RtStart;
            RtEnd = copy.RtEnd;
            Filter = copy.Filter;
        }
    }
}
