using System.Collections.Generic;
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

    public class CVComparer : IEqualityComparer<CVParamType>
    {
        public bool Equals(CVParamType cv1, CVParamType cv2)
        {
            return cv1.accession == cv2.accession;
        }

        public int GetHashCode(CVParamType cv)
        {
            return cv.accession.GetHashCode();
        }
    }
}
