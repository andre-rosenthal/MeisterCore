using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Web;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Net.Http.Headers;
using RestSharp.Authenticators;
using System.Text;
using RestSharp;
using System.Threading.Tasks;
using static MeisterCore.Support.MeisterSupport;

namespace MeisterCore
{
    public partial class Resource<REQ,RES>
    {
        private AuthenticationHeaderValue Credentials { get; set; }
        private Parameters Parm = null;
        private static Meister Meister;
        private MeisterOptions Options { get; set; }
        private MeisterExtensions Extensions { get; set; }
        private RuntimeOptions RuntimeOption { get; set; }
        private AuthenticationModes Authentications { get; set; }
        /// <summary>
        /// Base ctor ...
        /// </summary>
        internal Resource()
        {
        }
        /// <summary>
        /// Simple ctor - defaults to OData v2, async run mode, and basic authentication
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cretendials"></param>
        public Resource(Uri uri, AuthenticationHeaderValue cretendials): base()
        {
            Meister = Meister.Instance;
            Parm = new Parameters();
            Authentications = AuthenticationModes.Basic;
            Credentials = cretendials;
            RuntimeOption = RuntimeOptions.ExecuteAsync;
            Meister.Configure(uri, Protocols.ODataV2);
        }
        /// <summary>
        /// Full Constructor
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="options"></param>
        /// <param name="authentication"></param>
        /// <param name="cretendials"></param>
        public Resource(Uri uri, AuthenticationHeaderValue cretendials, MeisterExtensions extensions = MeisterExtensions.RemoveNullsAndEmptyArrays, MeisterOptions options = MeisterOptions.None, AuthenticationModes authentication = AuthenticationModes.Basic, RuntimeOptions runtimeOptions = RuntimeOptions.ExecuteAsync) : base()
        {
            Meister = Meister.Instance;
            Parm = new Parameters();
            Authentications = authentication;
            Credentials = cretendials;
            RuntimeOption = runtimeOptions;
            Extensions = extensions;
            Meister.SetExtensions(Extensions);
            Options = options;
            if (Options == MeisterOptions.UseODataV4)
                Meister.Configure(uri, Protocols.ODataV4);
            else
                Meister.Configure(uri, Protocols.ODataV2);
        }
        /// <summary>
        /// Authenticate the specified userid and password if using basic authentication
        /// </summary>
        public bool Authenticate()
        {
            if (Credentials != null)
                return Meister.Authenticate<REQ,RES>(Authentications, Credentials);
            else
                throw new MeisterException("AuthenticationHeaderValue is not set");
        }
        /// <summary>
        /// Direct execution of call returning always as dynamic 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        public dynamic Execute(string endpoint, REQ req, Uri callback = null)
        {
            if (Meister.IsAutheticated)
            {
                ConstructParm(callback);
                if (Options == MeisterOptions.UseODataV4)
                    return ExecuteUnderODataV4Async(endpoint, req, callback);
                else
                    return ExecuteUnderODataV2(endpoint, req, callback);
            }
            else
                throw new MeisterException("First call the Authentication process, then call Execute.");            
        }
        /// <summary>
        /// Executing the call under OData v2
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        private dynamic ExecuteUnderODataV2(string endpoint, REQ req, Uri callback)
        {
            var d = Meister.ExecuteODataV2<REQ, RES>(endpoint, req, Parm, RuntimeOption);
            OD2Body<RES> od2 = d.Result as OD2Body<RES>;
            ResultOD2 result = od2.d.results[0] as ResultOD2;
            if (result == null)
                return null;           
            var res = JsonConvert.DeserializeObject<IEnumerable<RES>>(result.Json);
            if (res == null)
                return null;
            return res;
        }
        /// <summary>
        /// OData v4 runtime
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="req"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private dynamic ExecuteUnderODataV4Async(string endpoint, REQ req, Uri callback)
        {
            return Meister.ExecuteODataV4<REQ,RES>(endpoint, req, Parm, RuntimeOption);
        }
        /// <summary>
        /// Set the parameters for Meister runtime
        /// </summary>
        /// <param name="callback"></param>
        private void ConstructParm(Uri callback)
        {
            foreach (MeisterOptions value in Enum.GetValues(typeof(MeisterOptions)))
                if ((Options & value) == value)
                    switch (Options)
                    {
                        case MeisterOptions.TestRun:
                            Parm.Testrun = Abap_true;
                            break;
                        case MeisterOptions.AsyncMode:
                            if (Parm.Queued != Abap_true)
                                Parm.Async = Abap_true;
                            break;
                        case MeisterOptions.QuequeMode:
                            if (Parm.Async != Abap_true)
                                Parm.Queued = Abap_true;
                            break;
                        case MeisterOptions.UseCallback:
                            if (Parm.Async == Abap_true && callback != null)
                                Parm.Callback = callback.AbsolutePath;
                            break;
                        case MeisterOptions.None:
                            break;
                        case MeisterOptions.CompressionsInbound:
                            if (Parm.Compression == Parameters.Outbound)
                                Parm.Compression = Parameters.Both;
                            else
                                Parm.Compression = Parameters.Inbound;
                            break;
                        case MeisterOptions.CompressionsOutbound:
                            if (Parm.Compression == Parameters.Inbound)
                                Parm.Compression = Parameters.Both;
                            else
                                Parm.Compression = Parameters.Outbound;
                            break;
                    }
        }
    }
}