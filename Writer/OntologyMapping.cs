using System.Collections.Generic;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParser.Writer
{
    public static class OntologyMapping
    {
        /// <summary>
        /// Thermo mass analyzer type to CV mapping
        /// </summary>
        public static readonly Dictionary<MassAnalyzerType, CVParamType> MassAnalyzerTypes =
            new Dictionary<MassAnalyzerType, CVParamType>
            {
                {
                    MassAnalyzerType.MassAnalyzerFTMS, new CVParamType
                    {
                        accession = "MS:1000079",
                        name = "fourier transform ion cyclotron resonance mass spectrometer",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    MassAnalyzerType.MassAnalyzerITMS, new CVParamType
                    {
                        accession = "MS:1000264",
                        name = "ion trap",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    MassAnalyzerType.MassAnalyzerSector, new CVParamType
                    {
                        accession = "MS:1000080",
                        name = "magnetic sector",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    MassAnalyzerType.MassAnalyzerTOFMS, new CVParamType
                    {
                        accession = "MS:1000084",
                        name = "time-of-flight",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    MassAnalyzerType.MassAnalyzerTQMS, new CVParamType
                    {
                        accession = "MS:1000081",
                        name = "quadrupole",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    MassAnalyzerType.MassAnalyzerSQMS, new CVParamType
                    {
                        accession = "MS:1000081",
                        name = "quadrupole",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    MassAnalyzerType.Any, new CVParamType
                    {
                        accession = "MS:1000443",
                        name = "mass analyzer type",
                        cvRef = "MS",
                        value = ""
                    }
                }
            };

        /// <summary>
        /// Thermo ionization mode to CV mapping
        /// </summary>
        public static readonly Dictionary<IonizationModeType, CVParamType> IonizationTypes =
            new Dictionary<IonizationModeType, CVParamType>
            {
                {
                    IonizationModeType.ElectroSpray, new CVParamType
                    {
                        accession = "MS:1000073",
                        name = "electrospray ionization",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    IonizationModeType.GlowDischarge, new CVParamType
                    {
                        accession = "MS:1000259",
                        name = "glow discharge ionization",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    IonizationModeType.ChemicalIonization, new CVParamType
                    {
                        accession = "MS:1000071",
                        name = "chemical ionization",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    IonizationModeType.AtmosphericPressureChemicalIonization, new CVParamType
                    {
                        accession = "MS:1000070",
                        name = "atmospheric pressure chemical ionization",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    IonizationModeType.MatrixAssistedLaserDesorptionIonization, new CVParamType
                    {
                        accession = "MS:1000239",
                        name = "atmospheric pressure matrix-assisted laser desorption ionization",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    IonizationModeType.NanoSpray, new CVParamType
                    {
                        accession = "MS:1000398",
                        name = "nanoelectrospray",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    IonizationModeType.Any, new CVParamType
                    {
                        accession = "MS:1000008",
                        name = "ionization type",
                        cvRef = "MS",
                        value = ""
                    }
                }
            };

        /// <summary>
        /// Thermo activation type to CV mapping
        /// </summary>
        public static readonly Dictionary<ActivationType, CVParamType> DissociationTypes =
            new Dictionary<ActivationType, CVParamType>
            {
                {
                    ActivationType.CollisionInducedDissociation, new CVParamType
                    {
                        accession = "MS:1000133",
                        name = "collision-induced dissociation",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    ActivationType.HigherEnergyCollisionalDissociation, new CVParamType
                    {
                        accession = "MS:1000422",
                        name = "beam-type collision-induced dissociation",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    ActivationType.ElectronTransferDissociation, new CVParamType
                    {
                        accession = "MS:1000598",
                        name = "electron transfer dissociation",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    ActivationType.MultiPhotonDissociation, new CVParamType
                    {
                        accession = "MS:1000435",
                        name = "photodissociation",
                        cvRef = "MS",
                        value = ""
                    }
                }
            };

        /// <summary>
        /// Thermo instrument model string to CV mapping
        /// </summary>
        public static readonly Dictionary<string, CVParamType> InstrumentModels =
            new Dictionary<string, CVParamType>
            {
                {
                    "LTQ FT", new CVParamType
                    {
                        accession = "MS:1000448",
                        name = "LTQ FT",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ Orbitrap Velos", new CVParamType
                    {
                        accession = "MS:1001742",
                        name = "LTQ Orbitrap Velos",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ Orbitrap", new CVParamType
                    {
                        accession = "MS:1000449",
                        name = "LTQ Orbitrap",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ Velos", new CVParamType
                    {
                        accession = "MS:1000855",
                        name = "LTQ Velos",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "Orbitrap Fusion", new CVParamType
                    {
                        accession = "MS:1002416",
                        name = "Orbitrap Fusion",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "Q Exactive", new CVParamType
                    {
                        accession = "MS:1001911",
                        name = "Q Exactive",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "Q Exactive Orbitrap", new CVParamType
                    {
                        accession = "MS:1001911",
                        name = "Q Exactive",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "Q Exactive Plus Orbitrap", new CVParamType
                    {
                        accession = "MS:1001911",
                        name = "Q Exactive",
                        cvRef = "MS",
                        value = ""
                    }
                }
            };

        /// <summary>
        /// Thermo instrument CV accession string to CV mapping
        /// </summary>
        public static readonly Dictionary<string, List<CVParamType>> InstrumentToDetectors =
            new Dictionary<string, List<CVParamType>>
            {
                {
                    "MS:1000448", new List<CVParamType>
                    {
                        new CVParamType
                        {
                            accession = "MS:1000624",
                            name = "inductive detector",
                            cvRef = "MS",
                            value = ""
                        },
                        new CVParamType
                        {
                            accession = "MS:1000253",
                            name = "electron multiplier",
                            cvRef = "MS",
                            value = ""
                        }
                    }
                },
                {
                    "MS:1001742", new List<CVParamType>
                    {
                        new CVParamType
                        {
                            accession = "MS:1000624",
                            name = "inductive detector",
                            cvRef = "MS",
                            value = ""
                        },
                        new CVParamType
                        {
                            accession = "MS:1000253",
                            name = "electron multiplier",
                            cvRef = "MS",
                            value = ""
                        }
                    }
                },
                {
                    "MS:1000449", new List<CVParamType>
                    {
                        new CVParamType
                        {
                            accession = "MS:1000624",
                            name = "inductive detector",
                            cvRef = "MS",
                            value = ""
                        },
                        new CVParamType
                        {
                            accession = "MS:1000253",
                            name = "electron multiplier",
                            cvRef = "MS",
                            value = ""
                        }
                    }
                },
                {
                    "MS:1000855", new List<CVParamType>
                    {
                        new CVParamType
                        {
                            accession = "MS:1000253",
                            name = "electron multiplier",
                            cvRef = "MS",
                            value = ""
                        }
                    }
                },
                {
                    "MS:1002416", new List<CVParamType>
                    {
                        new CVParamType
                        {
                            accession = "MS:1000624",
                            name = "inductive detector",
                            cvRef = "MS",
                            value = ""
                        },
                        new CVParamType
                        {
                            accession = "MS:1000253",
                            name = "electron multiplier",
                            cvRef = "MS",
                            value = ""
                        }
                    }
                },
                {
                    "MS:1001911", new List<CVParamType>
                    {
                        new CVParamType
                        {
                            accession = "MS:1000624",
                            name = "inductive detector",
                            cvRef = "MS",
                            value = ""
                        }
                    }
                },
                {
                    "MS:1000483", new List<CVParamType>
                    {
                        new CVParamType
                        {
                            accession = "MS:1000026",
                            name = "inductive detector",
                            cvRef = "MS",
                            value = ""
                        }
                    }
                }
            };
    }
}