using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.Writer
{
    public interface ISpectrumWriter
    {
        /// <summary>
        /// Write the RAW files' spectra to a file.
        /// </summary>
        /// <param name="rawFile">the RAW file interface</param>
        /// <param name="firstScanNumber">the first scan number</param>
        /// <param name="lastScanNumber">the last scan number</param>
        void Write(IRawDataPlus rawFile, int firstScanNumber, int lastScanNumber);
    }
}