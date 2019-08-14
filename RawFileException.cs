using System;
using System.Runtime.Serialization;

namespace ThermoRawFileParser
{
    public class RawFileException : Exception
    {
        public RawFileException()
        {
        }

        protected RawFileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public RawFileException(string message) : base(message)
        {
        }

        public RawFileException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}