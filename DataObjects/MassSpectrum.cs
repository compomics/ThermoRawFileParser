using System.Collections.Generic;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Util;
using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParser.DataObjects
{
    /// <summary>
    /// Mass spectrum - m/z vs spectral counts 
    /// </summary>
    public class MassSpectrum : Spectrum
    {
        public double[] Masses
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

        public double TIC { get; set; }

        public PolarityType Polarity { get; set; }

        public int MsOrder { get; set; }

        public PrecursorData PrecursorInfo { get; set; }

        public MassSpectrum()
        {
            dataTermX = new CVParamType
            {
                accession = "MS:1000514",
                name = "m/z array",
                cvRef = "MS",
                unitName = "m/z",
                value = "",
                unitCvRef = "MS",
                unitAccession = "MS:1000040"
            };

            dataTermY = new CVParamType
            {
                accession = "MS:1000515",
                name = "intensity array",
                cvRef = "MS",
                unitCvRef = "MS",
                unitAccession = "MS:1000131",
                unitName = "number of counts",
                value = ""
            };
        }

        /// <summary>
        /// Create Mass spectrum from RawFile
        /// </summary>
        /// <param name="rawFile">RawFile object, **has to be open and PDA instrument selected**</param>
        /// <param name="scanNr">Scan number in RawFile</param>
        /// <param name="doCentroiding">Perform centroiding of the data stream</param>
        public MassSpectrum(IRawDataPlus rawFile, int scanNr, bool doCentroiding) : this()
        {
            rawFileRef = rawFile;
            deviceType = rawFile.SelectedInstrument.DeviceType;
            instrumentNumber = 1;
            scanNumber = scanNr;

            // Get each scan from the RAW file
            var scan = Scan.FromFile(rawFileRef, scanNumber);

            // Get the scan event for this scan number
            var scanEvent = rawFileRef.GetScanEventForScanNumber(scanNumber);

            //Get trailer data
            var trailer = new TrailerData(rawFileRef.GetTrailerExtraInformation(scanNumber));

            Polarity = scanEvent.Polarity;
            MsOrder = (int)scanEvent.MSOrder;

            TIC = scan.ScanStatistics.TIC;

            //Get monoisotopic mass and injection time from trailer
            double injectionTime;
            trailer.TryGetDoubleValue("Ion Injection Time (ms)", out injectionTime);
            double monoisotopicMass;
            trailer.TryGetDoubleValue("Monoisotopic M/Z", out monoisotopicMass);

            //ScanWindow data
            ScanInfo = new ScanData
            {
                Filter = scanEvent.ToString(),
                RetentionTime = rawFileRef.RetentionTimeFromScanNumber(scanNr),
                LowerLimit = scan.ScanStatistics.LowMass,
                HigherLimit = scan.ScanStatistics.HighMass,
                InjectionTime = injectionTime,
                MonoisotopicMass = monoisotopicMass,
                unit = CVHelpers.massUnit
            };

            //precursor data
            if(MsOrder > 1)
                PrecursorInfo = new PrecursorData(rawFileRef, scanNumber, MsOrder, trailer);

            //Spectrum Data
            if (doCentroiding)
            {
                Centroided = true;
                // Check if the scan has a centroid stream
                if (scan.HasCentroidStream)
                {
                    BasePeakPosition = scan.CentroidScan.BasePeakMass;
                    BasePeakIntensity = scan.CentroidScan.BasePeakIntensity;
                    dataArrayLength = scan.CentroidScan.Length;
                    if (dataArrayLength > 0)
                    {
                        x = scan.CentroidScan.Masses;
                        y = scan.CentroidScan.Intensities;
                    }
                }
                else // otherwise take the segmented (low res) scan
                {
                    BasePeakPosition = scan.ScanStatistics.BasePeakMass;
                    BasePeakIntensity = scan.ScanStatistics.BasePeakIntensity;

                    // if the spectrum is profile perform centroiding
                    var segmentedScan = scanEvent.ScanData == ScanDataType.Profile
                        ? Scan.ToCentroid(scan).SegmentedScan
                        : scan.SegmentedScan;

                    dataArrayLength = segmentedScan.PositionCount;
                    if (dataArrayLength > 0)
                    { 
                        x = segmentedScan.Positions;
                        y = segmentedScan.Intensities;
                    }
                }
            }
            else // use the segmented data as is
            {
                BasePeakPosition = scan.ScanStatistics.BasePeakMass;
                BasePeakIntensity = scan.ScanStatistics.BasePeakIntensity;

                switch (scanEvent.ScanData) //check if the data centroided already
                {
                    case ScanDataType.Centroid:
                        Centroided = true;
                        break;
                    case ScanDataType.Profile:
                        Centroided = false;
                        break;
                }

                dataArrayLength = scan.SegmentedScan.PositionCount;
                if (dataArrayLength > 0)
                {
                    x = scan.SegmentedScan.Positions;
                    y = scan.SegmentedScan.Intensities;
                }
            }
        }

        /// <summary>
        /// Convert Mass Spectrum to MzML SpectrumType object
        /// </summary>
        /// <param name="zLibCompression">Use ZLib compression of binary fields</param>
        /// <returns></returns>
        public SpectrumType ToSpectrumType(bool zLibCompression)
        {
            var spectrumCvParams = new List<CVParamType>();

            spectrumCvParams.Add(GetSpectrumType());
            spectrumCvParams.Add(GetScanTypeTerm());
            spectrumCvParams.Add(GetPolarityTerm());

            spectrumCvParams.Add(new CVParamType
            {
                accession = "MS:1000511",
                cvRef = "MS",
                name = "ms level",
                value = MsOrder.ToString()
            });

            // Total ion current
            spectrumCvParams.Add(new CVParamType
            {
                name = "total ion current",
                accession = "MS:1000285",
                value = TIC.ToString(),
                cvRef = "MS"
            });

            // Base peak m/z
            if (BasePeakPosition != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "base peak m/z",
                    accession = "MS:1000504",
                    value = BasePeakPosition.ToString(),
                    unitCvRef = "MS",
                    unitName = "m/z",
                    unitAccession = "MS:1000040",
                    cvRef = "MS"
                });
            }

            // Base peak intensity
            if (BasePeakIntensity != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "base peak intensity",
                    accession = "MS:1000505",
                    value = BasePeakIntensity.ToString(),
                    unitCvRef = "MS",
                    unitName = "number of detector counts",
                    unitAccession = "MS:1000131",
                    cvRef = "MS"
                });
            }

            // Lowest observed mz
            if (LowestPosition != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "lowest observed m/z",
                    accession = "MS:1000528",
                    value = LowestPosition.ToString(),
                    unitCvRef = "MS",
                    unitAccession = "MS:1000040",
                    unitName = "m/z",
                    cvRef = "MS"
                });
            }

            // Highest observed mz
            if (HighestPosition != null)
            {
                spectrumCvParams.Add(new CVParamType
                {
                    name = "highest observed m/z",
                    accession = "MS:1000527",
                    value = HighestPosition.ToString(),
                    unitAccession = "MS:1000040",
                    unitName = "m/z",
                    unitCvRef = "MS",
                    cvRef = "MS"
                });
            }

            var spectrum = new SpectrumType
            {
                id = CreateNativeID(),
                defaultArrayLength = dataArrayLength,
                cvParam = spectrumCvParams.ToArray(),
                scanList = ScanInfo.ToScanList(),
                binaryDataArrayList = GetBinaryDataArray(zLibCompression)
            };

            if (PrecursorInfo != null)
                spectrum.precursorList = PrecursorInfo.ToPrecursorList();

            return spectrum;
        }
        public CVParamType GetPolarityTerm()
        {
            if (Polarity == PolarityType.Positive)
                return new CVParamType { accession = "MS:1000130", cvRef = "MS", name = "positive scan", value = "" };
            else if (Polarity == PolarityType.Negative)
                return new CVParamType { accession = "MS:1000129", cvRef = "MS", name = "negative scan", value = "" };
            else
                return new CVParamType();
        }

        public CVParamType GetSpectrumType()
        {
            if (MsOrder == 1)
                return new CVParamType { accession = "MS:1000579", cvRef = "MS", name = "MS1 spectrum", value = "" };
            else
                return new CVParamType { accession = "MS:1000580", cvRef = "MS", name = "MSn spectrum", value = "" };
        }
    }
}
