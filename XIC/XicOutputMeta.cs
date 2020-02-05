using System;

namespace ThermoRawFileParser.XIC
{
    public class XicOutputMeta
    {
        public bool base64 { get; set; }
        public string timeunit { get; set; }

        public XicOutputMeta()
        {
            base64 = false;
            timeunit = String.Empty;
        }

        public XicOutputMeta(XicOutputMeta copy)
        {
            base64 = copy.base64;
            timeunit = copy.timeunit;
        }
    }
}