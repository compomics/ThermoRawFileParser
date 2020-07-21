using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThermoFisher.CommonCore.Data.Business;

namespace ThermoRawFileParser.DataObjects
{
    /// <summary>
    /// wrapper around scan trailer data
    /// </summary>
    public class TrailerData : IDictionary<string, string>
    {
        private readonly Dictionary<string, string> _trailer;

        //characters that will be removed from the label
        private readonly char[] punctuation = new char[] { ' ', ':' }; 

        public TrailerData(LogEntry trailer)
        {
            _trailer = new Dictionary<string, string>(trailer.Length);

            for (int i = 0; i < trailer.Length; i++)
            {
                _trailer[trailer.Labels[i].Trim(punctuation)] = trailer.Values[i];
            }
        }

        public string this[string key] { get => _trailer[key]; set => throw new ArgumentException("Read-only collection"); }

        public ICollection<string> Keys => _trailer.Keys;

        public ICollection<string> Values => _trailer.Values;

        public int Count => _trailer.Count;

        public bool IsReadOnly => true;

        public void Add(string key, string value)
        {
            throw new ArgumentException("Read-only collection");
        }

        public void Add(KeyValuePair<string, string> item)
        {
            throw new ArgumentException("Read-only collection");
        }

        public void Clear()
        {
            _trailer.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _trailer.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _trailer.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _trailer.ToArray().CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _trailer.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return false;
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return false;
        }

        public bool TryGetValue(string key, out string value)
        {
            return _trailer.TryGetValue(key, out value);
        }

        public bool TryGetDoubleValue(string key, out double value)
        {
            bool success;
            try
            {
                value = double.Parse(_trailer[key]);
                success = true;
            }
            catch
            {
                value = -1;
                success = false;
            }

            return success;
        }

        public bool TryGetIntValue(string key, out int value)
        {
            bool success;
            try
            {
                value = int.Parse(_trailer[key]);
                success = true;
            }
            catch
            {
                value = -1;
                success = false;
            }

            return success;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _trailer.GetEnumerator();
        }
    }
}
