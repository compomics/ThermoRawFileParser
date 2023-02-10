using System.Collections.Generic;

namespace ThermoRawFileParser.Query
{
    public class QueryParameters
    {
        private int _errors;

        private int _warnings;

        private string _rawFilePath;

        private string _userFilePath;

        public bool help { get; set; }
        public string rawFilePath
        { 
            get => _rawFilePath;
            set
            {
                _rawFilePath = value;
                _userFilePath = value;
            } 
        }

        public string userFilePath { get => _userFilePath; }

        public string scans { get; set; }
        public string outputFile { get; set; }
        public bool noPeakPicking { get; set; }
        public HashSet<int> scanNumbers { get; set; }
        public bool stdout { get; set; }
        public bool Vigilant { get; set; }
        public int Errors { get => _errors; }
        public int Warnings { get => _warnings; }
        public LogFormat LogFormat { get; set; }
        
        public QueryParameters()
        {
            help = false;
            rawFilePath = null;
            scans = "";
            outputFile = null;
            noPeakPicking = false;
            scanNumbers = new HashSet<int>();
            stdout = false;
            Vigilant = false;
            LogFormat = LogFormat.DEFAULT;
            _errors = 0;
            _warnings = 0;
        }
        
        public QueryParameters(QueryParameters copy)
        {
            help = copy.help;
            rawFilePath = copy.rawFilePath;
            scans = copy.scans;
            outputFile = copy.outputFile;
            noPeakPicking = copy.noPeakPicking;
            scanNumbers = new HashSet<int>();
            foreach (int s in copy.scanNumbers) scanNumbers.Add(s);
            stdout = copy.stdout;
            Vigilant = copy.Vigilant;
            LogFormat = copy.LogFormat;
            _errors = copy.Errors;
            _warnings = copy.Warnings;
        }

        public void NewError()
        {
            _errors++;
        }

        public void NewWarn()
        {
            _warnings++;
        }

        public void UpdateRealPath(string realPath)
        {
            _rawFilePath = realPath;
        }
    }
}
