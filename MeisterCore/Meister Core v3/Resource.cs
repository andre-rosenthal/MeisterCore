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
using MeisterCore.Support;

namespace MeisterCore
{
    /// <summary>
    /// generic dynamic implementation 
    /// </summary>
    public partial class Resource
    {
        private Resource<dynamic, dynamic> resource { get; set; }
        /// <summary>
        /// empty ctor
        /// </summary>
        internal Resource()
        {
            resource = new Resource<dynamic, dynamic>();
        }
        /// <summary>
        /// ctor with Uri and Credentials
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cretendials"></param>
        public Resource(Uri uri, AuthenticationHeaderValue cretendials, string sap_client = null, Languages language = Languages.EN ) : base()
        {
            resource = new Resource<dynamic, dynamic>(uri, cretendials,sap_client,language);
        }
        /// <summary>
        /// Full ctor
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cretendials"></param>
        /// <param name="extensions"></param>
        /// <param name="options"></param>
        /// <param name="authentication"></param>
        /// <param name="runtimeOptions"></param>
        public Resource(Uri uri, AuthenticationHeaderValue cretendials, string sap_client = null, MeisterExtensions extensions = MeisterExtensions.RemoveNullsAndEmptyArrays, MeisterOptions options = MeisterOptions.None, AuthenticationModes authentication = AuthenticationModes.Basic, RuntimeOptions runtimeOptions = RuntimeOptions.ExecuteAsync, Languages language = Languages.EN ) : base()
        {
            resource = new Resource<dynamic, dynamic>(uri, cretendials,sap_client, extensions, options, authentication, runtimeOptions, language);
        }
        /// <summary>
        /// Authenticator
        /// </summary>
        /// <returns></returns>
        public bool Authenticate()
        {
            return resource.Authenticate();
        }
        /// <summary>
        /// Execute in pure dynamic
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="req"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public dynamic Execute(string endpoint, dynamic req, Uri callback = null)
        {
            return resource.Execute(endpoint, req, callback);
        }
        /// <summary>
        /// gets the raw json
        /// </summary>
        /// <returns></returns>
        public string GetRawJson()
        {
            return Meister.Instance.RawJsonResponse;
        }
    }
    public partial class Resource<REQ,RES>
    {
        private AuthenticationHeaderValue Credentials { get; set; }
        private Parameters Parm = null;
        private static Meister Meister;
        private MeisterOptions Options { get; set; }
        private MeisterExtensions Extensions { get; set; }
        private RuntimeOptions RuntimeOption { get; set; }
        private AuthenticationModes Authentications { get; set; }
        private Languages SapLanguage { get; set; }
        private string SAPClientNo { get; set; }
        /// <summary>
        /// Base ctor ...
        /// </summary>
        ///
        internal Resource()
        {
        }
        /// <summary>
        /// Simple ctor - defaults to OData v2, async run mode, and basic authentication
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cretendials"></param>
        public Resource(Uri uri, AuthenticationHeaderValue cretendials, string sap_client = null, Languages language = Languages.EN ): base()
        {
            Meister = Meister.Instance;
            Parm = new Parameters();
            Authentications = AuthenticationModes.Basic;
            Credentials = cretendials;
            RuntimeOption = RuntimeOptions.ExecuteAsync;
            SAPClientNo = sap_client;
            SapLanguage = language;
            Meister.Configure(uri, Protocols.ODataV2, sap_client,language);
        }
        /// <summary>
        /// Full Constructor
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="options"></param>
        /// <param name="authentication"></param>
        /// <param name="cretendials"></param>
        public Resource(Uri uri, AuthenticationHeaderValue cretendials, string sap_client = null, MeisterExtensions extensions = MeisterExtensions.RemoveNullsAndEmptyArrays, MeisterOptions options = MeisterOptions.None, AuthenticationModes authentication = AuthenticationModes.Basic, RuntimeOptions runtimeOptions = RuntimeOptions.ExecuteAsync, Languages language = Languages.EN ) : base()
        {
            Meister = Meister.Instance;
            Parm = new Parameters();
            Authentications = authentication;
            Credentials = cretendials;
            RuntimeOption = runtimeOptions;
            Extensions = extensions;
            Meister.SetExtensions(Extensions);
            Options = options;
            SAPClientNo = sap_client;
            SapLanguage = language;
            if (options.HasFlag(MeisterOptions.UseODataV4))
                Meister.Configure(uri, Protocols.ODataV4, sap_client,language);
            else
                Meister.Configure(uri, Protocols.ODataV2, sap_client,language);
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
        /// gets the raw json
        /// </summary>
        /// <returns></returns>
        public string GetRawJson()
        {
            return Meister.Instance.RawJsonResponse;
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
                try
                {
                    ConstructParm(callback);
                    return Meister.Execute<REQ, RES>(endpoint, req, Parm, RuntimeOption, Options);
                }
                catch (MeisterException)
                {
                    throw;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
                throw new MeisterException("First call the Authentication process, then call Execute.");            
        }
        /// <summary>
        /// Set the parameters for Meister runtime
        /// </summary>
        /// <param name="callback"></param>
        private void ConstructParm(Uri callback)
        {
            foreach (MeisterOptions value in Enum.GetValues(typeof(MeisterOptions)))
               if (Options.HasFlag(value))
                    switch (value)
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