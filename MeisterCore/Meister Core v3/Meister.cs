using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MeisterCore.Support;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using static MeisterCore.Support.MeisterSupport;
namespace MeisterCore
{
    /// <summary>
    /// Summary description for Meister Core Library Definistions - Singleton pattern
    /// Suffix pattern $format=json&sap-client=NNN&sap-language=XX
    /// </summary>
    ///
    internal partial class Meister : SingletonBase<Meister>
    {
        private const string MeisterHostOD2 = @"/sap/opu/odata/MEISTER/ENGINE/";
        private const string MeisterHostOD4 = @"/sap/opu/odata4/meister/od4grp/default/meister/od4/0001/";
        private const string ExecuteMeister = @"Execute";
        private const string TokenFetch = @"Requests";
        private const string metadata = @"$metadata";
        private const string Content = "Json";
        private const string EndPoint = "Endpoint";
        private const string Parms = "Parms";
        private const string csrf = "x-csrf-token";
        private const string Format = "format";
        private const string Sap_language = "sap-language";
        private const string Sap_client = "sap-client";
        public Protocols ODataProtocol { get; set; }
        public Uri Uri { get; set; }
        public bool IsAutheticated { get; set; }
        private string username;
        private string password;
        private string accessToken;
        private string tokenType;
        private string sap_client;
        public MeisterStatus MeisterStatus { get; set; }
        public Languages sap_language { get; private set; }
        private bool SapClientOK;
        private string CsrfToken { get; set; }
        private MeisterExtensions Extensions { get; set; }
        private BackendFailure Failure {get;set;}
        private static readonly RestClient Client = new RestClient();
        private ResourceManager resourceManager { get; set; }
        public string RawJsonResponse { get; set; }
        /// <summary>
        /// Ctor 
        /// </summary>
        internal Meister()
        {
            resourceManager = new ResourceManager("RootResource", typeof(Resource).Assembly);
        }
        /// <summary>
        /// Configuration step ..
        /// </summary>
        /// <param name="uri"></param>
        public void Configure(Uri uri, Protocols prot = Protocols.ODataV2, string sap_client = null, Languages sap_language = Languages.CultureBased) 
        {
            ODataProtocol = prot;
            this.sap_client = sap_client;
            this.sap_language = sap_language;
            SapClientOK = IsSuffixedSapClientNeeded();
            CsrfToken = string.Empty;
            if (prot == Protocols.ODataV2)
                Uri = new Uri(uri.AbsoluteUri + MeisterHostOD2);
            else
                Uri = new Uri(uri.AbsoluteUri + MeisterHostOD4);
            Client.BaseUrl = Uri;
        }
        /// <summary>
        /// Authentication
        /// </summary>
        /// <typeparam name="REQ"></typeparam>
        /// <typeparam name="RES"></typeparam>
        /// <param name="authentications"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        internal MeisterStatus Authenticate<REQ, RES>(AuthenticationModes authentications, AuthenticationHeaderValue credentials)
        {
            string authHeader = credentials.Parameter;
            if (authHeader == null)
                throw new MeisterException(resourceManager.GetString("BadAuthetication", CultureInfo.InvariantCulture));
            switch (authentications)
            {
                case AuthenticationModes.Basic:
                    return RunAsBasicAuthentication(authHeader);
                case AuthenticationModes.OAuth:
                    return RunAsOAuth2Authentication(authHeader);
                case AuthenticationModes.JWT:
                    break;
                case AuthenticationModes.SAML2:
                    break;
                default:
                    break;
            }
            return new MeisterStatus();
        }

