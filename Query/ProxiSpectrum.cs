using System.Collections.Generic;
using System.Linq;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParser.Query
{
    public class ProxiSpectrum
    {
        public List<double> mzs { get; set; }
        public List<double> intensities { get; set; }
        public List<ProxiCvTerm> attributes { get; set; }

        public ProxiSpectrum()
        {
            mzs = new List<double>();
            intensities = new List<double>();
            attributes = new List<ProxiCvTerm>();
        }

        public void AddAttribute(string accession=null, string cvGroup=null, string name=null, string value=null, string valueAccession=null)
        {
            attributes.Add(new ProxiCvTerm(accession, cvGroup, name, value, valueAccession));
        }

        public void AddMz(IList<double> mzList)
        {
            mzs = mzList.ToList<double>();
        }

        public void AddIntensities(IList<double> intList)
        {
            intensities = intList.ToList<double>();
        }
        
    }
}
