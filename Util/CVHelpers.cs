using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParser.Util
{
    public static class CVHelpers
    {
        public static CVParamType Copy (this CVParamType old)
        {
            return new CVParamType
            {
                accession = old.accession,
                name = old.name,
                cvRef = old.cvRef,
                unitAccession = old.unitAccession,
                unitCvRef = old.unitCvRef,
                unitName = old.unitName,
                value = old.value
            };
        }
    }
}
