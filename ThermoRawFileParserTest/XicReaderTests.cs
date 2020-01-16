using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ThermoRawFileParser.XIC;

namespace ThermoRawFileParserTest
{
    [TestFixture]
    public class XicReaderTests
    {
        [Test]
        public void testXicRetrieve()
        {
            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"small.RAW");
            XicData xicData = new XicData
            {
                // test the full range
                content = new List<XicUnit>
                {
                    new XicUnit()
                    {
                        Meta = new XicMeta()
                        {
                            MzStart = -1,
                            //MzStart = 749.786,
                            //MzEnd = 749.8093,
                            MzEnd = -1,
                            RtStart = -1,
                            //RtStart = 2,
                            RtEnd = -1
                        }
                    }
                }
            };
            
            XicReader.ReadXic(testRawFile, true, xicData);
            Assert.AreEqual(xicData.content, "dijfijf");
        }
        
        
    }
}