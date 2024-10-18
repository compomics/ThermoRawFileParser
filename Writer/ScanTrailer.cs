using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.Writer
{
    public class ScanTrailer
    {
        public int Length
        {
            get => data.Count;
        }

        public string[] Labels
        {
            get => data.Keys.ToArray();
        }

        public string[] Values
        {
            get => data.Values.ToArray();
        }

        private readonly Dictionary<string, string> data;

        public ScanTrailer(ILogEntryAccess trailerData)
        {
            data = new Dictionary<string, string>();

            for (int i = 0; i < trailerData.Length; i++)
            {
                data[trailerData.Labels[i]] = trailerData.Values[i].Trim();
            }
        }

        public ScanTrailer()
        {
            data = new Dictionary<string, string>();
        }

        /// <summary>
        /// Try returning selected trailer element as boolean value,
        /// if the element does not exist or cannot be converted to boolean return null
        /// </summary>
        /// <param name="key">name of the element</param>
        public bool? AsBool(string key)
        {
            if (data.ContainsKey(key))
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

        /// <summary>
        /// Try returning selected trailer element as double value,
        /// if the element does not exist or cannot be converted to double return null
        /// </summary>
        /// <param name="key">name of the element</param>
        public double? AsDouble(string key)
        {
            if (data.ContainsKey(key))
            {
                if (double.TryParse(data[key], NumberStyles.Any,
                    CultureInfo.CurrentCulture, out var result)) return result;
            }

            return null;
        }

        /// <summary>
        /// Try returning selected trailer element as integer value,
        /// if the element does not exist or cannot be converted to integer return null
        /// </summary>
        /// <param name="key">name of the element</param>
        public int? AsInt(string key)
        {
            if (data.ContainsKey(key))
            {
                if (int.TryParse(data[key], out var result)) return result;
            }

            return null;
        }

        /// <summary>
        /// Try returning selected trailer element as strictly positive (non zero) integer value,
        /// if the element does not exist or cannot be converted to strictly positive integer return null
        /// </summary>
        /// <param name="key">name of the element</param>
        public int? AsPositiveInt(string key)
        {
            int? value = AsInt(key);

            if (value != null && value > 0) return value;
            else return null;
        }

        /// <summary>
        /// Try returning selected trailer element as string,
        /// alias to `Get`
        /// </summary>
        /// <param name="key">name of the element</param>
        public string AsString(string key)
        {
            return Get(key);
        }

        /// <summary>
        /// Try getting selected trailer element by name,
        /// if the element does not exist return null
        /// </summary>
        /// <param name="key">name of the element</param>
        public string Get(string key)
        {
            if (data.ContainsKey(key))
            {
                return data[key];
            }

            return null;
        }

        /// <summary>
        /// Check if selected trailer element exists
        /// </summary>
        /// <param name="key">name of the element</param>
        public bool Has(string key)
        {
            return data.ContainsKey(key);
        }

        /// <summary>
        /// Return iterator over trailer element names matching regex
        /// </summary>
        /// <param name="regex">compiled regex object</param>
        public IEnumerable<string> MatchKeys(Regex regex)
        {
            return data.Keys.Where(k => regex.IsMatch(k));
        }

        /// <summary>
        /// Return iterator over trailer element values which names are matching regex
        /// </summary>
        /// <param name="regex">compiled regex object</param>
        public IEnumerable<string> MatchValues(Regex regex)
        {
            return data.Where(item => regex.IsMatch(item.Key)).Select(item => item.Value);
        }
    }
}