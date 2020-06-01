using ThermoRawFileParser.Writer.MzML;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoFisher.CommonCore.Data.Business;
using MathNet.Numerics.Optimization;
using System.Collections.Generic;

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
            scanNumber = scanNr;

            spectrumId = CreateNativeID();
            centroided = false; //PDA spectra are profile

            // Get scan from the RAW file
            var scan = Scan.FromFile(rawFileRef, scanNr);

            ScanInfo = new ScanData
            {
                RetentionTime = rawFile.RetentionTimeFromScanNumber(scanNr),
                LowerLimit = scan.ScanStatistics.ShortWavelength,
                HigherLimit = scan.ScanStatistics.LongWavelength
            };

            BasePeakPosition = scan.ScanStatistics.BasePeakMass;
            BasePeakIntensity = scan.ScanStatistics.BasePeakIntensity;
            dataArrayLength = scan.SegmentedScan.PositionCount;

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
                spectrumCvParams.Add(new CVParamType
                {
                    name = "lowest observed wavelength",
                    accession = "MS:1000619",
                    value = LowestPosition.Value.ToString(),
                    unitCvRef = "MS",
                    unitAccession = "UO:0000018",
                    unitName = "nanometer",
                    cvRef = "UO"
                });
            }

            // Highest observed wavelength
            if (HighestPosition != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "highest observed wavelength",
                    accession = "MS:1000618",
                    value = HighestPosition.Value.ToString(),
                    unitAccession = "UO:0000018",
                    unitName = "nanometer",
                    unitCvRef = "UO",
                    cvRef = "MS"
                });
            }

            var spectrum = new SpectrumType
            {
                id = spectrumId,
                defaultArrayLength = dataArrayLength,
                scanList = ScanInfo.ToScanList(),
                cvParam = spectrumCvParams.ToArray(),
                binaryDataArrayList = GetBinaryDataArray(zLibCompression)
            };

            return spectrum;
        }
    }
}
