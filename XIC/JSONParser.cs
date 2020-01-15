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
        private JSONInput jsonIn;

        public JSONParser(string jsonPath)
        {
            XicData data = new XicData();
            using (StreamReader sr = new StreamReader(jsonPath))
            {
                jsonIn = JsonConvert.DeserializeObject<JSONInput>(sr.ReadToEnd());
            }
        }


    }
}
