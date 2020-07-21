using System.Collections.Generic;
using ThermoFisher.CommonCore.Data;
using ThermoRawFileParser.Util;
using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParser.DataObjects
{
    /// <summary>
    /// Container for spectrum scan parameters, such as RT, mass limits etc
    /// </summary>
    public class ScanData
    {
        public CVParamType unit;
        
        public double RetentionTime { get; set; }

        public double LowerLimit { get; set; }

        public double HigherLimit { get; set; }

        public string Filter { get; set; }

        public double? InjectionTime { get; set; }

        public double? MonoisotopicMass { get; set; }

        /// <summary>
        /// Convert ScanData to MzML ScanList
        /// </summary>
        public ScanListType ToScanList()
        {
            var scanList = new ScanListType
            {
                count = "1",
                scan = new ScanType[1],
                cvParam = new CVParamType[1]
            };

            scanList.cvParam[0] = new CVParamType
            {
                accession = "MS:1000795",
                cvRef = "MS",
                name = "no combination",
                value = ""
            };

            //Scan CV Params
            var scanTypeCvParams = new List<CVParamType>
            {
                //scan start time
                new CVParamType
                {
                    name = "scan start time",
                    accession = "MS:1000016",
                    value = RetentionTime.ToString(),
                    unitCvRef = "UO",
                    unitAccession = "UO:0000031",
                    unitName = "minute",
                    cvRef = "MS"
                }
            };

            // Filter String
            if (!Filter.IsNullOrEmpty())
            {
                scanTypeCvParams.Add(
                    new CVParamType { cvRef = "MS", accession = "MS:1000512", name = "filter string", value = Filter });
            }

            // Injection Time
            if (InjectionTime.HasValue)
            {
                scanTypeCvParams.Add(
                    new CVParamType {
                        cvRef = "MS",
                        accession = "MS:1000927",
                        name = "ion injection time",
                        value = InjectionTime.ToString(),
                        unitCvRef = "UO",
                        unitAccession = "UO:0000028",
                        unitName = "millisecond"
                    });
            }

            var scanUserParams = new List<UserParamType>();
            // Monoisotopic Mass as userParam
            if (MonoisotopicMass.HasValue)
            {
                scanUserParams.Add(
                    new UserParamType
                    {
                        name = "[Thermo Trailer Extra]Monoisotopic M/Z:",
                        value = MonoisotopicMass.ToString(),
                        type = "xsd:float"
                    });
            }

            var scanType = new ScanType
            {
                cvParam = scanTypeCvParams.ToArray(),
                userParam = scanUserParams.ToArray()
            };

            // Scan window list
            scanType.scanWindowList = new ScanWindowListType
            {
                count = 1,
                scanWindow = new ParamGroupType[1]
            };
            var scanWindow = new ParamGroupType
            {
                cvParam = new CVParamType[2]
            };

            scanWindow.cvParam[0] = CVHelpers.Copy(unit, 
                name: "scan window lower limit",
                accession: "MS:1000501",
                value: LowerLimit.ToString(),
                cvRef: "MS");

            scanWindow.cvParam[1] = CVHelpers.Copy(unit,
                name: "scan window upper limit",
                accession: "MS:1000500",
                value: HigherLimit.ToString(),
                cvRef: "MS");

            scanType.scanWindowList.scanWindow[0] = scanWindow;

            scanList.scan[0] = scanType;

            return scanList;
        }
    }
}
