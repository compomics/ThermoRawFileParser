using System.Collections;

namespace ThermoRawFileParser.XIC
{
    public class XicParameters
    {
        private int _errors;
        private int _warnings;

        public bool help { get; set; }
        public ArrayList rawFileList { get; set; }
        public string jsonFilePath { get; set; }
        public ArrayList outputFileList { get; set; }
        public bool printJsonExample { get; set; }
        public bool base64 { get; set; }
        public bool stdout { get; set; }
        public bool Vigilant { get; set; }
        public int Errors { get => _errors; }
        public int Warnings { get => _warnings; }
        public LogFormat LogFormat { get; set; }

        public XicParameters()
        {
            help = false;
            rawFileList = new ArrayList();
            jsonFilePath = null;
            outputFileList = new ArrayList();
            printJsonExample = false;
            base64 = false;
            stdout = false;
            Vigilant = false;
            LogFormat = LogFormat.DEFAULT;
            _errors = 0;
            _warnings = 0;
        }

        public void NewError()
        {
            _errors++;
        }

        public void NewWarn()
        {
            _warnings++;
        }


        public XicParameters(XicParameters copy)
        {
            help = copy.help;
            rawFileList = new ArrayList();
            foreach (string fileName in copy.rawFileList)
            {
                rawFileList.Add(fileName);
            }

            jsonFilePath = copy.jsonFilePath;
            outputFileList = copy.outputFileList;
            printJsonExample = copy.printJsonExample;
            base64 = copy.base64;
            stdout = copy.stdout;
            LogFormat = copy.LogFormat;
            Vigilant = copy.Vigilant;
            _errors = copy.Errors;
            _warnings = copy.Warnings;
        }
    }
}