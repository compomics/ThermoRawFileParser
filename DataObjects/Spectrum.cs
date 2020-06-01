using System;
using System.Collections.Generic;
using System.IO;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoRawFileParser.Writer.MzML;
using zlib;

namespace ThermoRawFileParser.DataObjects
{
    /// <summary>
    /// General representation of the spectrum object, i.e. a set of X - Y pairs with
    /// some extra data
    /// </summary>

    public abstract class Spectrum
    {
        private readonly CVParamType encodingTerm =
            new CVParamType { accession = "MS:1000523", name = "64-bit float", cvRef = "MS", value = "" };
        private readonly CVParamType zLibTerm =
            new CVParamType { accession = "MS:1000574", name = "zlib compression", cvRef = "MS", value = "" };

        protected IRawDataPlus rawFileRef;
        
        protected CVParamType spectrumType;

        protected int scanNumber;

        protected bool centroided;

        protected double[] x;
        protected double[] y;
        protected CVParamType dataTermX;
        protected CVParamType dataTermY;

        public double? BasePeakPosition { get; set; }
        public double? BasePeakIntensity { get; set; }

        public double? LowestPosition
        {
            get
            {
                if (!x.IsNullOrEmpty()) return x[0];
                return null;
            }
        }

        public double? HighestPosition
        {
            get
            {
                if (!x.IsNullOrEmpty()) return x[x.Length - 1];
                return null;
            }
        }

        public int dataArrayLength;
        public string spectrumId;

        public ScanData ScanInfo { get; set; }

        /// <summary>
        /// Convert the double array into a byte array
        /// </summary>
        /// <param name="array">the double collection</param>
        /// <returns>the byte array</returns>
        private static byte[] Get64BitArray(IEnumerable<double> array)
        {
            byte[] bytes;

            using (var memoryStream = new MemoryStream())
            {
                foreach (var doubleValue in array)
                {
                    var doubleValueByteArray = BitConverter.GetBytes(doubleValue);
                    memoryStream.Write(doubleValueByteArray, 0, doubleValueByteArray.Length);
                }

                memoryStream.Position = 0;
                bytes = memoryStream.ToArray();
            }

            return bytes;
        }

        /// <summary>
        /// Convert the double array into a compressed zlib byte array
        /// </summary>
        /// <param name="array">the double collection</param>
        /// <returns>the byte array</returns>
        private static byte[] GetZLib64BitArray(IEnumerable<double> array)
        {
            byte[] bytes;

            using (var memoryStream = new MemoryStream())
            using (var outZStream = new ZOutputStream(memoryStream, zlibConst.Z_DEFAULT_COMPRESSION))
            {
                foreach (var doubleValue in array)
                {
                    var doubleValueByteArray = BitConverter.GetBytes(doubleValue);
                    outZStream.Write(doubleValueByteArray, 0, doubleValueByteArray.Length);
                }

                outZStream.finish();
                memoryStream.Position = 0;
                bytes = memoryStream.ToArray();
            }

            return bytes;
        }

        /// <summary>
        /// Converts spectrum data to mzML style BinaryDataArrayList
        /// </summary>
        /// <param name="zLibCompression">use ZLib compression</param>
        /// <returns>BinaryDataArrayList element of Spectrum</returns>
        public BinaryDataArrayListType GetBinaryDataArray(bool zLibCompression)
        {
            var binaryData = new List<BinaryDataArrayType>();

            // X Data
            if (!x.IsNullOrEmpty())
            {
                var massesBinaryData =
                    new BinaryDataArrayType
                    {
                        binary = zLibCompression ? GetZLib64BitArray(x) : Get64BitArray(x)
                    };

                massesBinaryData.encodedLength =
                    (4 * Math.Ceiling((double)massesBinaryData.binary.Length / 3)).ToString();

                var massesBinaryDataCvParams = new List<CVParamType>
                {
                    dataTermX,
                    encodingTerm
                };
                if (zLibCompression)
                {
                    massesBinaryDataCvParams.Add(zLibTerm);
                }

                massesBinaryData.cvParam = massesBinaryDataCvParams.ToArray();

                binaryData.Add(massesBinaryData);
            }

            // Intensity Data
            if (!y.IsNullOrEmpty())
            {
                var intensitiesBinaryData =
                    new BinaryDataArrayType
                    {
                        binary = zLibCompression ? GetZLib64BitArray(y) : Get64BitArray(y)
                    };

                intensitiesBinaryData.encodedLength =
                    (4 * Math.Ceiling((double)intensitiesBinaryData.binary.Length / 3)).ToString();

                var intensitiesBinaryDataCvParams = new List<CVParamType>
                {
                    dataTermY,
                    encodingTerm
                };
                if (zLibCompression)
                {
                    intensitiesBinaryDataCvParams.Add(zLibTerm);
                }

                intensitiesBinaryData.cvParam = intensitiesBinaryDataCvParams.ToArray();

                binaryData.Add(intensitiesBinaryData);
            }

            return (!binaryData.IsNullOrEmpty()) ?
                new BinaryDataArrayListType
                {
                    count = binaryData.Count.ToString(),
                    binaryDataArray = binaryData.ToArray()
                }
                :
                null;
        }

        /// <summary>
        /// Create native spectrum ID
        /// </summary>
        /// <returns>Native spectrum ID of a spectrum</returns>
        protected string CreateNativeID()
        {
            return String.Format("controllerType={0} controllerNumber={1} scan={2}", 
                (int)rawFileRef.SelectedInstrument.DeviceType,
                rawFileRef.SelectedInstrument.InstrumentIndex,
                scanNumber);
        }

        public CVParamType CopyCVParamType(CVParamType old)
        {
            return new CVParamType
            {
                accession = old.accession,
                cvRef = old.cvRef,
                name = old.name,
                unitAccession = old.unitAccession,
                unitCvRef = old.unitCvRef,
                unitName = old.unitName,
                value = old.value
            };
        }

        /// <summary>
        /// Return CVTerm for spectrum representation: Profile/Centroided
        /// </summary>
        public CVParamType GetScanTypeTerm()
        {
            return centroided ?
                new CVParamType { accession = "MS:1000127", cvRef = "MS", name = "centroid spectrum", value = "" }
                :
                new CVParamType { accession = "MS:1000128", cvRef = "MS", name = "profile spectrum", value = "" };

        }
    }
}
