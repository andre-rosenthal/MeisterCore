using Newtonsoft.Json;
namespace MeisterCore.Support
{
    public partial class BackendFailure
    {
        [JsonProperty("meisterFailed")]
        public string MeisterFailed { get; set; }

        [JsonProperty("returnCode")]
        public long ReturnCode { get; set; }

        [JsonProperty("backendMessage")]
        public string BackendMessage { get; set; }

        [JsonProperty("fromEndpoint")]
        public string FromEndpoint { get; set; }
    }
}