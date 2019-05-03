using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ThermoRawFileParserTest
{
    [TestFixture]
    public class UtilTests
    {
        [Test]
        public void TestRegex()
        {
            const string filterString = "ITMS + c NSI r d Full ms2 961.8803@cid35.00 [259.0000-1934.0000]";
            const string pattern = @"ms2 (.*?)@";
            
            Match result = Regex.Match(filterString, pattern);
            if (result.Success)
            {
                Assert.AreEqual("961.8803", result.Groups[1].Value);
            }
            else
            {
                Assert.Fail();
            }
        }
    }
}