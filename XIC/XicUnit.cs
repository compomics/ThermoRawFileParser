using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThermoRawFileParser.XIC
{
    public class XicUnit
    {
        public XicMeta Meta { get; set; }
        public object X { get; set; }
        public object Y { get; set; }
    }
}