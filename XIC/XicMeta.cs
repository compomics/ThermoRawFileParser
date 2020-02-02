using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThermoRawFileParser.XIC
{
    public class XicMeta
    {
        public double? MzStart { get; set; }
        public double? MzEnd { get; set; }
        public double? RtStart { get; set; }
        public double? RtEnd { get; set; }


        public XicMeta()
        {
            // MzStart = -1;
            // MzEnd = -1;
            // RtStart = -1;
            // RtEnd = -1;
        }

        public XicMeta(XicMeta copy)
        {
            MzStart = copy.MzStart;
            MzEnd = copy.MzEnd;
            RtStart = copy.RtStart;
            RtEnd = copy.RtEnd;
        }
    }
}