using System;

namespace PokeD.Server.Exceptions
{
    public class ServerException : Exception
    {
        public ServerException() : base() { }

        public ServerException(string message) : base(message) { }

        public ServerException(string format, params object[] args) : base(string.Format(format, args)) { }

        public ServerException(string message, Exception innerException) : base(message, innerException) { }

        public ServerException(string format, Exception innerException, params object[] args) : base(string.Format(format, args), innerException) { }
    }
}
