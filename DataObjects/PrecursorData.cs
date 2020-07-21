using System.Collections.Generic;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Util;
using ThermoRawFileParser.Writer;
using ThermoRawFileParser.Writer.MzML;

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

        private int masterScan;
        public int ScanNumber { get => masterScan; set => masterScan = value; }

        public MassSpectrum Spectrum => new MassSpectrum(rawFileRef, ScanNumber, true);

        public int MSOrder { get; set; }

        public double IsolationWidth { get; set; }

        public double IsolationOffset { get; set; }

        public double CollisionEnergy { get; set; }

        /// <summary>
        /// Build precursor information for a specific scan
        /// </summary>
        /// <param name="rawFile"></param>
        /// <param name="scanNr"></param>
        /// <param name="trailer"></param>
        public PrecursorData(IRawDataPlus rawFile, int scanNr, int msOrder, TrailerData trailer)
        {
            rawFileRef = rawFile;

            trailer.TryGetIntValue("Charge State", out charge);
            trailer.TryGetIntValue("Master Index", out masterScan);

            MSOrder = (int)rawFileRef.GetScanEventForScanNumber(masterScan).MSOrder;

            // Get the scan event for this scan number
            var scanEvent = rawFileRef.GetScanEventForScanNumber(scanNr);

            var reaction = scanEvent.GetReaction(MSOrder - 1);

            Mass = reaction.PrecursorMass;
            Activation = reaction.ActivationType;
            IsolationWidth = reaction.IsolationWidth;
            IsolationOffset = reaction.IsolationWidthOffset;

            CollisionEnergy = reaction.CollisionEnergyValid ? reaction.CollisionEnergy : 0;
        }

        public PrecursorListType ToPrecursorList()
        {
            PrecursorListType result = new PrecursorListType();

            List<PrecursorType> precursors = new List<PrecursorType>();

            PrecursorType precursor = new PrecursorType();

            precursor.spectrumRef = Spectrum.CreateNativeID();

            //isolation window
            CVParamType[] isolationWindowParams = new CVParamType[]
            {
                CVHelpers.Copy(
                    CVHelpers.massUnit,
                    accession: "MS:1000827",
                    name: "isolation window target m/z",
                    cvRef: "MS",
                    value: Mass.ToString()
                    ),
                  CVHelpers.Copy(
                    CVHelpers.massUnit,
                    accession: "MS:1000828",
                    name: "isolation window lower offset",
                    cvRef: "MS",
                    value: (IsolationWidth / 2 - IsolationOffset).ToString()
                    ),
                  CVHelpers.Copy(
                    CVHelpers.massUnit,
                    accession: "MS:1000829",
                    name: "isolation window upper offset",
                    cvRef: "MS",
                    value: (IsolationWidth / 2 + IsolationOffset).ToString()
                    )
            };

            precursor.isolationWindow = new ParamGroupType { cvParam = isolationWindowParams };

            //selectedIon list
            List<ParamGroupType> selectedIons = new List<ParamGroupType>();

            ParamGroupType selectedIon = new ParamGroupType
            {
                cvParam = new CVParamType[]
                {
                  CVHelpers.Copy(
                    CVHelpers.massUnit,
                    accession: "MS:1000744",
                    name: "selected ion m/z",
                    cvRef: "MS",
                    value: Mass.ToString()
                    ),
                  new CVParamType
                  {
                      accession = "MS:1000041",
                      cvRef = "MS",
                      name = "charge state",
                      value = Charge.ToString()
                  },
                  new CVParamType
                  {
                      accession = "MS:1000042",
                      cvRef = "MS",
                      name = "intensity",
                      value = Intensity.ToString()
                  }
                }
            };

            selectedIons.Add(selectedIon);

            precursor.selectedIonList = new SelectedIonListType
            {
                count = selectedIons.Count.ToString(),
                selectedIon = selectedIons.ToArray()
            };

            //activation
            if (!OntologyMapping.DissociationTypes.TryGetValue(Activation, out var activation))
            {
                activation = new CVParamType
                {
                    accession = "MS:1000044",
                    name = "Activation Method",
                    cvRef = "MS",
                    value = ""
                };
            }

            CVParamType[] activationParams = new CVParamType[]
            {
                activation,
                new CVParamType
                {
                    accession = "MS:1000045",
                    cvRef = "MS",
                    name = "collision energy",
                    unitAccession = "UO:0000266",
                    unitCvRef = "UO",
                    unitName = "electronvolt",
                    value = CollisionEnergy.ToString()
                }
            };

            precursor.activation = new ParamGroupType
            {
                cvParam = activationParams
            };

            precursors.Add(precursor);

            result.count = precursors.Count.ToString();
            result.precursor = precursors.ToArray();

            return result;
        }
    }
}
