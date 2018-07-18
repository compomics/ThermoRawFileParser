using System;
using System.IO;
using System.Xml.Serialization;
using IO.Mgf;
using NUnit.Framework;
using ThermoRawFileParser;
using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParserTest
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public void TestMgf()
        {
            // Get temp path for writing the test MGF
            String tempFilePath = Path.GetTempPath();

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"small.RAW");
            ParseInput parseInput = new ParseInput(testRawFile, tempFilePath, OutputFormat.Mgf, false, false, false,
                "coll",
                "run", "sub");

            RawFileParser rawFileParser = new RawFileParser();
            rawFileParser.Parse(parseInput);

            // Do this for the mzLib library issue
            String tempFileName = Path.GetTempPath() + "elements.dat";
            UsefulProteomicsDatabases.Loaders.LoadElements(tempFileName);

            var mgfData = Mgf.LoadAllStaticData(Path.Combine(tempFilePath, "small.mgf"));
            Assert.AreEqual(34, mgfData.NumSpectra);
            Assert.IsEmpty(mgfData.GetMS1Scans());
        }

        [Test]
        public void TestMzml()
        {
            // Get temp path for writing the test mzML
            String tempFilePath = Path.GetTempPath();

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"small.RAW");
            ParseInput parseInput = new ParseInput(testRawFile, tempFilePath, OutputFormat.Mzml, false, false, false,
                "coll", "run", "sub");

            RawFileParser rawFileParser = new RawFileParser();
            rawFileParser.Parse(parseInput);

            // Deserialize the mzML file
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(mzMLType));
            mzMLType testMzMl = (mzMLType) xmlSerializer.Deserialize(new FileStream(
                Path.Combine(tempFilePath, "small.mzML"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            Assert.AreEqual("48", testMzMl.run.spectrumList.count);
            Assert.AreEqual(48, testMzMl.run.spectrumList.spectrum.Length);

            Assert.AreEqual("1", testMzMl.run.chromatogramList.count);
            Assert.AreEqual(1, testMzMl.run.chromatogramList.chromatogram.Length);
        }
    }
}