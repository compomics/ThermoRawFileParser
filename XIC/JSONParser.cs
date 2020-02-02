using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using ThermoRawFileParser.Util;

namespace ThermoRawFileParser.XIC
{
    public static class JSONParser
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
                if (xic.IsAmbiguous())
                    throw new Exception("The definition of XIC is ambiguous");

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
                        case "ppm":
                            delta = xic.Mz.Value * xic.Tolerance.Value * 1e-6;
                            break;
                        case "amu":
                            delta = xic.Tolerance.Value;
                            break;
                        case "mmu":
                            delta = xic.Tolerance.Value * 1e-3;
                            break;
                        case "da":
                            delta = xic.Tolerance.Value;
                            break;
                        case "":
                            delta = xic.Mz.Value * xic.Tolerance.Value * 1e-6;
                            break;
                        default:
                            throw new Exception($"Cannot parse tolerance unit: {xic.ToleranceUnit}");
                    }

                    data.content.Add(new XicUnit(xic.Mz.Value - delta, xic.Mz.Value + delta, xic.RtStart,
                        xic.RtEnd));
                }

                else if (xic.HasMzRange())
                {
                    data.content.Add(
                        new XicUnit(xic.MzStart.Value, xic.MzEnd.Value, xic.RtStart.Value, xic.RtEnd.Value));
                }
                else
                {
                    throw new Exception(
                        $"Unparsable JSON element:\n{JsonConvert.SerializeObject(xic, Formatting.Indented)}");
                }
            }

            return data;
        }
    }
}