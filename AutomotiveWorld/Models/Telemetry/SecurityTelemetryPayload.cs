using AutomotiveWorld.Models.Parts;
using Newtonsoft.Json;

namespace AutomotiveWorld.Models.Telemetry
{
    public class SecurityTelemetryPayload
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("jsonAsString")]
        public string JsonAsString { get; set; }
    }
}
