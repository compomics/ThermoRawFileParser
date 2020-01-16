using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ThermoRawFileParser.Util;

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
                if (xic.IsAmbigous())
                    throw new Exception("The defenition of XIC is ambigous");

                if (xic.HasSequence())
                {
                    Peptide p = new Peptide(xic.Sequence);
                    xic.Mz = p.GetMz(xic.Charge);
                }

                if (xic.HasMzTol())
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

                else if (xic.HasMzRange())
                {
                    data.content.Add(new XicUnit(xic.MzStart, xic.MzEnd, xic.RtStart, xic.RtEnd));
                }
                else
                {
                    throw new Exception(String.Format("Unparsable JSON element:\n{0}", JsonConvert.SerializeObject(xic, Formatting.Indented)));
                }
            }

            return data;
        }
    }
}
