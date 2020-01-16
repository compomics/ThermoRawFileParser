using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ThermoRawFileParser.Util
{
    public static class PeptideData
    {
        public static readonly Dictionary<char, double> AAMasses = new Dictionary<char, double>
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

        public static readonly double Proton = 1.00727646677;
        public static readonly double H2O = 18.0105646837;

        public static readonly Regex invalidAA = new Regex(String.Format("[^{0}]", new String(PeptideData.AAMasses.Keys.ToArray())));
    }
    public class Peptide
    {
        public string Sequence { get; }

        
        public Peptide()
        {

        }

        public Peptide(string sequence)
        {
            if (IsValidSequence(sequence)) Sequence = sequence;
            else throw new Exception("Sequence have unknow amino acids");
        }

        public double GetMz(int z)
        {
            if (z == 0) throw new Exception("Charge cannot be zero!");

            double mass = Sequence.ToCharArray().Select(c => PeptideData.AAMasses[c]).Sum() + PeptideData.H2O;
            return (mass + z * PeptideData.Proton) / Math.Abs(z);
        }

        private bool IsValidSequence(string sequence)
        {
            return !PeptideData.invalidAA.IsMatch(sequence);
        }
    }
}
