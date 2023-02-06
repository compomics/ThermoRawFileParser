using System.ComponentModel;
using Newtonsoft.Json;

namespace ThermoRawFileParser.XIC
{
    public class XicMeta
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public double? MzStart { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public double? MzEnd { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public double? RtStart { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public double? RtEnd { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string Filter { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(null)]
        public string Comment { get; set; }


        public XicMeta()
        {
            MzStart = null;
            MzEnd = null;
            RtStart = null;
            RtEnd = null;
            Filter = null;
            Comment = null;
        }

        public XicMeta(XicMeta copy)
        {
            MzStart = copy.MzStart;
            MzEnd = copy.MzEnd;
            RtStart = copy.RtStart;
            RtEnd = copy.RtEnd;
            Filter = copy.Filter;
            Comment = copy.Comment;
        }
    }
}