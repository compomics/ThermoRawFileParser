using System;
using System.IO;
using System.Net;
using System.Reflection;
using IO.MzML;
using MassSpectrometry;
using MzLibUtil;
using NUnit.Framework;

namespace ThermoRawFileParserTest
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void Test1()
        {
            //ThermoStaticData staticThermo = ThermoStaticData.LoadAllStaticData(@"spectra.raw");
            //ThermoDynamicData dynamicThermo = ThermoDynamicData.InitiateDynamicConnection(@"spectra.raw")
            //Mzml mzmlFile = Mzml.LoadAllStaticData(@"spectra.mzML");           
            
            //create temp file
            String tempFileName = Path.GetTempPath() + "elements.dat";
            //string tempFileName = Path.GetTempFileName();
            Console.WriteLine("fdffffffffff" + File.Exists(tempFileName));
            
            UsefulProteomicsDatabases.Loaders.LoadElements(tempFileName);
            
            double[] intensities1 = new double[] { 120.3, 230.5, 780.6, 490.5};
            double[] mz1 = new double[] { 16.3, 45.2, 78.5, 112.6 };
            MzmlMzSpectrum massSpec1 = new MzmlMzSpectrum(mz1, intensities1, false);
            IMzmlScan[] scans = new IMzmlScan[]{
                new MzmlScan(1, massSpec1, 1, true, Polarity.Positive, 1, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec1.SumOfAllY, null, "1")
            };
            
            FakeMsDataFile f = new FakeMsDataFile(scans);
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(f, Path.Combine("/home/niels/Desktop/raw/test", "mzmlWithEmptyScan.mzML"), false);

            Mzml ok = Mzml.LoadAllStaticData(Path.Combine("/home/niels/Desktop/raw/test", "mzmlWithEmptyScan.mzML"));
            
            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(ok, Path.Combine("/home/niels/Desktop/raw/test", "mzmlWithEmptyScan2.mzML"), false);

            var testFilteringParams = new FilteringParams(200, 0.01, 5, true, true);
            ok = Mzml.LoadAllStaticData(Path.Combine("/home/niels/Desktop/raw/test", "mzmlWithEmptyScan2.mzML"), testFilteringParams);
            Assert.True(true);
        }
    }

}