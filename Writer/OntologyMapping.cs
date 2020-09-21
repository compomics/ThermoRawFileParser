using System.Collections.Generic;
using System.Linq;
using ThermoFisher.CommonCore.Data;
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
        private static readonly Dictionary<string, CVParamType> InstrumentModels =
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
                    "LTQ FT ULTRA", new CVParamType
                    {
                        accession = "MS:1000557",
                        name = "LTQ FT Ultra",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ ORBITRAP", new CVParamType
                    {
                        accession = "MS:1000449",
                        name = "LTQ Orbitrap",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ ORBITRAP CLASSIC", new CVParamType
                    {
                        accession = "MS:1002835",
                        name = "LTQ Orbitrap Classic",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ ORBITRAP DISCOVERY", new CVParamType
                    {
                        accession = "MS:1000555",
                        name = "LTQ Orbitrap Discovery",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ ORBITRAP XL", new CVParamType
                    {
                        accession = "MS:1000556",
                        name = "LTQ Orbitrap XL",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ ORBITRAP XL ETD", new CVParamType
                    {
                        accession = "MS:1000639",
                        name = "LTQ Orbitrap XL ETD",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "MALDI LTQ ORBITRAP", new CVParamType
                    {
                        accession = "MS:1000643",
                        name = "MALDI LTQ Orbitrap",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ ORBITRAP VELOS", new CVParamType
                    {
                        accession = "MS:1001742",
                        name = "LTQ Orbitrap Velos",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "ORBITRAP VELOS", new CVParamType
                    {
                        accession = "MS:1001742",
                        name = "LTQ Orbitrap Velos",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ ORBITRAP VELOS PRO", new CVParamType
                    {
                        accession = "MS:1003096",
                        name = "LTQ Orbitrap Velos Pro",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "ORBITRAP VELOS PRO", new CVParamType
                    {
                        accession = "MS:1003096",
                        name = "LTQ Orbitrap Velos Pro",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ ORBITRAP ELITE", new CVParamType
                    {
                        accession = "MS:1001910",
                        name = "LTQ Orbitrap Elite",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "ORBITRAP ELITE", new CVParamType
                    {
                        accession = "MS:1001910",
                        name = "LTQ Orbitrap Elite",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ", new CVParamType
                    {
                        accession = "MS:1000447",
                        name = "LTQ",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LXQ", new CVParamType
                    {
                        accession = "MS:1000450",
                        name = "LXQ",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ XL", new CVParamType
                    {
                        accession = "MS:1000854",
                        name = "LTQ XL",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ XL ETD", new CVParamType
                    {
                        accession = "MS:1000638",
                        name = "LTQ XL ETD",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "MALDI LTQ XL", new CVParamType
                    {
                        accession = "MS:1000642",
                        name = "MALDI LTQ XL",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ VELOS", new CVParamType
                    {
                        accession = "MS:1000855",
                        name = "LTQ Velos",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "LTQ VELOS ETD", new CVParamType
                    {
                        accession = "MS:1000856",
                        name = "LTQ Velos ETD",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "ORBITRAP FUSION", new CVParamType
                    {
                        accession = "MS:1002416",
                        name = "Orbitrap Fusion",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "ORBITRAP FUSION ETD", new CVParamType
                    {
                        accession = "MS:1002417",
                        name = "Orbitrap Fusion ETD",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "ORBITRAP FUSION LUMOS", new CVParamType
                    {
                        accession = "MS:1002732",
                        name = "Orbitrap Fusion Lumos",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "ORBITRAP ECLIPSE", new CVParamType
                    {
                        accession = "MS:1003029",
                        name = "Orbitrap Eclipse",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "ORBITRAP EXPLORIS 120", new CVParamType
                    {
                        accession = "MS:1003095",
                        name = "Orbitrap Exploris 120",
                        cvRef = "MS",
                        value = ""
                    }
                },
                                {
                    "ORBITRAP EXPLORIS 240", new CVParamType
                    {
                        accession = "MS:1003094",
                        name = "Orbitrap Exploris 240",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "ORBITRAP EXPLORIS 480", new CVParamType
                    {
                        accession = "MS:1003028",
                        name = "Orbitrap Exploris 480",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "EXACTIVE", new CVParamType
                    {
                        accession = "MS:1000649",
                        name = "Exactive",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "EXACTIVE PLUS", new CVParamType
                    {
                        accession = "MS:1002526",
                        name = "Exactive Plus",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "Q EXACTIVE", new CVParamType
                    {
                        accession = "MS:1001911",
                        name = "Q Exactive",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "Q EXACTIVE ORBITRAP", new CVParamType
                    {
                        accession = "MS:1001911",
                        name = "Q Exactive",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "Q EXACTIVE PLUS ORBITRAP", new CVParamType
                    {
                        accession = "MS:1002634",
                        name = "Q Exactive Plus",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "Q EXACTIVE HF", new CVParamType
                    {
                        accession = "MS:1002523",
                        name = "Q Exactive HF",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "Q EXACTIVE HF-X", new CVParamType
                    {
                        accession = "MS:1002877",
                        name = "Q Exactive HF-X",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "Q EXACTIVE PLUS", new CVParamType
                    {
                        accession = "MS:1002634",
                        name = "Q Exactive Plus",
                        cvRef = "MS",
                        value = ""
                    }
                },
                {
                    "ORBITRAP ID-X", new CVParamType
                    {
                        accession = "MS:1003112",
                        name = "Orbitrap ID-X",
                        cvRef = "MS",
                        value = ""
                    }
                }
            };

        /// <summary>
        /// Get the instrument model CV param for the given instrument name
        /// </summary>
        /// <param name="instrumentName">the instrument name</param>
        /// <returns>the instrument CV param</returns>
        public static CVParamType getInstrumentModel(string instrumentName)
        {
            CVParamType instrumentModel;
            instrumentName = instrumentName.ToUpper();
            if (OntologyMapping.InstrumentModels.ContainsKey(instrumentName))
            {
                instrumentModel = OntologyMapping.InstrumentModels[instrumentName];
            }
            else
            {
                var longestMatch = InstrumentModels.Where(pair => instrumentName.Contains(pair.Key))
                    .Select(pair => pair.Key)
                    .Aggregate("", (max, current) => max.Length > current.Length ? max : current);
                if (!longestMatch.IsNullOrEmpty())
                {
                    instrumentModel = OntologyMapping.InstrumentModels[longestMatch];
                }
                else
                {
                    instrumentModel = new CVParamType
                    {
                        accession = "MS:1000483",
                        name = "Thermo Fisher Scientific instrument model",
                        cvRef = "MS",
                        value = ""
                    };
                }
            }

            return instrumentModel;
        }

        /// <summary>
        /// Get a list of detectors for the given instrument
        /// </summary>
        /// <param name="instrumentAccession">the instrument accession</param>
        /// <returns>a list of detectors</returns>
        public static List<CVParamType> GetDetectors(string instrumentAccession)
        {
            List<CVParamType> detectors;
            switch (instrumentAccession)
            {
                // LTQ FT
                case "MS:1000448":
                // LTQ FT ULTRA    
                case "MS:1000557":
                // LTQ ORBITRAP 
                case "MS:1000449":
                // LTQ ORBITRAP CLASSIC
                case "MS:1002835":
                // LTQ ORBITRAP DISCOVERY   
                case "MS:1000555":
                // LTQ ORBITRAP XL   
                case "MS:1000556":
                // LTQ ORBITRAP XL ETD   
                case "MS:1000639":
                // MALDI LTQ ORBITRAP   
                case "MS:1000643":
                // LTQ ORBITRAP VELOS    
                case "MS:1001742":
                // LTQ ORBITRAP VELOS PRO
                case "MS:1003096":
                // LTQ ORBITRAP ELITE    
                case "MS:1001910":
                // ORBITRAP FUSION
                case "MS:1002416":
                // ORBITRAP FUSION ETD    
                case "MS:1002417":
                // ORBITRAP FUSION LUMOS
                case "MS:1002732":
                // ORBITRAP ECLIPSE    
                case "MS:1003029":
                // ORBITRAP ID-X
                case "MS:1003112":
                    detectors = new List<CVParamType>
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
                    };
                    break;
                // EXACTIVE
                case "MS:1000649":
                // EXACTIVE PLUS
                case "MS:1002526":
                // Q EXACTIVE
                case "MS:1001911":
                // Q EXACTIVE HF    
                case "MS:1002523":
                // Q EXACTIVE HF-X    
                case "MS:1002877":
                // Q EXACTIVE PLUS    
                case "MS:1002634":
                // ORBITRAP EXPLORIS 120
                case "MS:1003095":
                // ORBITRAP EXPLORIS 240
                case "MS:1003094":
                // ORBITRAP EXPLORIS 480
                case "MS:1003028":
                    detectors = new List<CVParamType>
                    {
                        new CVParamType
                        {
                            accession = "MS:1000624",
                            name = "inductive detector",
                            cvRef = "MS",
                            value = ""
                        }
                    };
                    break;
                // LTQ
                case "MS:1000447":
                // LTQ VELOS
                case "MS:1000855":
                // LTQ VELOS ETD
                case "MS:1000856":
                // LXQ    
                case "MS:1000450":
                // LTQ XL
                case "MS:1000854":
                // LTQ XL ETD
                case "MS:1000638":
                // MALDI LTQ XL    
                case "MS:1000642":
                    detectors = new List<CVParamType>
                    {
                        new CVParamType
                        {
                            accession = "MS:1000253",
                            name = "electron multiplier",
                            cvRef = "MS",
                            value = ""
                        }
                    };
                    break;
                default:
                    detectors = new List<CVParamType>
                    {
                        new CVParamType
                        {
                            accession = "MS:1000624",
                            name = "inductive detector",
                            cvRef = "MS",
                            value = ""
                        }
                    };
                    break;
            }

            return detectors;
        }
    }
}