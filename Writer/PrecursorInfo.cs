using System.Collections.Generic;

namespace ThermoRawFileParser.Writer
{
    public class PrecursorInfo
    {
        public int MSLevel { get; set; }

        public int Scan { get; set; }

        public MzML.PrecursorType[] Precursors { get { return _precursors.ToArray(); } }

        private List<MzML.PrecursorType> _precursors;

        public PrecursorInfo()
        {
            Scan = 0;
            _precursors = new List<MzML.PrecursorType>();
        }

        public PrecursorInfo(int scan, MzML.PrecursorType[] precursors)
        {
            Scan = scan;
            _precursors = new List<MzML.PrecursorType>(precursors);
        }
    }
}
