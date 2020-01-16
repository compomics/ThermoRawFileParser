using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ThermoRawFileParser.Util
{
    
    public class Peptide
    {
        public string Sequence { get; }

        private readonly Dictionary<char, double> AAMasses = new Dictionary<char, double>
    {
    { 'G', 57.02146 },
    { 'A', 71.03711 },
    { 'S', 87.03203 },
    { 'P', 97.05276 },
    { 'V', 99.06841 },
    { 'T', 101.04768 },
    { 'C', 103.00919 },
    { 'L', 113.08406 },
    { 'I', 113.08406 },
    { 'N', 114.04293 },
    { 'D', 115.02694 },
    { 'Q', 128.05858 },
    { 'K', 128.09496 },
    { 'E', 129.04259 },
    { 'M', 131.04049 },
    { 'H', 137.05891 },
    { 'F', 147.06841 },
    { 'U', 150.95364 },
    { 'R', 156.10111 },
    { 'Y', 163.06333 },
    { 'W', 186.07931 },
    { 'O', 237.14773 }
    };

        private readonly double proton = 1.00727646677;
        private readonly double h2o = 18.0105646837;

        private Regex invalidAA;
        public Peptide()
        {

        }

        public Peptide(string sequence)
        {
            invalidAA = new Regex(String.Format("[^{0}]", new String(AAMasses.Keys.ToArray())));
            if (IsValidSequence(sequence)) Sequence = sequence;
            else throw new Exception("Sequence have unknow amino acids");
        }

        public double GetMz(int z)
        {
            double mass = Sequence.ToCharArray().Select(c => AAMasses[c]).Sum() + h2o;
            return (mass + z * proton) / Math.Abs(z);
        }

        private bool IsValidSequence(string sequence)
        {
            var s = invalidAA.Matches(sequence);
            return !invalidAA.IsMatch(sequence);
        }
    }
}
