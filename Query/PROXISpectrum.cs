using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParser.Query
{
    public class PROXISpectrum
    {
        public List<double> mzs { get; set; }
        public List<double> intensities { get; set; }
        public List<PROXICVTerm> attributes { get; set; }

        public PROXISpectrum()
        {
            mzs = new List<double>();
            intensities = new List<double>();
            attributes = new List<PROXICVTerm>();
        }

        public void AddAttribute(string accession=null, string cvGroup=null, string name=null, string value=null, string valueAccession=null)
        {
            attributes.Add(new PROXICVTerm(accession, cvGroup, name, value, valueAccession));
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
