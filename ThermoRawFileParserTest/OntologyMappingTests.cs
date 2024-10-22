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
            Assert.That(match.accession, Is.EqualTo("MS:1000449"));
            // partial match, should return the longest partial match
            var partialMatch = OntologyMapping.GetInstrumentModel("LTQ Orbitrap XXL");
            Assert.That(partialMatch.accession, Is.EqualTo("MS:1000449"));
            // no match, should return the generic thermo instrument
            var noMatch = OntologyMapping.GetInstrumentModel("non existing model");
            Assert.That(noMatch.accession, Is.EqualTo("MS:1000483"));
        }
    }
}