        /// <summary>
        /// OAuth 2.0
        /// </summary>
        /// <param name="authHeader"></param>
        /// <returns></returns>
        private MeisterStatus RunAsOAuth2Authentication(string authHeader)
        {
            string encodedUsernamePassword = authHeader.Substring("Token ".Length).Trim();
            Encoding encoding = Encoding.GetEncoding("iso-8859-1");
            string accesstokentype = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));
            int seperatorIndex = accesstokentype.IndexOf(':');
            accessToken = accesstokentype.Substring(0, seperatorIndex);
            tokenType = accesstokentype.Substring(seperatorIndex + 1);
            IAuthenticator authenticator = Client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(accessToken, tokenType);
            if (authenticator != null)
            {
                var request = new RestRequest(Method.GET);
                DoResourceAllocation(request, metadata);
                request.AddHeader("Authorization", string.Format("bearer {0}", accessToken));
                request.AddHeader("Accept", "application/json");
                IRestResponse response = Client.Execute(request);
                MeisterStatus = new MeisterStatus(response, request.Resource);
                if (HttpResponseInValidRange(response.StatusCode))
                    IsAutheticated = true;
                return MeisterStatus;
            }
            else
                throw new MeisterException(resourceManager.GetString("InvalidOauth", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Basic Authenticator
        /// </summary>
        /// <param name="authHeader"></param>
        private MeisterStatus RunAsBasicAuthentication(string authHeader)
        {
            string encodedUsernamePassword = authHeader.Trim();
            Encoding encoding = Encoding.GetEncoding("iso-8859-1");
            string usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));
            int separatorIndex = usernamePassword.IndexOf(':');
            username = usernamePassword.Substring(0, separatorIndex);
            password = usernamePassword.Substring(separatorIndex + 1);
            IAuthenticator authenticator = Client.Authenticator = new HttpBasicAuthenticator(username, password);
            if (authenticator != null)
            {
                var request = new RestRequest(Method.GET);
                DoResourceAllocation(request, metadata);
                request.AddHeader(csrf, "Fetch");
                IRestResponse response = Client.Execute(request);
                MeisterStatus = new MeisterStatus(response, request.Resource);
                if (HttpResponseInValidRange(response.StatusCode))
                    IsAutheticated = true;
                return MeisterStatus;
            }
            else
                throw new MeisterException(resourceManager.GetString("BadAuthetication", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Meister execution path
        /// </summary>
        /// <typeparam name="REQ"></typeparam>
        /// <typeparam name="RES"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="req"></param>
        /// <param name="parm"></param>
        /// <param name="runtimeOption"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        internal dynamic Execute<REQ, RES>(string endpoint, REQ req, Parameters Parm, RuntimeOptions RuntimeOption, MeisterOptions options)
        {
            if (options.HasFlag(MeisterOptions.UseODataV4))
                return ExecuteODataV4<REQ, RES>(endpoint, req, Parm, RuntimeOption, options);
            else
                return ExecuteODataV2<REQ, RES>(endpoint, req, Parm, RuntimeOption, options);
        }
        /// <summary>
        /// Execute under OData v4
        /// </summary>
        /// <typeparam name="REQ"></typeparam>
        /// <typeparam name="RES"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="req"></param>
        /// <param name="parm"></param>
        /// <param name="runtimeOption"></param>
        /// <returns></returns>
        internal dynamic ExecuteODataV4<REQ, RES>(string endpoint, REQ req, Parameters parm, RuntimeOptions runtimeOption, MeisterOptions options)
        {
            OD4Body<RES> od4 = ExecuteODataV4Internal<REQ, RES>(endpoint, req, parm, runtimeOption, options);
            if (od4 == null)
                return null;
            else if (string.IsNullOrEmpty(od4.odatacontext) && runtimeOption == RuntimeOptions.ExecuteSync)
                throw new MeisterException(od4.value);
            else if (string.IsNullOrEmpty(od4.odatacontext) && string.IsNullOrEmpty(od4.value))
                throw new MeisterException(od4.value);
            else
                try
                {
                    string json = string.Empty;
                    if (options.HasFlag(MeisterOptions.CompressionsOutbound))
                        json = Unzip(od4.value);
                    else
                        json = od4.value;
                    json = RemoveDrefAnnotations(json);
                    return VanillaProcess<RES>(json);
                }
                catch (MeisterException)
                {
                    throw new MeisterException(resourceManager.GetString("UnableToMarshall", CultureInfo.InvariantCulture) + od4.value);                    
                }
        }
        /// <summary>
        /// Internal runtime for OD4
        /// </summary>
        /// <typeparam name="REQ"></typeparam>
        /// <typeparam name="RES"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="req"></param>
        /// <param name="parm"></param>
        /// <param name="runtimeOption"></param>
        /// <returns></returns>
        private OD4Body<RES> ExecuteODataV4Internal<REQ, RES>(string endpoint, REQ req, Parameters parm, RuntimeOptions runtimeOption, MeisterOptions options)
        {
            CancellationTokenSource cancel = null;
            try
            {
                RestRequest request = null;
                if (string.IsNullOrEmpty(CsrfToken))
                {
                    request = new RestRequest(Method.GET);
                    DoResourceAllocation(request, TokenFetch);
                    request.AddHeader(csrf, "Fetch");
                    Client.CookieContainer = new CookieContainer();
                    IRestResponse resp = Client.Execute(request);
                    if (HttpResponseInValidRange(resp.StatusCode))
                    {
                        CsrfToken = (from h in resp.Headers where h.Name == csrf select h).FirstOrDefault().Value.ToString();
                        request = null;
                    }
                    else
                        throw new MeisterException(resourceManager.GetString("FailedCsrfToken", CultureInfo.InvariantCulture));
                }
                IRestResponse<OD4Body<RES>> response = null;
                request = new RestRequest(Method.POST);
                request.RequestFormat = DataFormat.Json;
                var body = new Body<REQ>(endpoint, parm, req);
                var bodycall = new BodyCall();
                DoResourceAllocation(request, ExecuteMeister);
                bodycall.Parms = PerformExtensions<Parameters>(body.Parms);
                bodycall.Endpoint = body.Endpoint;
                bodycall.Json = PerformExtensions<REQ>(body.Json);
                if (options.HasFlag(MeisterOptions.CompressionsInbound))
                    bodycall.Json = Zip(bodycall.Json);
                string json = JsonConvert.SerializeObject(bodycall);
                cancel = new CancellationTokenSource();
                request.AddJsonBody(json);
                request.AddHeader("X-Request-With", "XMLHttpRequest");
                request.AddHeader("Accept", "*/*");
                request.AddHeader(csrf, CsrfToken);
                request.AddHeader("Content-Type", "application/json;odata.metadata=minimal; charset=utf-8");
                response = null;
                if (runtimeOption.HasFlag(RuntimeOptions.ExecuteAsync))
                {
                    var task = Task.Run(async () =>
                    {
                        response = await Client.ExecuteAsync<OD4Body<RES>>(request, cancel.Token).ConfigureAwait(true);
                        BuildStatusData<RES, OD4Body<RES>>(response);
                        return response.Data;
                    });
                    task.Wait(cancel.Token);
                    return task.Result;
                }
                else
                    return ExecuteSync<OD4Body<RES>>(request).Result;
            }
            finally
            {
                if (cancel != null)
                    cancel.Dispose();
            }
        }
        /// <summary>
        /// Execute calls under OData v2
        /// </summary>
        /// <typeparam name="REQ"></typeparam>
        /// <typeparam name="RES"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="req"></param>
        /// <param name="parm"></param>
        /// <param name="runtimeOption"></param>
        /// <returns></returns>
#pragma warning disable 1998
        internal dynamic ExecuteODataV2<REQ, RES>(string endpoint, REQ req, Parameters parm, RuntimeOptions runtimeOption, MeisterOptions options)
        {
            OD2Body<RES> od2 = ExecuteODataV2Internal<REQ, RES>(endpoint, req, parm, runtimeOption, options);
            if (od2 == null)
                return null;
            else if (od2.d == null && runtimeOption == RuntimeOptions.ExecuteSync)
                throw new MeisterException(resourceManager.GetString("OData2Failure", CultureInfo.InvariantCulture));
            else
            {
                try
                {
                    string json = string.Empty;
                    var list = od2.d.results;
                    if (list != null && list.Count > 0)
                    {
                        if (options.HasFlag(MeisterOptions.CompressionsOutbound))
                            json = Unzip(list.FirstOrDefault().Json);
                        else
                            json = list.FirstOrDefault().Json;
                        json = RemoveDrefAnnotations(json);
                    }
                    // first check if the backend sent a canonical failure .. if not, do nothing at the exception ..
                    // it comes in two flavors, as an object when threw from the gateway directly or as a list when threw from the backend
                    try
                    {
                        BackendFailure failure = JsonConvert.DeserializeObject<BackendFailure>(json);
                        if (failure != null)
                        {
                            throw new MeisterException(failure.BackendMessage, HttpStatusCode.InternalServerError);
                        }
                    }
                    catch (Exception)
                    {
                        try
                        {
                            List<BackendFailure> failures = JsonConvert.DeserializeObject<List<BackendFailure>>(json);
                            if (failures != null && failures.Count > 0)
                            {
                                BackendFailure failure = failures.FirstOrDefault();
                                if (failure.ReturnCode > 0)
                                    throw new MeisterException(failure.BackendMessage, HttpStatusCode.InternalServerError);
                            }
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
                    return VanillaProcess<RES>(json);
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
                    throw new MeisterException(resourceManager.GetString("UnableToMashallOD2", CultureInfo.InvariantCulture));
                }
            }
        }
        /// <summary>
        /// Execute internal of OData v2 call
        /// </summary>
        /// <typeparam name="REQ"></typeparam>
        /// <typeparam name="RES"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="req"></param>
        /// <param name="parm"></param>
        /// <param name="runtimeOption"></param>
        /// <returns></returns>
        private OD2Body<RES> ExecuteODataV2Internal<REQ, RES>(string endpoint, REQ req, Parameters parm, RuntimeOptions runtimeOption, MeisterOptions options)
        {
            CancellationTokenSource cancel = null;
            try
            {
                RestRequest request = new RestRequest(Method.GET);
                DoResourceAllocation(request, ExecuteMeister);
                request.AddQueryParameter(EndPoint, InQuotes(endpoint));
                request.AddQueryParameter(Parms, InQuotes(PerformExtensions<Parameters>(parm)));
                if (options.HasFlag(MeisterOptions.CompressionsOutbound))
                    request.AddQueryParameter(Content, InQuotes(Zip(PerformExtensions<REQ>(req))));
                else
                    request.AddQueryParameter(Content, InQuotes(PerformExtensions<REQ>(req)));
                cancel = new CancellationTokenSource();
                IRestResponse<OD2Body<RES>> response = null;
                if (runtimeOption.HasFlag(RuntimeOptions.ExecuteAsync))
                {
                    var task = Task.Run(async () =>
                    {
                        response = await Client.ExecuteAsync<OD2Body<RES>>(request, cancel.Token).ConfigureAwait(true);
                        BuildStatusData<RES,OD2Body<RES>>(response);
                        return response.Data;
                    });
                    task.Wait(cancel.Token);
                    return task.Result;
                }
                else
                    return ExecuteSync<OD2Body<RES>>(request).Result;
            }
            finally
            {
                if (cancel != null)
                    cancel.Dispose();
            }
        }
        /// <summary>
        /// Builds the Status object after the call is completed
        /// </summary>
        /// <typeparam name="RES"></typeparam>
        /// <param name="response"></param>
        private void BuildStatusData<RES,IR>(IRestResponse<IR> response)
        {
            MeisterStatus.StatusCode = response.StatusCode;
            MeisterStatus.StatusCodeDescription = response.StatusDescription;
            MeisterStatus.OriginalUrl = Client.BaseUrl.AbsoluteUri;
        }
        /// <summary>
        /// Builds the Status object after the call is completed
        /// </summary>
        /// <param name="response"></param>
        private void BuildStatusData(IRestResponse response)
        {
            MeisterStatus.StatusCode = response.StatusCode;
            MeisterStatus.StatusCodeDescription = response.StatusDescription;
            MeisterStatus.OriginalUrl = Client.BaseUrl.AbsoluteUri;
        }
        /// <summary>
        /// Set extensions at Meister runtime
        /// </summary>
        /// <param name="extensions"></param>
        internal void SetExtensions(MeisterExtensions extensions)
        {
            Extensions = extensions;
        }
        /// <summary>
        /// Json extensions from Meister
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="T"></param>
        /// <returns></returns>
        private string PerformExtensions<T>(T t)
        {
            string json;
            switch (Extensions)
            {
                case MeisterExtensions.RemoveNulls:
                    json = JsonConvert.SerializeObject(t, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                    break;
                case MeisterExtensions.RemoveNullsAndEmptyArrays:
                    json = JsonConvert.SerializeObject(t, new JsonSerializerSettings { ContractResolver = new SkipEmptyCollectionsContractResolver(), NullValueHandling = NullValueHandling.Ignore });
                    break;
                default:
                    json = JsonConvert.SerializeObject(t);
                    break;
            }
            return json;
        }
        /// <summary>
        /// Execute async
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<T> ExecuteAsync<T>(RestRequest request)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
            try
            {
                IRestResponse<T> response = await Client.ExecuteAsync<T>(request).ConfigureAwait(true);
                BuildStatusData(response);
                return response.Data;
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
        /// <summary>
        /// Sync execution
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <returns></returns>
        private Task<T> ExecuteSync<T>(RestRequest request)
        {
            var response = Client.Execute(request);
            BuildStatusData(response);
            try
            {
                var res = JsonConvert.DeserializeObject<T>(response.Content);
                return Task.FromResult<T>(res);
            }
            catch (MeisterException mex)
            {
                MeisterStatus.StatusCode = mex.httpStatusCode;
                MeisterStatus.LogEntry = mex.Message;
                throw;
            }
            catch (Exception ex)
            {
                try
                {
                    Failure = JsonConvert.DeserializeObject<BackendFailure>(response.Content);
                    throw new MeisterException(Failure.BackendMessage, HttpStatusCode.InternalServerError,ex);
                }
                catch (Exception exe)
                {
                    throw new MeisterException(resourceManager.GetString("MeisterOwn", CultureInfo.InvariantCulture), HttpStatusCode.InternalServerError, exe);
                }
            }
        }
        /// <summary>
        /// Check values of Response
        /// </summary>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public bool HttpResponseInValidRange(HttpStatusCode statusCode)
        {
            return (statusCode >= HttpStatusCode.OK && statusCode < HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Vanilla Processor
        /// </summary>
        /// <typeparam name="RES"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        private dynamic VanillaProcess<RES>(string json)
        {
            RawJsonResponse = json;
            try
            {
                List<VanillaResponse<RES>> vanilla = JsonConvert.DeserializeObject<List<VanillaResponse<RES>>>(json);
                if (IsVanillaResponse(vanilla))
                    return vanilla.FirstOrDefault().res;
                else
                    return JsonConvert.DeserializeObject<IEnumerable<RES>>(json).FirstOrDefault();
            }
            catch (Exception)
            {
                try
                {
                    VanillaResponse<RES> vanilla = JsonConvert.DeserializeObject<VanillaResponse<RES>>(json);
                    if (vanilla != null && vanilla.res != null)
                        return vanilla.res;
                    else
                        try
                        {
                            return JsonConvert.DeserializeObject<IEnumerable<RES>>(json).FirstOrDefault();
                        }
                        catch (Exception)
                        {
                            return JsonConvert.DeserializeObject<RES>(json);
                        }
                }
                catch (Exception)
                {
                    try
                    {
                        VanillaResponse vanilla = JsonConvert.DeserializeObject<VanillaResponse>(json);
                        if (vanilla != null)
                            return vanilla.res;
                        else
                            try
                            {
                                return JsonConvert.DeserializeObject<IEnumerable<RES>>(json).FirstOrDefault();
                            }
                            catch (Exception)
                            {
                                return JsonConvert.DeserializeObject<RES>(json);
                            }
                    }
                    catch (Exception)
                    {
                        try
                        {
                            return JsonConvert.DeserializeObject<IEnumerable<RES>>(json).FirstOrDefault();
                        }
                        catch (Exception)
                        {
                            try
                            {
                                return JsonConvert.DeserializeObject<RES>(json);
                            }
                            catch (Exception)
                            {
                                return DissectJson(json);
                            }
                        }
                    }                   
                }
            }
        }
        /// <summary>
        /// Checks if the envelope is dref based with IEnumerable
        /// </summary>
        /// <typeparam name="RES"></typeparam>
        /// <param name="vanilla"></param>
        /// <returns></returns>
        private bool IsVanillaResponse<RES>(List<VanillaResponse<RES>> vanilla)
        {
            if (vanilla != null && vanilla.Count > 0 && vanilla.FirstOrDefault().res != null)
                return true;
            return false;
        }
        /// <summary>
        /// Checks if the envelope is dref based without IEnumerable 
        /// </summary>
        /// <typeparam name="RES"></typeparam>
        /// <param name="vanilla"></param>
        /// <returns></returns>
        private bool IsVanillaResponse(List<VanillaResponse> vanilla)
        {
            if (vanilla != null && vanilla.Count > 0 && vanilla.FirstOrDefault().res != null)
                return true;
            return false;
        }
        /// <summary>
        /// Brute force approach
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private dynamic DissectJson(string json)
        {
            if (json.Contains(val))
            {
                string v = Quote + val + Quote + ":";
                string[] sa = { v };
                sa = json.Split(sa, System.StringSplitOptions.RemoveEmptyEntries);
                if (sa.Length == 2)
                    return sa[1].Substring(1, sa[1].Length - 1);
                else if (sa.Length == 1)
                    return sa[0].Substring(1, sa[0].Length - 1);
                else
                    return json;
            }
            else
                return json;
        }
        /// <summary>
        /// Checks if there is need to suffix with the SAP client number
        /// </summary>
        /// <returns></returns>
        private bool IsSuffixedSapClientNeeded()
        {
            short client = 0;
            if (!String.IsNullOrEmpty(sap_client))
                return Int16.TryParse(sap_client, out client) ;
            else
                return false;
        }
        /// <summary>
        /// Adds the resource to the request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="resource"></param>
        private void DoResourceAllocation(RestRequest request, string resource)
        {
            request.Resource = resource;
            request.RequestFormat = DataFormat.Json;
            var list = AddSAPSuffixes(request);
            if (list != null)
                foreach (var p in list)
                    request.AddParameter(p);
        }
        private List<Parameter> AddSAPSuffixes(RestRequest request)
        {
            List<Parameter> list = new List<Parameter>();
            if (SapClientOK)
                list.Add(new Parameter(Sap_client, sap_client, ParameterType.GetOrPost));
            if (sap_language != Languages.CultureBased)
            {
                var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
                foreach (var item in cultures)
                    if (item.TwoLetterISOLanguageName.ToUpper(CultureInfo.CurrentCulture) == Enum.GetName(typeof(Languages), sap_language))
                        request.AddHeader("Accept-Language", item.Name);
                list.Add(new Parameter(Sap_language, Enum.GetName(typeof(Languages), sap_language), ParameterType.GetOrPost));
            }
            return list;
        }
    }
}