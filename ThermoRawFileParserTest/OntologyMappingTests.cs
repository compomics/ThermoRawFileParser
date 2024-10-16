using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ThermoRawFileParser.Writer;

namespace ThermoRawFileParserTest
{
    [TestFixture]
    public class OntologyMappingTests
    {
        [Test]
        public void TestGetInstrumentModel()
        {
            // exact match
            var match = OntologyMapping.GetInstrumentModel("LTQ Orbitrap");
            Assert.AreEqual("MS:1000449", match.accession);
            // partial match, should return the longest partial match
            var partialMatch = OntologyMapping.GetInstrumentModel("LTQ Orbitrap XXL");
            Assert.AreEqual("MS:1000449", partialMatch.accession);
            // no match, should return the generic thermo instrument
            var noMatch = OntologyMapping.GetInstrumentModel("non existing model");
            Assert.AreEqual("MS:1000483", noMatch.accession);
        }
    }
}