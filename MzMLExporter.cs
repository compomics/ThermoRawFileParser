using System.IO;
using IO.MzML;
using IO.Thermo;
using MassSpectrometry;
using MzLibUtil;

namespace ThermoRawFileParser
{
    public class MzMLExporter
    {
        /// <summary>
        /// Extract the RAW file metadata and spectra in MGF format. 
        /// </summary>
        public void Extract()
        {
            //ThermoStaticData staticThermo = ThermoStaticData.LoadAllStaticData(@"spectra.raw");
            //ThermoDynamicData dynamicThermo = ThermoDynamicData.InitiateDynamicConnection(@"spectra.raw")
            //Mzml mzmlFile = Mzml.LoadAllStaticData(@"spectra.mzML");           
            
            double[] intensities1 = new double[] { 120.3, 230.5, 780.6, 490.5};
            double[] mz1 = new double[] { 16.3, 45.2, 78.5, 112.6 };
            MzmlMzSpectrum massSpec1 = new MzmlMzSpectrum(mz1, intensities1, false);
            IMzmlScan[] scans = new IMzmlScan[]{
                new MzmlScan(1, massSpec1, 1, true, Polarity.Positive, 1, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec1.SumOfAllY, null, "1")
            };
            
            //FakeMsDataFile f = new FakeMsDataFile(scans);
            //MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(, Path.Combine("/home/niels/Desktop/raw/test", "mzmlWithEmptyScan.mzML"), false);

            Mzml ok = Mzml.LoadAllStaticData(Path.Combine("/home/niels/Desktop/raw/test", "mzmlWithEmptyScan.mzML"));
            
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(ok, Path.Combine("/home/niels/Desktop/raw/test", "mzmlWithEmptyScan2.mzML"), false);

            var testFilteringParams = new FilteringParams(200, 0.01, 5, true, true);
            ok = Mzml.LoadAllStaticData(Path.Combine("/home/niels/Desktop/raw/test", "mzmlWithEmptyScan2.mzML"), testFilteringParams);
        }
        
    }
}