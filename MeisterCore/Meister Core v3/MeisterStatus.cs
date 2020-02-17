using Newtonsoft.Json;
using RestSharp;
using System.Net;
using System.Xml;
namespace MeisterCore.Support
{
    public partial class MeisterStatus
    {
        public MeisterStatus()
        {
            StatusCode = HttpStatusCode.Unauthorized;
            StatusCodeDescription = HttpStatusCode.Unauthorized.ToString();
            LogEntry = string.Empty;
            OriginalUrl = string.Empty;
            FromEndpoint = string.Empty;
        }
        public MeisterStatus(IRestResponse response, string endpoint)
        {
            if (response.ResponseUri != null)
            {
                StatusCode = response.StatusCode;
                StatusCodeDescription = response.StatusDescription;
                LogEntry = response.Content;
                OriginalUrl = response.ResponseUri.AbsoluteUri;
                FromEndpoint = endpoint;
                if (LogEntry.Contains("<?xml"))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(LogEntry);
                    LogEntry = JsonConvert.SerializeXmlNode(doc);
                }
            }
            else
            {
                StatusCode = HttpStatusCode.ServiceUnavailable;
                StatusCodeDescription = HttpStatusCode.GetName(typeof(HttpStatusCode), StatusCode);
                FromEndpoint = endpoint;
                OriginalUrl = response.Request.Resource;
            }
        }
        [JsonProperty("statusCode")]
        public HttpStatusCode StatusCode  { get; set; }
        [JsonProperty("statusCodeDescription")]
        public string StatusCodeDescription { get; set; }
        [JsonProperty("logEntry")]
        public string LogEntry { get; set; }
        [JsonProperty("originalUrl")]
        public string OriginalUrl { get; set; }
        [JsonProperty("fromEndpoint")]
        public string FromEndpoint { get; set; }
    }
}