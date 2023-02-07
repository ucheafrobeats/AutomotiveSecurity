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

        [JsonProperty("Type")]
        public string Type { get; set; }
    }
}
