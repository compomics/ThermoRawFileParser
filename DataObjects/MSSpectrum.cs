using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParser.DataObjects
{
    class MSSpectrum : Spectrum
    {
            public double[] Masses
            {
                get
                {
                    return x;
                }
                set
                {
                    x = value;
                }
            }

            public double[] Intensities
            {
                get
                {
                    return y;
                }
                set
                {
                    y = value;
                }
            }

            public MSSpectrum()
            {
                dataTermX = new CVParamType
                {
                    accession = "MS:1000514",
                    name = "m/z array",
                    cvRef = "MS",
                    unitName = "m/z",
                    value = "",
                    unitCvRef = "MS",
                    unitAccession = "MS:1000040"
                };

                dataTermY = new CVParamType
                {
                    accession = "MS:1000515",
                    name = "intensity array",
                    cvRef = "MS",
                    unitCvRef = "MS",
                    unitAccession = "MS:1000131",
                    unitName = "number of counts",
                    value = ""
                };
            }
    }
}
