using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
    /// Post class support - pre call
    /// </summary>
    /// <typeparam name="REQ"></typeparam>
    internal class Body<REQ>
    {
        public string Endpoint { get; set; }
        public Parameters Parms { get; set; }
        public REQ Json { get; set; }
        public Body(string endpoint, Parameters parms, REQ json)
        {
            Endpoint = endpoint;
            Parms = parms;
            Json = json;
        }
    }
    /// <summary>
    /// Post class support - at call
    /// </summary>
    /// <typeparam name="REQ"></typeparam>
    internal class BodyCall
    {
        public string Endpoint { get; set; }
        public string Parms { get; set; }
        public string Json { get; set; }
        public BodyCall()
        {
            Endpoint = string.Empty;
            Parms = string.Empty;
            Json = string.Empty;
        }
    }
    /// <summary>
    /// OData v4 support class
    /// </summary>
    [JsonObject("OD4Body")]
    public class OD4Body<RES>
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; }
        [JsonProperty("value")]
        public string value { get; set; }
    }
    /// <summary>
    /// OData v2 class support
    /// </summary>
    /// <typeparam name="REQ"></typeparam>
    internal class OD2Body<REQ>
    {
        public D d { get; set; }
    }
    /// <summary>
    /// Node D of OData v2 payload
    /// </summary>
    internal class D
    {
        public List<ResultOD2> results { get; set; }
    }
    /// <summary>
    /// Node Result of OData v2 payload
    /// </summary>
    internal class ResultOD2
    {
        public __Metadata __metadata { get; set; }
        public string Endpoint { get; set; }
        public string Parms { get; set; }
        public string Json { get; set; }
    }
    /// <summary>
    /// Node __Metadata of OData v2 payload
    /// </summary>
    internal class __Metadata
    {
        public string id { get; set; }
        public string uri { get; set; }
        public string type { get; set; }
    }
    /// <summary>
    /// Summary description for Meister Core Library Definistions - SIngleton pattern
    /// </summary>
    ///
    internal partial class Meister : SingletonBase<Meister>
    {
        private const string MeisterHostOD2 = @"/sap/opu/odata/MEISTER/ENGINE/";
        private const string MeisterHostOD4 = @"/sap/opu/odata4/meister/od4grp/default/meister/od4/0001/";
        private const string ExecuteMeister = @"Execute";
        private const string TokenFetch = @"Requests";
        private const string metadata = @"$metadata?sap-statistics=true";
        private const string InJson = "&$format=json";
        private const string Content = "Json";
        private const string EndPoint = "Endpoint";
        private const string Parms = "Parms";
        private const string csrf = "x-csrf-token";
        public Protocols ODataProtocol { get; set; }
        public Uri Uri { get; set; }
        public bool IsAutheticated { get; set; }
        private string username;
        private string password;
        private string accessToken;
        private string tokenType;
        private string CsrfToken { get; set; }
        private MeisterExtensions Extensions { get; set; }
        private static readonly RestClient Client = new RestClient();
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
        public void Configure(Uri uri, Protocols prot = Protocols.ODataV2)
        {
            ODataProtocol = prot;
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
            if (authentications == AuthenticationModes.Basic && !IsAutheticated)
            {
                string authHeader = credentials.Parameter;
                if (authHeader != null)
                {
                    string encodedUsernamePassword = authHeader.Trim();
                    Encoding encoding = Encoding.GetEncoding("iso-8859-1");
                    string usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));
                    int seperatorIndex = usernamePassword.IndexOf(':');
                    username = usernamePassword.Substring(0, seperatorIndex);
                    password = usernamePassword.Substring(seperatorIndex + 1);
                    IAuthenticator authenticator = Client.Authenticator = new HttpBasicAuthenticator(username, password);
                    if (authenticator != null)
                    {
                        var request = new RestRequest(Method.GET);
                        request.Resource = metadata;
                        request.AddHeader(csrf, "Fetch");
                        IRestResponse response = Client.Execute(request);
                        if (HttpResponseInValidRange(response.StatusCode))
                            IsAutheticated = true;
                        return response.ResponseStatus == ResponseStatus.Completed;
                    }
                }
                else
                    throw new MeisterException("The authorization header is either empty or isn't Basic");
            }
            else
            {
                if (authentications == AuthenticationModes.OAuth && !IsAutheticated)
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
                            request.Resource = metadata;
                            request.AddHeader("Authorization", string.Format("bearer {0}", accessToken));
                            request.AddHeader("Accept", "application/json");
                            IRestResponse response = Client.Execute(request);
                            if (HttpResponseInValidRange(response.StatusCode))
                                IsAutheticated = true;
                            return response.ResponseStatus == ResponseStatus.Completed;
                        }
                    }
                }
                else
                    throw new MeisterException("The authorization header is either empty or isn't OAuth2");
            }
            return false;
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
        internal dynamic ExecuteODataV4<REQ, RES>(string endpoint, REQ req, Parameters parm, RuntimeOptions runtimeOption)
        {
            OD4Body<RES> od4 = ExecuteODataV4Internal<REQ, RES>(endpoint, req, parm, runtimeOption);
            if (od4 == null)
                return null;
            else if (string.IsNullOrEmpty(od4.odatacontext) && runtimeOption == RuntimeOptions.ExecuteSync)
                throw new MeisterException(od4.value);
            else
                try
                {
                    return JsonConvert.DeserializeObject<IEnumerable<RES>>(od4.value);
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
        private OD4Body<RES> ExecuteODataV4Internal<REQ, RES>(string endpoint, REQ req, Parameters parm, RuntimeOptions runtimeOption)
        {
            RestRequest request = null;
            if (string.IsNullOrEmpty(CsrfToken))
            {
                request = new RestRequest(Method.GET);
                request.Resource = TokenFetch;
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
            request.Resource = ExecuteMeister;
            bodycall.Json = PerformExtensions<REQ>(body.Json);
            bodycall.Json = Unescape(bodycall.Json);
            bodycall.Parms = PerformExtensions<Parameters>(body.Parms);
            bodycall.Parms = Unescape(bodycall.Parms);
            bodycall.Endpoint = body.Endpoint;
            string json = JsonConvert.SerializeObject(bodycall);
            CancellationTokenSource cancel = new CancellationTokenSource();
            request.AddJsonBody(json);
            request.AddHeader("X-Request-With", "XMLHttpRequest");
            request.AddHeader("Accept", "*/*");
            request.AddHeader(csrf, CsrfToken);
            request.AddHeader("Content-Type","application/json;odata.metadata=minimal; charset=utf-8");
            response = null;
            if (runtimeOption == RuntimeOptions.ExecuteAsync)
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
        internal async Task<OD2Body<RES>> ExecuteODataV2<REQ, RES>(string endpoint, REQ req, Parameters parm, RuntimeOptions runtimeOption)
        {
            RestRequest request = new RestRequest(Method.GET);
            request.Resource = ExecuteMeister;
            request.AddQueryParameter(EndPoint, InQuotes(endpoint));
            request.AddQueryParameter(Parms, PerformExtensions<Parameters>(parm));
            request.AddQueryParameter(Content, PerformExtensions<REQ>(req));
            Task<OD2Body<RES>> task = default(Task<OD2Body<RES>>);
            if (runtimeOption == RuntimeOptions.ExecuteAsync)
            {
                var taska = Task.Run(async () =>
                {
                    return await ExecuteAsync<OD2Body<RES>>(request);
                });
                task = taska;
                return task.Result;
            }
            else
            {
                task = ExecuteSync<OD2Body<RES>>(request);
                return task.Result;
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
            string json = string.Empty;
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
            catch (MeisterException)
            {
                return null;
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

    }
}

