using System;
using System.Net;
namespace MeisterCore
{
    [Serializable()]
    public class MeisterException : Exception
    {
        internal HttpStatusCode statusCode;
        public MeisterException() : base() { }
        public MeisterException(string message) : base(message) { }
        public MeisterException(string message, HttpStatusCode status) : base(message) { statusCode = status; }
        public MeisterException(string message, HttpStatusCode status, System.Exception inner) : base(message, inner) { statusCode = status; }

        protected MeisterException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public HttpStatusCode httpStatusCode { get { return statusCode;  } }
    }
}

