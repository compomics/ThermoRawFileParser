using System;
using System.IO;
using System.Reflection;
using IO.Mgf;
using NUnit.Framework;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParserTest
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void TestMgf()
        {
            // get temp path
            String tempFilePath = Path.GetTempPath();

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"small.RAW");
            ParseInput parseInput = new ParseInput(testRawFile, tempFilePath, OutputFormat.Mgf, false, false, "coll", "run", "sub");        
            SpectrumWriter spectrumWriter = new MgfSpectrumWriter(parseInput);
            
            IRawDataPlus rawFile;
            using (rawFile = RawFileReaderFactory.ReadFile(parseInput.RawFilePath))
            {                                
                rawFile.SelectInstrument(Device.MS, 1);
                
                // Get the first and last scan from the RAW file
                int firstScanNumber = rawFile.RunHeaderEx.FirstSpectrum;
                int lastScanNumber = rawFile.RunHeaderEx.LastSpectrum;                                
                
                spectrumWriter.WriteSpectra(rawFile, firstScanNumber, lastScanNumber);

                                              
            }
            
            // do this for the mzLib library
            String tempFileName = Path.GetTempPath() + "elements.dat";           
            UsefulProteomicsDatabases.Loaders.LoadElements(tempFileName);  
            
            var loadAllStaticData = Mgf.LoadAllStaticData(Path.Combine(tempFilePath, "small.mgf"));
            Console.WriteLine("test");

//            ThermoStaticData staticThermo = ThermoStaticData.LoadAllStaticData(@"spectra.raw");
//            ThermoDynamicData dynamicThermo = ThermoDynamicData.InitiateDynamicConnection(@"spectra.raw")
//            Mzml mzmlFile = Mzml.LoadAllStaticData(@"spectra.mzML");           
//            
//            create temp file
//            String tempFileName = Path.GetTempPath() + "elements.dat";
//            
//            UsefulProteomicsDatabases.Loaders.LoadElements(tempFileName);
//            
//            double[] intensities1 = new double[] { 120.3, 230.5, 780.6, 490.5};
//            double[] mz1 = new double[] { 16.3, 45.2, 78.5, 112.6 };
//            MzmlMzSpectrum massSpec1 = new MzmlMzSpectrum(mz1, intensities1, false);
//            IMzmlScan[] scans = new IMzmlScan[]{
//                new MzmlScan(1, massSpec1, 1, true, Polarity.Positive, 1, new MzRange(1, 100), "f", MZAnalyzerType.Orbitrap, massSpec1.SumOfAllY, null, "1")
//            };
//            
//            FakeMsDataFile f = new FakeMsDataFile(scans);
//            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(f, Path.Combine("/home/niels/Desktop/raw/test", "mzmlWithEmptyScan.mzML"), false);
//
//            Mzml ok = Mzml.LoadAllStaticData(Path.Combine("/home/niels/Desktop/raw/test", "mzmlWithEmptyScan.mzML"));
//            
//            MzmlMethods.CreateAndWriteMyMzmlWithCalibratedSpectra(ok, Path.Combine("/home/niels/Desktop/raw/test", "mzmlWithEmptyScan2.mzML"), false);
//
//            var testFilteringParams = new FilteringParams(200, 0.01, 5, true, true);
//            //ok = Mzml.LoadAllStaticData(Path.Combine("/home/niels/Desktop/raw/test", "mzmlWithEmptyScan2.mzML"), testFilteringParams);
//            Assert.True(true);
        }
    }

}