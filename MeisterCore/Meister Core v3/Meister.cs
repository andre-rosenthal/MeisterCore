using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
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

        public Languages sap_language { get; private set; }

        private bool SapClientOK;
        private string CsrfToken { get; set; }
        private MeisterExtensions Extensions { get; set; }
        private BackendFailure Failure {get;set;}
        private static readonly RestClient Client = new RestClient();
        public string RawJsonResponse { get; set; }
        /// <summary>
        /// Ctor 
        /// </summary>
        private Meister()
        {
        }
        /// <summary>
        /// Configuration step ..
        /// </summary>
        /// <param name="uri"></param>
        public void Configure(Uri uri, Protocols prot = Protocols.ODataV2, string sap_client = null, Languages sap_language = Languages.EN )
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
        internal bool Authenticate<REQ, RES>(AuthenticationModes authentications, AuthenticationHeaderValue credentials)
        {
            switch (authentications)
            {
                case AuthenticationModes.Basic:
                    if (!IsAutheticated)
                    {
                        string authHeader = credentials.Parameter;
                        if (authHeader != null)
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
                                if (HttpResponseInValidRange(response.StatusCode))
                                    IsAutheticated = true;
                                return IsAutheticated;
                            }
                        }
                        else
                            throw new MeisterException("The authorization header is either empty or isn't Basic");
                    }
                    else
                        return true;
                    break;
                case AuthenticationModes.OAuth:
                    if (!IsAutheticated)
                    {
                        string authHeader = credentials.Parameter;
                        if (authHeader != null)
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
                                if (HttpResponseInValidRange(response.StatusCode))
                                    IsAutheticated = true;
                                return IsAutheticated;
                            }
                        }
                    }
                    else
                        throw new MeisterException("The authorization header is either empty or isn't OAuth2");
                    break;
                case AuthenticationModes.JWT:
                    break;
                case AuthenticationModes.SAML2:
                    break;
                default:
                    break;
            }
            return false;
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
                    throw new MeisterException("Unable to marshall object. OD4Body(value) = " + od4.value);
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
                        throw new MeisterException("Failed to obtain a Csrf-Token from Meister");
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
                        response = await Client.ExecuteTaskAsync<OD4Body<RES>>(request, cancel.Token);
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
                throw new MeisterException("Failure to acquire OData 2 return set");
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
                        catch (MeisterException)
                        {
                            throw;
                        }
                        catch (Exception)
                        { }
                    }
                    return VanillaProcess<RES>(json);
                }
                catch (MeisterException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw new MeisterException("Unable to marshall object d[0] from OD2Body");
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
                        response = await Client.ExecuteTaskAsync<OD2Body<RES>>(request, cancel.Token);
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
                IRestResponse<T> response = await Client.ExecuteTaskAsync<T>(request);
                return response.Data;
            }
            catch (MeisterException)
            {
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
            try
            {
                var res = JsonConvert.DeserializeObject<T>(response.Content);
                return Task.FromResult<T>(res);
            }
            catch (MeisterException mex)
            {
                throw mex;
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
                    throw new MeisterException("Nuget exception", HttpStatusCode.InternalServerError, exe);
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
                if (sa.Count() == 2)
                    return sa[1].Substring(1, sa[1].Length - 1);
                else if (sa.Count() == 1)
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
            var list = AddSAPSuffixes();
            if (list != null)
                foreach (var p in list)
                    request.AddParameter(p);
        }
        private List<Parameter> AddSAPSuffixes()
        {
            Parameter p = null;
            List<Parameter> list = new List<Parameter>();
            if (SapClientOK)
            {
                p = new Parameter
                {
                    Name = Sap_client,
                    Value = sap_client
                };
                list.Add(p);
            }
            if (sap_language != Languages.EN)
            {
                p = new Parameter
                {
                    Name = Sap_language,
                    Value = Enum.GetName(typeof(Languages), sap_language)
                };
                list.Add(p);
            }
            return list;
        }

    }
}