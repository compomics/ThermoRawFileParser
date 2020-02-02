using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NUnit.Framework;
using ThermoFisher.CommonCore.Data;
using ThermoRawFileParser.XIC;

namespace ThermoRawFileParserTest
{
    [TestFixture]
    public class XicReaderTests
    {
        [Test]
        public void testXicRead()
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
                            //RtStart = -1,
                            RtStart = 0.1,
                            //RtEnd = -1
                            RtEnd = 0.01
                        }
                    }
                }
            };

            XicReader.ReadXic(testRawFile, true, xicData);
            //Assert.AreEqual(xicData.content, "dijfijf");
            JsonSchemaGenerator generator = new JsonSchemaGenerator();

            JsonSchema schema = generator.Generate(typeof(XicData));
            Console.Write(schema.ToString());
        }

        [Test]
        public void testValidate()
        {
            string json = @"[
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

            string jsonschema = @"{
  'type': 'array',
  'title': 'The XIC input Schema',
  'items': {
    '$id': '#/items',
    'type': 'object',
    'title': 'The XIC items Schema',
    'anyOf' : [
            {'required' : ['mz', 'tolerance']},
            {'required' : ['mz_start', 'mz_end']},
            {'required' : ['sequence']}
            ],   
    'properties': {
      'mz': {
        '$id': '#/items/properties/mz',
        'type': 'number',
        'minimum': 0,
        'title': 'The Mz Schema',        
      },
      'tolerance': {
        '$id': '#/items/properties/tolerance',
        'type': 'number',
        'minimum': 0,
        'title': 'The Tolerance Schema',
      },
      'tolerance_unit': {
        '$id': '#/items/properties/tolerance_unit',
        'type': 'string',
        'title': 'The Tolerance_unit Schema',
        'enum': ['ppm', 'amu', 'mmu', 'da']
      },
      'mz_start': {
        '$id': '#/items/properties/mz_start',
        'type': 'number',
        'minimum': 0, 
        'title': 'The Mz_start Schema',
        
      },
      'mz_end': {
        '$id': '#/items/properties/mz_end',
        'type': 'number',
        'minimum': 0, 
        'title': 'The Mz_end Schema',
        
      },
      'rt_start': {
        '$id': '#/items/properties/rt_start',
        'type': 'number',
        'minimum': 0, 
        'title': 'The Rt_start Schema',
        
      },
      'rt_end': {
        '$id': '#/items/properties/rt_end',
        'type': 'number',
        'minimum': 0,
        'title': 'The Rt_end Schema',        
      },
      'sequence': {
        '$id': '#/items/properties/sequence',
        'type': 'string',
        'title': 'The Sequence Schema',
      }
    }
  }
}";

// the culture validator will be used to validate the array items


            Task<NJsonSchema.JsonSchema> schemaFromString = NJsonSchema.JsonSchema.FromJsonAsync(jsonschema);
            var jsonSchemaResult = schemaFromString.Result;
            var errorss = jsonSchemaResult.Validate(json);

            Console.WriteLine("dddd");
        }
    }
}