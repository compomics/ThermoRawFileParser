using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using log4net;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Writer;
using ThermoRawFileParser.Util;

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

            //checking for symlinks
            var fileInfo = new FileInfo(queryParameters.rawFilePath);
            if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint)) //detected path is a symlink
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var realPath = NativeMethods.GetFinalPathName(queryParameters.rawFilePath);
                    Log.DebugFormat("Detected reparse point, real path: {0}", realPath);
                    queryParameters.UpdateRealPath(realPath);
                }
                else //Mono should handle all non-windows platforms
                {
                    var realPath = Path.Combine(Path.GetDirectoryName(queryParameters.rawFilePath), Mono.Unix.UnixPath.ReadLink(queryParameters.rawFilePath));
                    Log.DebugFormat("Detected reparse point, real path: {0}", realPath);
                    queryParameters.UpdateRealPath(realPath);
                }
            }

            using (rawFile = RawFileReaderFactory.ReadFile(queryParameters.rawFilePath))
            {
                Log.Info($"Started parsing {queryParameters.userFilePath}");

                if (!rawFile.IsOpen)
                {
                    throw new RawFileParserException("Unable to access the RAW file using the native Thermo library.");
                }

                // Check for any errors in the RAW file
                if (rawFile.IsError)
                {
                    throw new RawFileParserException(
                        $"RAW file cannot be processed because of an error - {rawFile.FileError}");
                }

                // Check if the RAW file is being acquired
                if (rawFile.InAcquisition)
                {
                    throw new RawFileParserException("RAW file cannot be processed since it is still being acquired");
                }

                // Get the number of instruments (controllers) present in the RAW file and set the 
                // selected instrument to the MS instrument, first instance of it
                rawFile.SelectInstrument(Device.MS, 1);

                // Set a cvGroup number counter
                var cvGroup = 1;

                foreach (var scanNumber in queryParameters.scanNumbers)
                {
                    var proxiSpectrum = new ProxiSpectrum();
                    
                    try
                    {
                        // Get each scan from the RAW file
                        var scan = Scan.FromFile(rawFile, scanNumber);

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

                        proxiSpectrum.AddAttribute(accession: "MS:1003057", name: "scan number",
                            value: scanNumber.ToString(CultureInfo.InvariantCulture));
                        proxiSpectrum.AddAttribute(accession: "MS:1000016", name: "scan start time",
                            value: (time * 60).ToString(CultureInfo.InvariantCulture));
                        proxiSpectrum.AddAttribute(accession: "MS:1000511", name: "ms level",
                            value: ((int) scanFilter.MSOrder).ToString(CultureInfo.InvariantCulture));

                        // trailer extra data list
                        ScanTrailer trailerData;
                        try
                        {
                            trailerData = new ScanTrailer(rawFile.GetTrailerExtraInformation(scanNumber));
                        }
                        catch (Exception ex)
                        {
                            Log.WarnFormat("Cannot load trailer infromation for scan {0} due to following exception\n{1}", scanNumber, ex.Message);
                            queryParameters.NewWarn();
                            trailerData = new ScanTrailer();
                        }

                        int? charge = trailerData.AsPositiveInt("Charge State:");
                        double? monoisotopicMz = trailerData.AsDouble("Monoisotopic M/Z:");
                        double? ionInjectionTime = trailerData.AsDouble("Ion Injection Time (ms):");
                        double? isolationWidth = trailerData.AsDouble("MS" + (int)scanFilter.MSOrder + " Isolation Width:");

                        //injection time
                        if (ionInjectionTime != null)
                        {
                            proxiSpectrum.AddAttribute(accession: "MS:1000927", name: "ion injection time",
                                value: ionInjectionTime.ToString(), cvGroup: cvGroup.ToString());
                            proxiSpectrum.AddAttribute(accession: "UO:0000028", name: "millisecond",
                                cvGroup: cvGroup.ToString());
                            cvGroup++;
                        }

                        if (reaction != null)
                        {
                            // Store the precursor information
                            var selectedIonMz =
                                SpectrumWriter.CalculateSelectedIonMz(reaction, monoisotopicMz, isolationWidth);
                            proxiSpectrum.AddAttribute(accession: "MS:1000744", name: "selected ion m/z",
                                value: selectedIonMz.ToString(CultureInfo.InvariantCulture));
                            proxiSpectrum.AddAttribute(accession: "MS:1000827",
                                name: "isolation window target m/z",
                                value: selectedIonMz.ToString(CultureInfo.InvariantCulture));

                            // Store the isolation window information
                            var offset = isolationWidth.Value / 2 + reaction.IsolationWidthOffset;
                            proxiSpectrum.AddAttribute(accession: "MS:1000828",
                                name: "isolation window lower offset",
                                value: (isolationWidth.Value - offset).ToString());
                            proxiSpectrum.AddAttribute(accession: "MS:1000829",
                                name: "isolation window upper offset",
                                value: offset.ToString());
                        }

                        // scan polarity
                        if (scanFilter.Polarity == PolarityType.Positive)
                        {
                            proxiSpectrum.AddAttribute(accession: "MS:1000465", name: "scan polarity",
                                value: "positive scan", valueAccession: "MS:1000130");
                        }
                        else
                        {
                            proxiSpectrum.AddAttribute(accession: "MS:1000465", name: "scan polarity",
                                value: "negative scan", valueAccession: "MS:1000129");
                        }

                        // charge state
                        if (charge != null)
                        {
                            proxiSpectrum.AddAttribute(accession: "MS:1000041", name: "charge state",
                                value: charge.ToString());
                        }

                        // write the filter string
                        proxiSpectrum.AddAttribute(accession: "MS:1000512", name: "filter string",
                            value: scanEvent.ToString());

                        double[] masses = null;
                        double[] intensities = null;

                        if (!queryParameters.noPeakPicking) // centroiding requested
                        {
                            // check if the scan has a centroid stream
                            if (scan.HasCentroidStream)
                            {
                                if (scan.CentroidScan.Length > 0)
                                {
                                    proxiSpectrum.AddAttribute(accession: "MS:1000525", name: "spectrum representation",
                                        value: "centroid spectrum", valueAccession: "MS:1000127");

                                    masses = scan.CentroidScan.Masses;
                                    intensities = scan.CentroidScan.Intensities;
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

                                    masses = segmentedScan.Positions;
                                    intensities = segmentedScan.Intensities;
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

                                masses = scan.SegmentedScan.Positions;
                                intensities = scan.SegmentedScan.Intensities;
                            }
                        }

                        if (masses != null && intensities != null)
                        {
                            Array.Sort(masses, intensities);

                            proxiSpectrum.AddMz(masses);
                            proxiSpectrum.AddIntensities(intensities);
                        }

                        resultList.Add(proxiSpectrum);
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetBaseException() is IndexOutOfRangeException)
                        {
                            Log.WarnFormat("Spectrum #{0} is outside of file boundries", scanNumber);
                            queryParameters.NewWarn();
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }
            }

            Log.Info($"Finished processing {queryParameters.userFilePath}");

            return resultList;
        }
    }
}