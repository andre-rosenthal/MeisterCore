using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MeisterCore
{
    [Serializable()]
    public class MeisterException : Exception
    {
        internal HttpStatusCode statusCode;
        public MeisterException() : base() { }
        public MeisterException(string message) : base(message) { }
        public MeisterException(string message, HttpStatusCode status) : base(message) { statusCode = status; }
        public MeisterException(string message, System.Exception inner) : base(message, inner) { }

        protected MeisterException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public HttpStatusCode httpStatusCode { get { return statusCode;  } }
    }
}

