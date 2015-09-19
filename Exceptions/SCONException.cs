using System;

namespace PokeD.Server.Exceptions
{
    public class SCONException : Exception
    {
        public SCONException() : base() { }

        public SCONException(string message) : base(message) { }

        public SCONException(string format, params object[] args) : base(string.Format(format, args)) { }

        public SCONException(string message, Exception innerException) : base(message, innerException) { }

        public SCONException(string format, Exception innerException, params object[] args) : base(string.Format(format, args), innerException) { }
    }
}
