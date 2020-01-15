using System;
using System.Collections.Generic;

namespace ThermoRawFileParser.XIC
{
    public class XicData
    {
        public XicOutputMeta outputmeta { get; set; }
        public List<XicUnit> content { get; set; }

        public XicData()
        {
            outputmeta = new XicOutputMeta();

            content = new List<XicUnit>();
        }
        
        public XicData(XicData copy){
            
            outputmeta = new XicOutputMeta(copy.outputmeta);

            content = new List<XicUnit>();
            foreach (XicUnit unit in copy.content)
            {
                content.Add(new XicUnit(unit));
            }
        }
    }
}
