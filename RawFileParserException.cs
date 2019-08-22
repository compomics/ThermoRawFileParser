using System;
using System.Runtime.Serialization;

namespace ThermoRawFileParser
{
    public class RawFileParserException : Exception
    {
        public RawFileParserException()
        {
        }

        protected RawFileParserException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public RawFileParserException(string message) : base(message)
        {
        }

        public RawFileParserException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}