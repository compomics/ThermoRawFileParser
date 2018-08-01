using System;
using System.Collections.Generic;
using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParser.Writer
{
    public class Metadata
    {
        /** The general Path properties contains: RAW path , RAW file version **/
        private List<Dictionary<string, string>> fileProperties = new List<Dictionary<string, string>>();

        /** The Instruments properties contains the information of the instrument **/ 
        private List<Dictionary<string, CVTerm>> instrumentProperties = new List<Dictionary<string, CVTerm>>();

        /** Scan Settings **/
        private List<Dictionary<String, CVTerm>> scanSettings = new List<Dictionary<string, CVTerm>>();

        /** MS and MS data including number of MS and MS/MS **/
        private List<Dictionary<String, CVTerm>> msData = new List<Dictionary<string, CVTerm>>(); 
        
        private List<Dictionary<string, string>> sampleData = new List<Dictionary<string, string>>();
        
        /**
         * Default constructor 
         */
        public Metadata(){}

        public Metadata(List<Dictionary<string, string>> fileProperties,
            List<Dictionary<string, CVTerm>> instrumentProperties,
            List<Dictionary<string, CVTerm>> msData)
        {
            this.fileProperties = fileProperties;
            this.instrumentProperties = instrumentProperties;
            this.msData = msData;
        }

        public List<Dictionary<string, string>> FileProperties => fileProperties;

        public List<Dictionary<string, CVTerm>> InstrumentProperties => instrumentProperties;

        public List<Dictionary<string, CVTerm>> MsData => msData;

        public List<Dictionary<string, string>> SampleData => sampleData;

        public List<Dictionary<string, CVTerm>> ScanSettings => scanSettings;

        /**
         * Add a File property to the fileProperties 
         */
        public void addFileProperty(String key, String value)
        {
            var dic = new Dictionary<string, string>();
            dic.Add(key, value);
            fileProperties.Add(dic);
        }

        public void addInstrumentProperty(string key, CVTerm value)
        {
            var dic = new Dictionary<string, CVTerm>();
            dic.Add(key, value);
            instrumentProperties.Add(dic);
        }

        public void addScanSetting(string key, CVTerm value)
        {
            var dic = new Dictionary<string, CVTerm>();
            dic.Add(key, value);
            scanSettings.Add(dic);
        }

        public void addMSData(string key, CVTerm value)
        {
            var dic = new Dictionary<string, CVTerm>();
            dic.Add(key, value);
            msData.Add(dic);
        }

        public void addSampleProperty(string key, string value)
        {
            var dic = new Dictionary<string, string>();
            dic.Add(key, value);
            sampleData.Add(dic);
        }
        
        
    }

    public class CVTerm
    {
        private string acc = "";
        private string cvLabelID ="";
        private string cvName = "";
        private string cvValue ="";

        public CVTerm()
        {
        }

        public CVTerm(string accession, string cvLabel, string name, string value)
        {
            this.acc = accession;
            this.cvLabelID = cvLabel;
            this.cvName = name;
            this.cvValue = value;
        }

        public string accession => acc;
        public string cvLabel => cvLabelID;
        public string name => cvName;
        public string value => cvValue;
    }
    
}