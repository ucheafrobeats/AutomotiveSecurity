using Newtonsoft.Json;

namespace AutomotiveWorld.Models.Telemetry
{
    public class SecurityTelemetryPayload
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
