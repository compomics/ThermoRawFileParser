using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ThermoFisher.CommonCore.Data;
using ThermoRawFileParser;
using ThermoRawFileParser.XIC;

namespace ThermoRawFileParserTest
{
    [TestFixture]
    public class XicReaderTests
    {
        [Test]
        public void testXicReadFullRange()
        {
            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data/small.RAW");
            XicData xicData = new XicData
            {
                // test the full range
                Content = new List<XicUnit>
                {
                    new XicUnit()
                    {
                        Meta = new XicMeta()
                        {
                            MzStart = null,
                            MzEnd = null,
                            RtStart = null,
                            RtEnd = null
                        }
                    }
                }
            };

            XicParameters xicparams = new XicParameters();

            XicReader.ReadXic(testRawFile, false, xicData, ref xicparams);
            XicUnit xicUnit = xicData.Content[0];
            Assert.AreEqual(14, ((Array) xicUnit.RetentionTimes).Length);
            Assert.AreEqual(14, ((Array) xicUnit.Intensities).Length);
            Assert.AreEqual(140, xicUnit.Meta.MzStart.Value, 0.01);
            Assert.AreEqual(2000, xicUnit.Meta.MzEnd.Value, 0.01);
            Assert.AreEqual(0.004935, xicUnit.Meta.RtStart.Value, 0.01);
            Assert.AreEqual(0.4872366666, xicUnit.Meta.RtEnd.Value, 0.01);
        }

        [Test]
        public void testXicRead()
        {
            var testRawFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data/small2.RAW");
            XicData xicData = new XicData
            {
                // test the full retention time range
                Content = new List<XicUnit>
                {
                    new XicUnit()
                    {
                        Meta = new XicMeta()
                        {
                            MzStart = 749.786,
                            MzEnd = 749.8093,
                            RtStart = null,
                            RtEnd = null
                        }
                    }
                }
            };

            XicParameters xicparams = new XicParameters();

            XicReader.ReadXic(testRawFile, false, xicData, ref xicparams);
            XicUnit xicUnit = xicData.Content[0];
            Assert.AreEqual(46, ((Array) xicUnit.RetentionTimes).Length);
            Assert.AreEqual(46, ((Array) xicUnit.Intensities).Length);
            Assert.AreEqual(749.786, xicUnit.Meta.MzStart.Value, 0.01);
            Assert.AreEqual(749.8093, xicUnit.Meta.MzEnd.Value, 0.01);
            Assert.AreEqual(10, xicUnit.Meta.RtStart.Value, 0.01);
            Assert.AreEqual(10.98, xicUnit.Meta.RtEnd.Value, 0.01);
            
            xicData = new XicData
            {
                // test the nonsensical retention time range
                Content = new List<XicUnit>
                {
                    new XicUnit()
                    {
                        Meta = new XicMeta()
                        {
                            MzStart = 749.786,
                            MzEnd = 749.8093,
                            RtStart = 300,
                            RtEnd = 400
                        }
                    }
                }
            };
            XicReader.ReadXic(testRawFile, false, xicData, ref xicparams);
            xicUnit = xicData.Content[0];
            Assert.AreEqual(1, ((Array) xicUnit.RetentionTimes).Length);
            Assert.AreEqual(1, ((Array) xicUnit.Intensities).Length);
            Assert.AreEqual(749.786, xicUnit.Meta.MzStart.Value, 0.01);
            Assert.AreEqual(749.8093, xicUnit.Meta.MzEnd.Value, 0.01);
            Assert.AreEqual(300, xicUnit.Meta.RtStart.Value, 0.01);
            Assert.AreEqual(400, xicUnit.Meta.RtEnd.Value, 0.01);
        }

        [Test]
        public void testValidateJson()
        {
            string json = @"[
        {
            'mz':488.5384,
            'tolerance':10,
            'tolerance_unit':'ppm',
            'scan_filter':'ms'           
        },
        {
            'mz':575.2413,
            'tolerance':10,
            'tolerance_unit':'ppm'
        },
        {
            'mz_start':749.7860,
            'mz_end' : 750.4,            
            'rt_start':630,
            'rt_end':660
        },
        {
            'sequence':'LENNART',
            'tolerance':10,
            'rt_start':630,
            'rt_end':660
        },
        {
            'mz':575.2413,
            'tolerance':10,
            'tolerance_unit':'ppm',
            'comment': 'this is comment'
        }
        ]";

            // test a valid json
            var errors = JSONParser.ValidateJson(json);
            Assert.True(errors.IsNullOrEmpty());

            json = @"[
        {
            'mz':488.5384,
            'tolerance_unit':'ppm'           
        },
        {
            'mz':575.2413,
            'tolerance':10,
            'tolerance_unit':'ppm'
        },
        {
            'mz_start':749.7860,
            'mz_end' : 750.4, 
            'rt_start':630,
            'rt_end':660
        },
        {
            'sequence':'LENNART',
            'rt_start':630,
            'rt_end':660
        }
        ]";

            // test a json with 2 missing properties
            errors = JSONParser.ValidateJson(json);
            Assert.False(errors.IsNullOrEmpty());
            Assert.AreEqual(2, errors.Count);

            json = @"[
        {
            'mz': -488.5384,
            'tolerance':10,
            'tolerance_unit':'ppm'           
        },
        {
            'mz':575.2413,
            'tolerance':10,
            'tolerance_unit':'ppm'
        },
        {
            'mz_start':749.7860,
            'mz_end' : 750.4, 
            'rt_start': -630,
            'rt_end': 660
        },
        {
            'sequence':'LENNART',
            'tolerance':10,
            'rt_start': 630,
            'rt_end': 660
        }
        ]";

            // test a json with 2 negative numbers
            errors = JSONParser.ValidateJson(json);
            Assert.False(errors.IsNullOrEmpty());
            Assert.AreEqual(2, errors.Count);
        }

        [Test]
        public void testParseJson()
        {
            string json = @"[
        {
            'mz': 488.5384,
            'tolerance':10,
            'tolerance_unit':'ppm',
            'scan_filter': 'ms2'          
        },
        {
            'mz':575.2413,
            'tolerance':10,
        },
        {
            'mz_start':749.7860,
            'mz_end' : 750.4, 
            'rt_start': 630,
            'rt_end': 660
        },
        {
            'sequence':'LENNART',
            'tolerance':10,
            'rt_start': 630,
            'rt_end': 660
        }
        ]";

            var xicData = JSONParser.ParseJSON(json);
            Assert.NotNull(xicData);

            json = @"[
        {
            'mz': 488.5384,
            'tolerance':10,
            'tolerance_unit':'ppm'           
        },
        {
            'mz':575.2413,
            'tolerance':10,
        },
        {
            'mz_start':749.7860,
            'mz_end' : 750.4, 
            'rt_start': 680,
            'rt_end': 660
        },
        {
            'sequence':'LENNART',
            'tolerance':10,
            'rt_start': 630,
            'rt_end': 660
        }
        ]";

            Assert.Throws<RawFileParserException>(() => JSONParser.ParseJSON(json));

            json = @"[
        {
            'mz': 488.5384,
            'tolerance':10,
            'tolerance_unit':'ppm'           
        },
        {
            'mz':575.2413,
            'tolerance':10,
        },
        {
            'mz_start':849.7860,
            'mz_end' : 750.4, 
            'rt_start': 630,
            'rt_end': 660
        },
        {
            'sequence':'LENNART',
            'tolerance':10,
            'rt_start': 630,
            'rt_end': 660
        }
        ]";

            Assert.Throws<RawFileParserException>(() => JSONParser.ParseJSON(json));
        }
    }
}