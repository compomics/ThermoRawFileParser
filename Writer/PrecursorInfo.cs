namespace ThermoRawFileParser.Writer
{
    /// <summary>
    /// Class that stores info from precursors
    /// </summary>
    public class PrecursorInfo
    {
        //Current MSLevel
        public int MSLevel { get; }

        //precursor scan number, 0 - means not a precursor
        public int Scan { get; }

        //technical field to store number of reactions the precursor has
        //every level of SA counts as additional reaction and thus we need to keep track of it
        public int ReactionCount { get; }

        //mzML-formatted precursor information for all levels
        public MzML.PrecursorType[] Precursors { get ; }

        public PrecursorInfo()
        {
            Scan = 0;
            ReactionCount = 0;
            Precursors = new MzML.PrecursorType[0];
        }

        public PrecursorInfo(int scan, int level, int reactionCount, MzML.PrecursorType[] precursors)
        {
            Scan = scan;
            MSLevel = level;
            ReactionCount = reactionCount;
            Precursors = precursors;
        }
    }
}
