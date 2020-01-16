using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ThermoRawFileParser.XIC
{
    public class JSONParser
    {
        public static XicData ParseJSON(string jsonPath)
        {
            List<JSONInputUnit> jsonIn;
            XicData data = new XicData();
            using (StreamReader sr = new StreamReader(jsonPath))
            {
                jsonIn = JsonConvert.DeserializeObject<List<JSONInputUnit>>(sr.ReadToEnd());
            }

            foreach (JSONInputUnit xic in jsonIn)
            {
                if (xic.Tolerance != 0 && xic.ToleranceUnit != null && xic.Mz != 0)
                {
                    double delta;
                    switch (xic.ToleranceUnit.ToLower())
                    {
                        case "ppm": delta = xic.Mz * xic.Tolerance * 1e-6; break;
                        case "amu": delta = xic.Tolerance; break;
                        case "mmu": delta = xic.Tolerance * 1e-3; break;
                        case "da": delta = xic.Tolerance; break;
                        case "": delta = xic.Mz * xic.Tolerance * 1e-6; break;
                        default:
                            throw new Exception(String.Format("Cannot parse tolerance unit: {0}", xic.ToleranceUnit));
                    }
                    data.content.Add(new XicUnit(xic.Mz - delta, xic.Mz + delta, xic.RtStart, xic.RtEnd));
                }

                if (xic.MzStart != 0 && xic.MzEnd != 0) data.content.Add(new XicUnit(xic.MzStart, xic.MzEnd, xic.RtStart, xic.RtEnd));
            }

            return data;
        }
    }
}
