using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThermoRawFileParser.XIC
{
    public class JSONInputUnit
    {
        public double MzStart { get; set; }
        public double MzEnd { get; set; }
        public double Mz { get; set; }
        public string Sequence { get; set; }
        public double Tolerance { get; set; }
        public string ToleranceUnit { get; set; }
        public int Charge { get; set; }
        public double RtStart { get; set; }
        public double RtEnd { get; set; }
    }
}
