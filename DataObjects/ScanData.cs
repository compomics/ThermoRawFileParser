using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParser.DataObjects
{
    public class ScanData
    {
        public double RetentionTime { get; set; }

        public double LowerLimit { get; set; }

        public double HigherLimit { get; set; }

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

            //scan start time
            var scanTypeCvParams = new List<CVParamType>
            {
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

            var scanType = new ScanType
            {
                cvParam = scanTypeCvParams.ToArray()
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
            scanWindow.cvParam[0] = new CVParamType
            {
                name = "scan window lower limit",
                accession = "MS:1000501",
                value = LowerLimit.ToString(),
                cvRef = "MS",
                unitAccession = "UO:0000018",
                unitCvRef = "UO",
                unitName = "nanometer"
            };
            scanWindow.cvParam[1] = new CVParamType
            {
                name = "scan window upper limit",
                accession = "MS:1000500",
                value = HigherLimit.ToString(),
                cvRef = "MS",
                unitAccession = "UO:0000018",
                unitCvRef = "UO",
                unitName = "nanometer"
            };

            scanType.scanWindowList.scanWindow[0] = scanWindow;

            scanList.scan[0] = scanType;

            return scanList;
        }
    }
}
