using Namotion.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace ThermoRawFileParser.DataObjects
{
    /// <summary>
    /// Representation of Precursor information of mass spectrum
    /// </summary>
    public class PrecursorData
    {
        private IRawDataPlus rawFileRef;
        private int charge;

        public int Charge { get => charge; set => charge = value; }

        public double Mass { get; set; }

        public double Intensity { get; set; }

        public ActivationType Activation { get; set; }

        public int ScanNumber { get; set; }

        public MassSpectrum Spectrum => new MassSpectrum(rawFileRef, ScanNumber, true);

        public int MSOrder { get; set; }

        /// <summary>
        /// Build precursor information for a specific scan
        /// </summary>
        /// <param name="rawFile"></param>
        /// <param name="scanNr"></param>
        public PrecursorData(IRawDataPlus rawFile, int scanNr)
        {
            rawFileRef = rawFile;

            var trailer = rawFileRef.GetTrailerExtraInformation(scanNr);
            int.TryParse(trailer.TryGetPropertyValue<string>("Charge State:"), out charge);

            // Get the scan event for this scan number
            var scanEvent = rawFileRef.GetScanEventForScanNumber(scanNr);

            
        }
    }
}
