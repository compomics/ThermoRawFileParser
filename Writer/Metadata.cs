using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using ThermoRawFileParser.Writer.MzML;

namespace ThermoRawFileParser.Writer
{
    public class Metadata
    {
        /** The general Path properties contains: RAW path , RAW file version **/
        private List<Dictionary<string, string>> fileProperties = new List<Dictionary<string, string>>();

        /** The Instruments properties contains the information of the instrument **/ 
        private List<Dictionary<string, CVParamType>> instrumentProperties = new List<Dictionary<string, CVParamType>>();

        /** Scan Settings **/
        private List<Dictionary<String, CVParamType>> scanSettings = new List<Dictionary<string, CVParamType>>();

        /** MS and MS data including number of MS and MS/MS **/
        private List<Dictionary<String, CVParamType>> msData = new List<Dictionary<string, CVParamType>>(); 
        
        /**
         * Default constructor 
         */
        public Metadata(){}

        public Metadata(List<Dictionary<string, string>> fileProperties,
            List<Dictionary<string, CVParamType>> instrumentProperties,
            List<Dictionary<string, CVParamType>> msData)
        {
            this.fileProperties = fileProperties;
            this.instrumentProperties = instrumentProperties;
            this.msData = msData;
        }

        public List<Dictionary<string, string>> FileProperties => fileProperties;

        public List<Dictionary<string, CVParamType>> InstrumentProperties => instrumentProperties;

        public List<Dictionary<string, CVParamType>> MsData => msData;

        /**
         * Add a File property to the fileProperties 
         */
        public void addFileProperty(String key, String value)
        {
            var dic = new Dictionary<string, string>();
            dic.Add(key, value);
            fileProperties.Add(dic);
        }

        public void addInstrumentProperty(string key, CVParamType value)
        {
            var dic = new Dictionary<string, CVParamType>();
            dic.Add(key, value);
            instrumentProperties.Add(dic);
        }

        public void addScanSetting(string key, CVParamType value)
        {
            var dic = new Dictionary<string, CVParamType>();
            dic.Add(key, value);
            scanSettings.Add(dic);
        }

        public void addMSData(string key, CVParamType value)
        {
            var dic = new Dictionary<string, CVParamType>();
            dic.Add(key, value);
            msData.Add(dic);
        }
    }
}