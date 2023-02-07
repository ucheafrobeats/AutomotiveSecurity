using AutomotiveWorld.Models.Parts;
using Newtonsoft.Json;

namespace AutomotiveWorld.Models.Telemetry
{
    public class MaintenanceTelemetryPayload
    {
        [JsonProperty("tire")]
        public Tire Tire { get; set; }

        [JsonProperty("psiSpec")]
        public PsiSpec PsiSpec { get; set; }
    }
}
