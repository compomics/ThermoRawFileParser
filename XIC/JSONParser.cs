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
            JSONInput jsonIn;
            XicData data = new XicData();
            using (StreamReader sr = new StreamReader(jsonPath))
            {
                jsonIn = JsonConvert.DeserializeObject<JSONInput>(sr.ReadToEnd());
            }

            foreach (JSONInputUnit xic in jsonIn.queries)
            {
                data.content.Add(new XicUnit(xic.MzStart, xic.MzEnd, xic.RtStart, xic.RtEnd));
            }

            return data;
        }
    }
}
