using System;
using System.Runtime.Serialization;

namespace MS2Lib
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [Serializable]
    public class BadMS2ArchiveException : Exception
    {
        public BadMS2ArchiveException() : this("Unexpected error in reading the MS2 archive.")
        {

        }

        public BadMS2ArchiveException(string message) : base(message)
        {

        }

        public BadMS2ArchiveException(string message, Exception innerException) : base(message, innerException)
        {

        }

        protected BadMS2ArchiveException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}