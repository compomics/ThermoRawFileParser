using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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

    }
}
