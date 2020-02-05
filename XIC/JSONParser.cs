using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NJsonSchema.Validation;
using ThermoFisher.CommonCore.Data;
using ThermoRawFileParser.Util;

namespace ThermoRawFileParser.XIC
{
    public static class JSONParser
    {
        private const string Schema = @"{
    'type': 'array',
    'title': 'The XIC input Schema',
    'items': {
     '$id': '#/items',
     'type': 'object',
     'title': 'The XIC items Schema',
     'oneOf' : [
                {'required' : ['mz', 'tolerance']},
                {'required' : ['mz_start', 'mz_end']},
                {'required' : ['sequence', 'tolerance']}
                ],
     'not' : {
        'anyOf' : [
            {'required' : ['mz','mz_start']},
            {'required' : ['mz','mz_end']},
            {'required' : ['mz','sequence']}, 
            {'required' : ['mz_start','sequence']},
            {'required' : ['mz_end','sequence']},
            {'required' : ['mz_start','tolerance']},            
            {'required' : ['mz_end','tolerance']},            
        ]
     },             
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

        public static ICollection<ValidationError> ValidateJson(string jsonString)
        {
            Task<NJsonSchema.JsonSchema> schemaFromString = NJsonSchema.JsonSchema.FromJsonAsync(Schema);
            var jsonSchemaResult = schemaFromString.Result;
            return jsonSchemaResult.Validate(jsonString);
        }

        public static XicData ParseJSON(string jsonString)
        {
            List<JSONInputUnit> jsonIn;
            XicData data = new XicData();
            jsonIn = JsonConvert.DeserializeObject<List<JSONInputUnit>>(jsonString);
            foreach (JSONInputUnit xic in jsonIn)
            {
                XicUnit xicUnit = null;
                if (xic.HasSequence())
                {
                    Peptide p = new Peptide(xic.Sequence);
                    xic.Mz = p.GetMz(xic.Charge);
                }

                if (xic.HasMz())
                {
                    double delta;
                    switch (xic.ToleranceUnit.ToLower())
                    {
                        case "ppm":
                            delta = xic.Mz.Value * xic.Tolerance.Value * 1e-6;
                            break;
                        case "amu":
                            delta = xic.Tolerance.Value;
                            break;
                        case "mmu":
                            delta = xic.Tolerance.Value * 1e-3;
                            break;
                        case "da":
                            delta = xic.Tolerance.Value;
                            break;
                        case "":
                            delta = xic.Mz.Value * xic.Tolerance.Value * 1e-6;
                            break;
                        default:
                            delta = 10;
                            break;
                    }

                    xicUnit = new XicUnit(xic.Mz.Value - delta, xic.Mz.Value + delta, xic.RtStart,
                        xic.RtEnd);
                }
                else if (xic.HasMzRange())
                {
                    xicUnit = new XicUnit(xic.MzStart.Value, xic.MzEnd.Value, xic.RtStart, xic.RtEnd);
                }

                if (xicUnit == null || !xicUnit.HasValidRanges())
                {
                    throw new RawFileParserException(
                        $"Invalid M/Z and/or retention time range:\n{JsonConvert.SerializeObject(xic, Formatting.Indented)}");
                }

                data.Content.Add(xicUnit);
            }

            return data;
        }
    }
}