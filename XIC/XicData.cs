using System;
using System.Collections.Generic;

namespace ThermoRawFileParser.XIC
{
    public class XicData
    {
        public XicOutputMeta outputmeta { get; set; }
        public List<XicUnit> content { get; set; }

        public XicData(){
            outputmeta = new XicOutputMeta();

            content = new List<XicUnit>();
        }
    }
}
