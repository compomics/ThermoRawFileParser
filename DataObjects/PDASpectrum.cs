using ThermoRawFileParser.Writer.MzML;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoFisher.CommonCore.Data.Business;
using MathNet.Numerics.Optimization;
using System.Collections.Generic;
using ThermoRawFileParser.Util;

namespace ThermoRawFileParser.DataObjects
{
    /// <summary>
    /// Spectrum formed by wavelength vs abundance pairs (produced by PDA device)
    /// </summary>
    public class PDASpectrum : Spectrum
    {
        public double[] Wavelengthes
        {
            get
            {
                return x;
            }
            set
            {
                x = value;
            }
        }

        public double[] Intensities
        {
            get
            {
                return y;
            }
            set
            {
                y = value;
            }
        }

        /// <summary>
        /// Create empty PDA spectrum
        /// </summary>
        public PDASpectrum()
        {
            dataTermX = new CVParamType
            {
                accession = "MS:1000617",
                name = "wavelength array",
                cvRef = "MS",
                unitName = "nanometer",
                value = "",
                unitCvRef = "UO",
                unitAccession = "UO:0000018"
            };

            dataTermY = new CVParamType
            {
                accession = "MS:1000515",
                name = "intensity array",
                cvRef = "MS",
                unitCvRef = "UO",
                unitAccession = "UO:0000269",
                unitName = "absorbance unit",
                value = ""
            };

            spectrumType = new CVParamType
            {
                name = "electromagnetic radiation spectrum",
                accession = "MS:1000804",
                value = "",
                cvRef = "MS"
            };
        }

        /// <summary>
        /// Create PDA spectrum from RawFile
        /// </summary>
        /// <param name="rawFile">RawFile object, **has to be open and PDA instrument selected**</param>
        /// <param name="instrumentNr">Number of the instrument in the RawFile</param>
        /// <param name="scanNr">Scan number in RawFile</param>
        public PDASpectrum(IRawDataPlus rawFile, int instrumentNr, int scanNr) : this()
        {
            rawFileRef = rawFile;
            deviceType = rawFile.SelectedInstrument.DeviceType;
            instrumentNumber = instrumentNr;
            scanNumber = scanNr;

            Centroided = false; //PDA spectra are profile

            // Get scan from the RAW file
            var scan = Scan.FromFile(rawFileRef, scanNr);

            // Fill ScanInformation
            ScanInfo = new ScanData
            {
                RetentionTime = rawFileRef.RetentionTimeFromScanNumber(scanNr),
                LowerLimit = scan.ScanStatistics.ShortWavelength,
                HigherLimit = scan.ScanStatistics.LongWavelength,
                unit = CVHelpers.nanometerUnit
            };

            BasePeakPosition = scan.ScanStatistics.BasePeakMass;
            BasePeakIntensity = scan.ScanStatistics.BasePeakIntensity;
            dataArrayLength = scan.SegmentedScan.PositionCount;

            //Spectrum Data
            if(dataArrayLength > 0)
            {
                x = scan.SegmentedScan.Positions;
                y = scan.SegmentedScan.Intensities;
            }

        }

        /// <summary>
        /// Convert object to MzML SpectrumType
        /// </summary>
        /// <param name="zLibCompression">Use ZLib compression of binary fields</param>
        /// <returns>MzML Spectrum Type</returns>
        public SpectrumType ToSpectrumType(bool zLibCompression)
        {
            // Keep the CV params in a list and convert to array afterwards
            var spectrumCvParams = new List<CVParamType>
            {
                spectrumType,
                GetScanTypeTerm()
            };

            // Lowest observed wavelength
            if (LowestPosition != null)
            {
                spectrumCvParams.Add(
                    CVHelpers.Copy(CVHelpers.nanometerUnit, name: "lowest observed wavelength",
                    accession: "MS:1000619", value: LowestPosition.Value.ToString(), cvRef: "MS"));
            }

            // Highest observed wavelength
            if (HighestPosition != null)
            {
                spectrumCvParams.Add(
                    CVHelpers.Copy(CVHelpers.nanometerUnit, name: "highest observed wavelength",
                    accession: "MS:1000618", value: HighestPosition.Value.ToString(), cvRef: "MS"));
            }

            //Fill all necessary fields of spectrum object
            var spectrum = new SpectrumType
            {
                id = CreateNativeID(),
                defaultArrayLength = dataArrayLength,
                scanList = ScanInfo.ToScanList(),
                cvParam = spectrumCvParams.ToArray(),
                binaryDataArrayList = GetBinaryDataArray(zLibCompression)
            };

            return spectrum;
        }
    }
}
