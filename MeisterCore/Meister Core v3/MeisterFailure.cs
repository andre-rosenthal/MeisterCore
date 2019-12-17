using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MeisterCore.Support
{
    public class BackendFailure
    {
        public List<Failure> failures { get; set; }
    }

    public class Failure
    {
        public string meisterFailed { get; set; }
        public int returnCode { get; set; }
        public string backendMessage { get; set; }
        public string fromEndpoint { get; set; }
    }
}