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
        public List<CVTerm> attributes { get; set; }

        public PROXISpectrum()
        {
            mzs = new List<double>();
            intensities = new List<double>();
            attributes = new List<CVTerm>();
        }
    }
}
