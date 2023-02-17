using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AutomotiveWorld.Models.Telemetry
{
    public class CustomLogTelemetry
    {
        [JsonProperty("entityId")]
        public string EntityId { get; set; }

        [JsonProperty("jsonAsString")]
        public string JsonAsString { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("subType")]
        public string SubType { get; set; }
    }
}
