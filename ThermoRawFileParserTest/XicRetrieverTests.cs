using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ThermoRawFileParser.XIC;

namespace ThermoRawFileParserTest
{
    [TestFixture]
    public class XicRetrieverTests
    {
        [Test]
        public void testXicRetrieve()
        {
            XicData xicData = new XicData
            {
                content = new List<XicUnit>
                {
                    new XicUnit()
                    {
                        Meta = new XicMeta()
                        {
                            //MzStart = -1,
                            MzStart = 749.786,
                            MzEnd = 749.8093,
                            //MzEnd = -1,
                            RtStart = 0,
                            //RtStart = 2,
                            RtEnd = 900
                        }
                    }
                }
            };
            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"small.RAW");
            XicRetriever.RetrieveXic(testRawFile, true, xicData);
            Console.WriteLine("");
        }
        
        
    }
}