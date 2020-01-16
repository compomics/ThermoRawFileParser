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
                int counter = 0;
                if (xic.Mz > 0) counter++;
                if (xic.MzStart > 0 || xic.MzEnd > 0) counter++;
                if (xic.Sequence != null && xic.Sequence != "") counter++;
                
                if (counter != 1) throw new Exception("Json input parsing error, more than one mz specified");
                
                if (xic.Mz > 0)
                {
                    
                    if (xic.Tolerance < 0 || xic.ToleranceUnit == null || xic.ToleranceUnit == "" || !(new HashSet<string>(new string[2]{"da", "ppm"})).Contains(xic.ToleranceUnit)) throw new Exception("Json input parsing error, please specify a tolerance and tolerance unit [da, ppm]");
                    
                    double mzStart;
                    double mzEnd;
                    if (xic.ToleranceUnit == "da")
                    {
                        mzStart = xic.Mz - xic.Tolerance;
                        mzEnd = xic.Mz + xic.Tolerance;
                    }
                    else {
                        double tol = xic.Tolerance / 1000000.0;
                        mzStart = xic.Mz * (1 - tol);
                        mzEnd = xic.Mz * (1 + tol);
                    }
                    data.content.Add(new XicUnit(xic.MzStart, xic.MzEnd, xic.RtStart, xic.RtEnd));
                }
                
                else if (xic.MzStart > 0 || xic.MzEnd > 0)
                {
                    data.content.Add(new XicUnit(xic.MzStart, xic.MzEnd, xic.RtStart, xic.RtEnd));
                }
            }

            return data;
        }
    }
}
