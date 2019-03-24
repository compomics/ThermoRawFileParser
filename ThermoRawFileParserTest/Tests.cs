using System;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Serialization;
using IO.Mgf;
using IO.MzML;
using NUnit.Framework;
using ThermoRawFileParser;
using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParserTest
{
    [TestFixture]
    public class Tests
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [Test]
        public void TestMgf()
        {
            // Get temp path for writing the test MGF
            var tempFilePath = Path.GetTempPath();

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"small.RAW");
            var parseInput = new ParseInput(testRawFile, tempFilePath, OutputFormat.Mgf, false, MetadataFormat.NONE,
                null, null, null, null, false);

            RawFileParser.Parse(parseInput);

            // Do this for the mzLib library issue
            var tempFileName = Path.GetTempPath() + "elements.dat";
            UsefulProteomicsDatabases.Loaders.LoadElements(tempFileName);

            var mgfData = Mgf.LoadAllStaticData(Path.Combine(tempFilePath, "small.mgf"));
            Assert.AreEqual(34, mgfData.NumSpectra);
            Assert.IsEmpty(mgfData.GetMS1Scans());
        }

        [Test]
        public void TestMzml()
        {
            // Get temp path for writing the test mzML
            var tempFilePath = Path.GetTempPath();

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"small.RAW");
            var parseInput = new ParseInput(testRawFile, tempFilePath, OutputFormat.Mzml, false, MetadataFormat.NONE,
                null, null, null, null, false);

            RawFileParser.Parse(parseInput);

            // Deserialize the mzML file
            var xmlSerializer = new XmlSerializer(typeof(mzMLType));
            var testMzMl = (mzMLType) xmlSerializer.Deserialize(new FileStream(
                Path.Combine(tempFilePath, "small.mzML"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            Assert.AreEqual("48", testMzMl.run.spectrumList.count);
            Assert.AreEqual(48, testMzMl.run.spectrumList.spectrum.Length);

            Assert.AreEqual("1", testMzMl.run.chromatogramList.count);
            Assert.AreEqual(1, testMzMl.run.chromatogramList.chromatogram.Length);
        }

        [Test]
        public void TestIndexedMzML()
        {
            // Get temp path for writing the test mzML
            var tempFilePath = Path.GetTempPath();

            Console.WriteLine(tempFilePath);

            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"small.RAW");
            var parseInput = new ParseInput(testRawFile, tempFilePath, OutputFormat.IndexMzML, false,
                MetadataFormat.NONE,
                null, null, null, null, false);

            RawFileParser.Parse(parseInput);

            // Deserialize the mzML file
            var xmlSerializer = new XmlSerializer(typeof(indexedmzML));
            var testMzMl = (indexedmzML) xmlSerializer.Deserialize(new FileStream(
                Path.Combine(tempFilePath, "small.mzML"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

            Assert.AreEqual("48", testMzMl.mzML.run.spectrumList.count);
            Assert.AreEqual(48, testMzMl.mzML.run.spectrumList.spectrum.Length);

            Assert.AreEqual("1", testMzMl.mzML.run.chromatogramList.count);
            Assert.AreEqual(1, testMzMl.mzML.run.chromatogramList.chromatogram.Length);

            Assert.AreEqual(2, testMzMl.indexList.index.Length);
            Assert.AreEqual("spectrum", testMzMl.indexList.index[0].name.ToString());
            Assert.AreEqual(48, testMzMl.indexList.index[0].offset.Length);
            Assert.AreEqual("chromatogram", testMzMl.indexList.index[1].name.ToString());
            Assert.AreEqual(1, testMzMl.indexList.index[1].offset.Length);
        }
    }
}