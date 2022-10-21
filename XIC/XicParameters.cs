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
        public string outputDirectory { get; set; }
        public bool printJsonExample { get; set; }
        public string outputFileName { get; set; }
        public bool base64 { get; set; }
        public bool stdout { get; set; }
        public bool Vigilant { get; set; }
        public int Errors { get => _errors; }

        public int Warnings { get => _warnings; }

        public XicParameters()
        {
            help = false;
            rawFileList = new ArrayList();
            jsonFilePath = null;
            outputDirectory = null;
            printJsonExample = false;
            outputFileName = null;
            base64 = false;
            stdout = false;
            Vigilant = false;
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
            outputDirectory = copy.outputDirectory;
            printJsonExample = copy.printJsonExample;
            outputFileName = copy.outputFileName;
            base64 = copy.base64;
            stdout = copy.stdout;
            Vigilant = copy.Vigilant;
            _errors = copy.Errors;
            _warnings = copy.Warnings;
        }
    }
}