using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParser.Util
{
    public static class CVHelpers
    {
        /// <summary>
        /// Nanometer unit constant
        /// </summary>
        static public readonly CVParamType nanometerUnit = new CVParamType
        {
            unitAccession = "UO:0000018",
            unitCvRef = "UO",
            unitName = "nanometer"
        };

        /// <summary>
        /// m/z unit constant
        /// </summary>
        static public readonly CVParamType massUnit = new CVParamType
        {
            unitAccession = "MS:1000040",
            unitCvRef = "MS",
            unitName = "m/z"
        };

        /// <summary>
        /// Create copy of a CVParamType object with optional substitution of fields
        /// </summary>
        /// <param name="old">CVParamType object to copy from</param>
        /// <returns></returns>
        public static CVParamType Copy(CVParamType old, string accession=null, string cvRef=null,
            string name=null, string unitAccession=null, string unitCvRef=null,
            string unitName=null, string value=null)
        {
            return new CVParamType
            {
                accession = accession ?? old.accession,
                cvRef = cvRef ?? old.cvRef,
                name = name ?? old.name,
                unitAccession = unitAccession ?? old.unitAccession,
                unitCvRef = unitCvRef ?? old.unitCvRef,
                unitName = unitName ?? old.unitName,
                value = value ?? old.value
            };
        }
    }
}
