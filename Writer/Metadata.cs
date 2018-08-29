using System;
using System.Collections.Generic;
using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParser.Writer
{
    public class Metadata
    {
        /** The general Path properties contains: RAW path , RAW file version **/
        private List<CVTerm> fileProperties = new List<CVTerm>();

        /** The Instruments properties contains the information of the instrument **/ 
        private List<CVTerm> instrumentProperties = new List<CVTerm>();

        /** Scan Settings **/
        private List<CVTerm> scanSettings = new List<CVTerm>();

        /** MS and MS data including number of MS and MS/MS **/
        private List<CVTerm> msData = new List<CVTerm>(); 
        
        private List<CVTerm> sampleData = new List<CVTerm>();
        
        /**
         * Default constructor 
         */
        public Metadata(){}

        public Metadata(List<CVTerm> fileProperties,
            List<CVTerm> instrumentProperties,
            List<CVTerm> msData)
        {
            this.fileProperties = fileProperties;
            this.instrumentProperties = instrumentProperties;
            this.msData = msData;
        }

        public List<CVTerm> FileProperties => fileProperties;

        public List<CVTerm> InstrumentProperties => instrumentProperties;

        public List<CVTerm> MsData => msData;

        public List<CVTerm> SampleData => sampleData;

        public List<CVTerm> ScanSettings => scanSettings;

        /**
         * Add a File property to the fileProperties 
         */
        public void addFileProperty(CVTerm value)
        {
            fileProperties.Add(value);
        }

        public void addInstrumentProperty( CVTerm value)
        {
            instrumentProperties.Add(value);
        }

        public void addScanSetting(CVTerm value)
        {
            scanSettings.Add(value);
        }

        public void addMSData(CVTerm value)
        {
            msData.Add(value);
        }

        public void addSampleProperty(CVTerm value)
        {
            sampleData.Add(value);
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