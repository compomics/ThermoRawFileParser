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
        private List<Dictionary<String, Object>> scanSettings = new List<Dictionary<string, Object>>();

        /** MS and MS data including number of MS and MS/MS **/
        private List<Dictionary<String, Object>> msData = new List<Dictionary<string, Object>>(); 
        
        private List<Dictionary<string, string>> sampleData = new List<Dictionary<string, string>>();
        
        /**
         * Default constructor 
         */
        public Metadata(){}

        public Metadata(List<Dictionary<string, string>> fileProperties,
            List<Dictionary<string, CVTerm>> instrumentProperties,
            List<Dictionary<string, Object>> msData)
        {
            this.fileProperties = fileProperties;
            this.instrumentProperties = instrumentProperties;
            this.msData = msData;
        }

        public List<Dictionary<string, string>> FileProperties => fileProperties;

        public List<Dictionary<string, CVTerm>> InstrumentProperties => instrumentProperties;

        public List<Dictionary<string, Object>> MsData => msData;

        public List<Dictionary<string, string>> SampleData => sampleData;

        public List<Dictionary<string, Object>> ScanSettings => scanSettings;

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

        public void addScanSetting(string key, Object value)
        {
            var dic = new Dictionary<string, Object>();
            dic.Add(key, value);
            scanSettings.Add(dic);
        }

        public void addMSData(string key, Object value)
        {
            var dic = new Dictionary<string, Object>();
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

    public class CVTerm{
        
        private string acc = "";
        private string cvLabelID ="";
        private string cvName = "";
        private string cvValue ="";

        public CVTerm()
        {
        }

        public CVTerm(string accession, string cvLabel, string name, string value)
        {
            acc = accession;
            cvLabelID = cvLabel;
            cvName = name;
            cvValue = value;
        }

        public string accession => acc;
        public string cvLabel => cvLabelID;
        public string name => cvName;
        public string value => cvValue;

       
        public override int GetHashCode()
        {
            return CvTermComparer.GetHashCode(this); 
            
        }

        private sealed class CvTermEqualityComparer : IEqualityComparer<CVTerm>
        {
            public bool Equals(CVTerm x, CVTerm y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.acc, y.acc) && string.Equals(x.cvLabelID, y.cvLabelID) && string.Equals(x.cvName, y.cvName) && string.Equals(x.cvValue, y.cvValue);
            }

            public int GetHashCode(CVTerm obj)
            {
                unchecked
                {
                    var hashCode = (obj.acc != null ? obj.acc.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (obj.cvLabelID != null ? obj.cvLabelID.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (obj.cvName != null ? obj.cvName.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (obj.cvValue != null ? obj.cvValue.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        public static IEqualityComparer<CVTerm> CvTermComparer { get; } = new CvTermEqualityComparer();

        public override bool Equals(object obj)
        {
            return CvTermComparer.Equals(obj);
        }
    }
    
}