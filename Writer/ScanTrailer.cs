using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThermoFisher.CommonCore.Data.Business;

namespace ThermoRawFileParser.Writer
{
    public class ScanTrailer
    {
        public int Length { get => data.Count; }

        public string[] Labels { get => data.Keys.ToArray(); }

        public string[] Values { get => data.Values.ToArray(); }

        private Dictionary<string, string> data;

        public ScanTrailer(LogEntry trailerData)
        {
            data = new Dictionary<string, string>();

            for (int i = 0; i < trailerData.Length; i++)
            {
                data[trailerData.Labels[i]] = trailerData.Values[i].Trim();
            }
        }

        public bool? AsBool(string key)
        {
            if(data.ContainsKey(key))
            {
                var stringValue = data[key].ToLower();

                switch (stringValue)
                {
                    case "on":
                    case "true":
                    case "yes":
                        return true;
                    default:
                        return false;
                }
            }

            return null;
        }

        public double? AsDouble(string key)
        {
            if (data.ContainsKey(key))
            {
                if (double.TryParse(data[key], out var result)) return result;
            }
            return null;
        }

        public int? AsInt(string key)
        {
            if (data.ContainsKey(key))
            {
                if (int.TryParse(data[key], out var result)) return result;
            }
            return null;
        }

        public int? AsPositiveInt(string key)
        {
            int? value = AsInt(key);

            if (value != null && value > 0) return value;
            else return null;

        }

        public string AsString(string key)
        {
            return Get(key);
        }

        public string Get(string key)
        {
            if (data.ContainsKey(key))
            {
                return data[key];
            }
            return null;
        }

        public bool Has(string key)
        {
            return data.ContainsKey(key);
        }

        public IEnumerable<string> MatchKeys(Regex regex)
        {
            return data.Keys.Where(k => regex.IsMatch(k));
        }

        public IEnumerable<string> MatchValues(Regex regex)
        {
            return data.Where(item => regex.IsMatch(item.Key)).Select(item => item.Value);
        }
    }
}
