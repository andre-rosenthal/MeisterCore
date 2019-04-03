using Newtonsoft.Json;

namespace MeisterCore
{
   
    public enum CompressionsP
    {
        none,
        Inbound,
        Outbound,
        Both
    }

    [JsonObject("Parms")]
    public class Parameters
    {
        public const string NoCompression = "";
        public const string Inbound = "I";
        public const string Outbound = "O";
        public const string Both = "B";
        private string _testrun;
        private string _compression;
        private string _async;
        private string _style;
        private string _sDKHint;
        private string _queued;
        private string _callback;
        [JsonProperty("COMPRESSION")]
        public string Compression { get => _compression; set => _compression = value; }
        [JsonProperty("TEST_RUN")]
        public string Testrun { get => _testrun; set => _testrun = value; }
        [JsonProperty("STYLE")]
        public string Style { get => _style; set => _style = value; }
        [JsonProperty("SDK_HINT")]
        public string SDKHint { get => _sDKHint; set => _sDKHint = value; }
        [JsonProperty("ASYNC")]
        public string Async { get => _async; set => _async = value; }
        [JsonProperty("QUEUED")]
        public string Queued { get => _queued; set => _queued = value; }
        [JsonProperty("CALLBACK")]
        public string Callback { get => _callback; set => _callback = value; }
        public Parameters()
        {
            Style = "Default";
            Testrun = string.Empty;
            Compression = string.Empty;
            SDKHint = string.Empty;
            Async = string.Empty;
            Queued = string.Empty;
            Callback = string.Empty;
        }
    }
}