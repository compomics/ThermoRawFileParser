using System.Collections.Generic;

namespace ThermoRawFileParser.XIC
{
    public class XicData
    {
        public XicOutputMeta OutputMeta { get; set; }
        public List<XicUnit> Content { get; set; }

        public XicData()
        {
            OutputMeta = new XicOutputMeta();

            Content = new List<XicUnit>();
        }

        public XicData(XicData copy)
        {
            OutputMeta = new XicOutputMeta(copy.OutputMeta);

            Content = new List<XicUnit>();
            foreach (XicUnit unit in copy.Content)
            {
                Content.Add(new XicUnit(unit));
            }
        }
    }
}