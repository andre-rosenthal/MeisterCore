using Newtonsoft.Json;

namespace MeisterCore
{
   
    public enum Compressions
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
        private string _style;
        private string _sDKHint;
        private const string jpCompression = "COMPRESSION";

        [JsonProperty(jpCompression)]
        public string Compression { get => _compression; set => _compression = value; }
        [JsonProperty("TEST_RUN")]
        public string Testrun { get => _testrun; set => _testrun = value; }
        [JsonProperty("STYLE")]
        public string Style { get => _style; set => _style = value; }
        [JsonProperty("SDK_HINT")]
        public string SDKHint { get => _sDKHint; set => _sDKHint = value; }
        public Parameters()
        {
            Style = "Default";
            Testrun = string.Empty;
            Compression = string.Empty;
            SDKHint = string.Empty;
        }
    }
}