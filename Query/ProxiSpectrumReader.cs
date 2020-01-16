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


        private const string PositivePolarity = "+";
        private const string NegativePolarity = "-";

        private QueryParameters queryParameters;

        public ProxiSpectrumReader(QueryParameters _queryParameters)
        {
            this.queryParameters = _queryParameters;
        }

        public List<PROXISpectrum> Retrieve()
        {
            List<PROXISpectrum> resultList = new List<PROXISpectrum>();
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
                int cvGroup = 1;

                foreach (int scanNumber in queryParameters.scanNumbers)
                {
                    var proxiSpectrum = new PROXISpectrum();
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
                        switch (scanFilter.MSOrder)
                        {
                            case MSOrderType.Ms2:
                                try
                                {
                                    reaction = scanEvent.GetReaction(0);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    Log.Warn("No reaction found for scan " + scanNumber);
                                }
                                
                                goto default;
                            case MSOrderType.Ms3:
                            {
                                try
                                {
                                    reaction = scanEvent.GetReaction(1);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    Log.Warn("No reaction found for scan " + scanNumber);
                                }

                                goto default;
                            }
                            default:
                                proxiSpectrum.AddAttribute(accession: "MS:10003057", name: "scan number",
                                    value: scanNumber.ToString(CultureInfo.InvariantCulture));
                                proxiSpectrum.AddAttribute(accession: "MS:10000016", name: "scan start time",
                                    value: (time * 60).ToString(CultureInfo.InvariantCulture));
                                proxiSpectrum.AddAttribute(accession: "MS:1000511", name: "ms level",
                                    value: ((int) scanFilter.MSOrder).ToString(CultureInfo.InvariantCulture));

                                // trailer extra data list
                                var trailerData = rawFile.GetTrailerExtraInformation(scanNumber);
                                int charge = 0;
                                double? monoisotopicMz = null;
                                double? isolationWidth = null;
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
                                    var selectedIonMz =
                                        SpectrumWriter.CalculateSelectedIonMz(reaction, monoisotopicMz, isolationWidth);
                                    proxiSpectrum.AddAttribute(accession: "MS:10000744", name: "selected ion m/z",
                                        value: selectedIonMz.ToString(CultureInfo.InvariantCulture));
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

                                // Check if the scan has a centroid stream
                                if (scan.HasCentroidStream && (scanEvent.ScanData == ScanDataType.Centroid ||
                                                               (scanEvent.ScanData == ScanDataType.Profile &&
                                                                !queryParameters.noPeakPicking)))
                                {
                                    var centroidStream = rawFile.GetCentroidStream(scanNumber, false);
                                    if (scan.CentroidScan.Length > 0)
                                    {
                                        proxiSpectrum.AddMz(centroidStream.Masses);
                                        proxiSpectrum.AddIntensities(centroidStream.Intensities);
                                    }
                                }
                                // Otherwise take the profile data
                                else
                                {
                                    // Get the scan statistics from the RAW file for this scan number
                                    var scanStatistics = rawFile.GetScanStatsForScanNumber(scanNumber);

                                    // Get the segmented (low res and profile) scan data
                                    var segmentedScan =
                                        rawFile.GetSegmentedScanFromScanNumber(scanNumber, scanStatistics);
                                    proxiSpectrum.AddMz(segmentedScan.Positions);
                                    proxiSpectrum.AddIntensities(segmentedScan.Intensities);
                                }

                                resultList.Add(proxiSpectrum);
                                break;
                        }
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