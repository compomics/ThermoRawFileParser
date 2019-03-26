using System.Collections.Generic;

namespace ThermoRawFileParser.Writer
{
    public class Metadata
    {
        /** The general Path properties contains: RAW path , RAW file version **/

        /** The Instruments properties contains the information of the instrument **/

        /** Scan Settings **/

        /** MS and MS data including number of MS and MS/MS **/

        /**
         * Default constructor 
         */
        public Metadata(){}

        public Metadata(List<CVTerm> fileProperties,
            List<CVTerm> instrumentProperties,
            List<CVTerm> msData)
        {
            FileProperties = fileProperties;
            InstrumentProperties = instrumentProperties;
            MsData = msData;
        }

        public List<CVTerm> FileProperties { get; } = new List<CVTerm>();

        public List<CVTerm> InstrumentProperties { get; } = new List<CVTerm>();

        public List<CVTerm> MsData { get; } = new List<CVTerm>();

        public List<CVTerm> SampleData { get; } = new List<CVTerm>();

        public List<CVTerm> ScanSettings { get; } = new List<CVTerm>();

        /**
         * Add a File property to the fileProperties 
         */
        public void addFileProperty(CVTerm value)
        {
            FileProperties.Add(value);
        }

        public void addInstrumentProperty( CVTerm value)
        {
            InstrumentProperties.Add(value);
        }

        public void addScanSetting(CVTerm value)
        {
            ScanSettings.Add(value);
        }
        
        public void addScanSetting(ICollection<CVTerm> value)
        {
            ScanSettings.AddRange(value);
        }

        

        public void addMSData(CVTerm value)
        {
            MsData.Add(value);
        }
        
        public void addMSData(HashSet<CVTerm> value)
        {
            MsData.AddRange(value);
        }

        public void addSampleProperty(CVTerm value)
        {
            SampleData.Add(value);
        }     
    }

    public class CVTerm{
        
        private readonly string acc = "";
        private readonly string cvLabelID ="";
        private readonly string cvName = "";
        private readonly string cvValue ="";

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