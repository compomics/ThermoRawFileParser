using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using log4net;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParser.Query
{
    public class ProxiSpectrumReader
    {
        private static readonly ILog Log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly QueryParameters queryParameters;

        public ProxiSpectrumReader(QueryParameters _queryParameters)
        {
            this.queryParameters = _queryParameters;
        }

        public List<ProxiSpectrum> Retrieve()
        {
            var resultList = new List<ProxiSpectrum>();
            IRawDataPlus rawFile;
            using (rawFile = RawFileReaderFactory.ReadFile(queryParameters.rawFilePath))
            {
                if (!rawFile.IsOpen)
                {
                    throw new RawFileParserException("Unable to access the RAW file using the native Thermo library.");
                }

                // Check for any errors in the RAW file
                if (rawFile.IsError)
                {
                    throw new RawFileParserException(
                        $"Error opening ({rawFile.FileError}) - {queryParameters.rawFilePath}");
                }

                // Check if the RAW file is being acquired
                if (rawFile.InAcquisition)
                {
                    throw new RawFileParserException("RAW file still being acquired - " + queryParameters.rawFilePath);
                }

                // Get the number of instruments (controllers) present in the RAW file and set the 
                // selected instrument to the MS instrument, first instance of it
                rawFile.SelectInstrument(Device.MS, 1);

                // Set a cvGroup number counter
                var cvGroup = 1;

                foreach (var scanNumber in queryParameters.scanNumbers)
                {
                    var proxiSpectrum = new ProxiSpectrum();
                    double monoisotopicMz = 0.0;
                    try
                    {
                        // Get each scan from the RAW file
                        var scan = Scan.FromFile(rawFile, scanNumber);

                        // Check to see if the RAW file contains label (high-res) data and if it is present
                        // then look for any data that is out of order
                        var time = rawFile.RetentionTimeFromScanNumber(scanNumber);

                        // Get the scan filter for this scan number
                        var scanFilter = rawFile.GetFilterForScanNumber(scanNumber);

                        // Get the scan event for this scan number
                        var scanEvent = rawFile.GetScanEventForScanNumber(scanNumber);

                        IReaction reaction = null;
                        if (scanEvent.MSOrder != MSOrderType.Ms)
                        {
                            reaction = SpectrumWriter.GetReaction(scanEvent, scanNumber);
                        }

                        proxiSpectrum.AddAttribute(accession: "MS:10003057", name: "scan number",
                            value: scanNumber.ToString(CultureInfo.InvariantCulture));
                        proxiSpectrum.AddAttribute(accession: "MS:10000016", name: "scan start time",
                            value: (time * 60).ToString(CultureInfo.InvariantCulture));
                        proxiSpectrum.AddAttribute(accession: "MS:1000511", name: "ms level",
                            value: ((int) scanFilter.MSOrder).ToString(CultureInfo.InvariantCulture));

                        // trailer extra data list
                        var trailerData = rawFile.GetTrailerExtraInformation(scanNumber);
                        var charge = 0;
                        var isolationWidth = 0.0;
                        for (var i = 0; i < trailerData.Length; i++)
                        {
                            if (trailerData.Labels[i] == "Ion Injection Time (ms):")
                            {
                                proxiSpectrum.AddAttribute(accession: "MS:10000927", name: "ion injection time",
                                    value: trailerData.Values[i], cvGroup: cvGroup.ToString());
                                proxiSpectrum.AddAttribute(accession: "UO:0000028", name: "millisecond",
                                    cvGroup: cvGroup.ToString());
                                cvGroup++;
                            }

                            if (trailerData.Labels[i] == "Charge State:")
                            {
                                charge = Convert.ToInt32(trailerData.Values[i]);
                            }

                            if (trailerData.Labels[i] == "Monoisotopic M/Z:")
                            {
                                monoisotopicMz = double.Parse(trailerData.Values[i], NumberStyles.Any,
                                    CultureInfo.CurrentCulture);
                            }

                            if (trailerData.Labels[i] == "MS" + (int) scanFilter.MSOrder + " Isolation Width:")
                            {
                                isolationWidth = double.Parse(trailerData.Values[i], NumberStyles.Any,
                                    CultureInfo.CurrentCulture);
                            }
                        }

                        if (reaction != null)
                        {
                            // Store the precursor information
                            var selectedIonMz =
                                SpectrumWriter.CalculateSelectedIonMz(reaction, monoisotopicMz, isolationWidth);
                            proxiSpectrum.AddAttribute(accession: "MS:10000744", name: "selected ion m/z",
                                value: selectedIonMz.ToString(CultureInfo.InvariantCulture));
                            proxiSpectrum.AddAttribute(accession: "MS:1000827",
                                name: "isolation window target m/z",
                                value: selectedIonMz.ToString(CultureInfo.InvariantCulture));

                            // Store the isolation window information
                            var isolationHalfWidth = isolationWidth / 2;
                            proxiSpectrum.AddAttribute(accession: "MS:1000828",
                                name: "isolation window lower offset",
                                value: isolationHalfWidth.ToString(CultureInfo.InvariantCulture));
                            proxiSpectrum.AddAttribute(accession: "MS:1000829",
                                name: "isolation window upper offset",
                                value: isolationHalfWidth.ToString(CultureInfo.InvariantCulture));
                        }

                        // scan polarity
                        if (scanFilter.Polarity == PolarityType.Positive)
                        {
                            proxiSpectrum.AddAttribute(accession: "MS:10000465", name: "scan polarity",
                                value: "positive scan", valueAccession: "MS:1000130");
                        }
                        else
                        {
                            proxiSpectrum.AddAttribute(accession: "MS:10000465", name: "scan polarity",
                                value: "negative scan", valueAccession: "MS:1000129");
                        }

                        // charge state
                        proxiSpectrum.AddAttribute(accession: "MS:10000041", name: "charge state",
                            value: charge.ToString(CultureInfo.InvariantCulture));

                        // write the filter string
                        proxiSpectrum.AddAttribute(accession: "MS:10000512", name: "filter string",
                            value: scanEvent.ToString());

                        if (!queryParameters.noPeakPicking) // centroiding requested
                        {
                            // check if the scan has a centroid stream
                            if (scan.HasCentroidStream)
                            {
                                if (scan.CentroidScan.Length > 0)
                                {
                                    proxiSpectrum.AddAttribute(accession: "MS:1000525", name: "spectrum representation",
                                        value: "centroid spectrum", valueAccession: "MS:1000127");

                                    proxiSpectrum.AddMz(scan.CentroidScan.Masses);
                                    proxiSpectrum.AddIntensities(scan.CentroidScan.Intensities);
                                }
                            }
                            else // otherwise take the low res segmented data
                            {
                                // if the spectrum is profile perform centroiding
                                var segmentedScan = scanEvent.ScanData == ScanDataType.Profile
                                    ? Scan.ToCentroid(scan).SegmentedScan
                                    : scan.SegmentedScan;

                                if (segmentedScan.PositionCount > 0)
                                {
                                    proxiSpectrum.AddAttribute(accession: "MS:1000525", name: "spectrum representation",
                                        value: "centroid spectrum", valueAccession: "MS:1000127");

                                    proxiSpectrum.AddMz(segmentedScan.Positions);
                                    proxiSpectrum.AddIntensities(segmentedScan.Intensities);
                                }
                            }
                        }
                        else // use the segmented data as is
                        {
                            if (scan.SegmentedScan.Positions.Length > 0)
                            {
                                switch (scanEvent.ScanData) //check if the data is centroided already
                                {
                                    case ScanDataType.Centroid:
                                        proxiSpectrum.AddAttribute(accession: "MS:1000525",
                                            name: "spectrum representation",
                                            value: "centroid spectrum", valueAccession: "MS:1000127");
                                        break;

                                    case ScanDataType.Profile:
                                        proxiSpectrum.AddAttribute(accession: "MS:1000525",
                                            name: "spectrum representation",
                                            value: "profile spectrum", valueAccession: "MS:1000128");
                                        break;
                                }

                                proxiSpectrum.AddMz(scan.SegmentedScan.Positions);
                                proxiSpectrum.AddIntensities(scan.SegmentedScan.Intensities);
                            }
                        }

                        resultList.Add(proxiSpectrum);
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetBaseException() is IndexOutOfRangeException)
                        {
                            // ignore
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
            }

            return resultList;
        }
    }
}