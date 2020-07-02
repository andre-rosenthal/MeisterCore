using System;
using System.Net.Http.Headers;
using static MeisterCore.Support.MeisterSupport;
using MeisterCore.Support;
using System.Resources;
using System.Globalization;

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
        public Resource(Uri uri, AuthenticationHeaderValue cretendials, string sapClient = null, Languages language = Languages.CultureBased ) : base()
        {
            resource = new Resource<dynamic, dynamic>(uri, cretendials,sapClient,language);
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
        public Resource(Uri uri, AuthenticationHeaderValue cretendials, string sapClient = null, MeisterExtensions extensions = MeisterExtensions.RemoveNullsAndEmptyArrays, MeisterOptions options = MeisterOptions.None, AuthenticationModes authentication = AuthenticationModes.Basic, RuntimeOptions runtimeOptions = RuntimeOptions.ExecuteAsync, Languages language = Languages.CultureBased ) : base()
        {
            resource = new Resource<dynamic, dynamic>(uri, cretendials,sapClient, extensions, options, authentication, runtimeOptions, language);
        }
        /// <summary>
        /// Authenticator
        /// </summary>
        /// <returns></returns>
        public MeisterStatus Authenticate()
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
        private ResourceManager resourceManager { get; set; }
        public MeisterStatus MeisterStatus { get; set; }
        /// <summary>
        /// Base ctor ...
        /// </summary>
        ///
        internal Resource()
        {
            resourceManager = new ResourceManager("RootResource", typeof(Resource).Assembly);
        }
        /// <summary>
        /// Simple ctor - defaults to OData v2, async run mode, and basic authentication
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cretendials"></param>
        public Resource(Uri uri, AuthenticationHeaderValue cretendials, string sapClient = null, Languages language = Languages.CultureBased ): base()
        {
            if (uri == null)
                throw new MeisterException(resourceManager.GetString("GatewayNotSet",CultureInfo.InvariantCulture));
            Meister = Meister.Instance;
            Parm = new Parameters();
            Authentications = AuthenticationModes.Basic;
            Credentials = cretendials;
            RuntimeOption = RuntimeOptions.ExecuteAsync;
            SAPClientNo = sapClient;
            SapLanguage = language;
            Meister.Configure(uri, Protocols.ODataV2, sapClient,language);
            MeisterStatus = new MeisterStatus();
        }
        /// <summary>
        /// Full Constructor
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="options"></param>
        /// <param name="authentication"></param>
        /// <param name="cretendials"></param>
        public Resource(Uri uri, AuthenticationHeaderValue cretendials, string sapClient = null, MeisterExtensions extensions = MeisterExtensions.RemoveNullsAndEmptyArrays, MeisterOptions options = MeisterOptions.None, AuthenticationModes authentication = AuthenticationModes.Basic, RuntimeOptions runtimeOptions = RuntimeOptions.ExecuteAsync, Languages language = Languages.CultureBased) : base()
        {
            if (uri == null)
                throw new MeisterException(resourceManager.GetString("GatewayNotSet", CultureInfo.InvariantCulture));
            Meister = Meister.Instance;
            Parm = new Parameters();
            Authentications = authentication;
            Credentials = cretendials;
            RuntimeOption = runtimeOptions;
            Extensions = extensions;
            Meister.SetExtensions(Extensions);
            Options = options;
            SAPClientNo = sapClient;
            SapLanguage = language;
            if (options.HasFlag(MeisterOptions.UseODataV4))
                Meister.Configure(uri, Protocols.ODataV4, sapClient,language);
            else
                Meister.Configure(uri, Protocols.ODataV2, sapClient,language);
            MeisterStatus = new MeisterStatus();
        }
        /// <summary>
        /// Authenticate the specified userid and password if using basic authentication
        /// </summary>
        public MeisterStatus Authenticate()
        {
            if (Credentials != null)
            {
                MeisterStatus = Meister.Authenticate<REQ, RES>(Authentications, Credentials);
                return MeisterStatus;
            }
            else
                throw new MeisterException(resourceManager.GetString("MissingAuthenticationHeader", CultureInfo.InvariantCulture));
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
            if (req == null)
                throw new MeisterException(resourceManager.GetString("RequestObjectNull", CultureInfo.InvariantCulture));
            if (Meister.IsAutheticated)
            {
                try
                {
                    ConstructParm(callback);
                    dynamic response = Meister.Execute<REQ, RES>(endpoint, req, Parm, RuntimeOption, Options);
                    MeisterStatus = Meister.MeisterStatus;
                    return response;
                }
                catch (MeisterException mex)
                {
                    MeisterStatus.StatusCode = mex.httpStatusCode;
                    MeisterStatus.LogEntry = mex.Message;
                    throw;
                }
                catch (Exception ex)
                {
                    MeisterStatus.StatusCode = System.Net.HttpStatusCode.NotImplemented;
                    MeisterStatus.LogEntry = ex.Message;
                    throw;
                }
            }
            else
                throw new MeisterException(resourceManager.GetString("ExecuteAfterAuthenticate", CultureInfo.InvariantCulture));
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
                            Parm.Testrun = AbapTrue;
                            break;
                        case MeisterOptions.AsyncMode:
                            if (Parm.Queued != AbapTrue)
                                Parm.Async = AbapTrue;
                            break;
                        case MeisterOptions.QuequeMode:
                            if (Parm.Async != AbapTrue)
                                Parm.Queued = AbapTrue;
                            break;
                        case MeisterOptions.UseCallback:
                            if (Parm.Async == AbapTrue && callback != null)
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