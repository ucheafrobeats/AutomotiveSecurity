using Newtonsoft.Json;

namespace AutomotiveWorld.Models
{
    public class VehicleTelemetry
    {
        [JsonProperty("vin")]
        public string Vin { get; set; }

        [JsonProperty("vehicleJsonAsString")]
        public string VehicleJsonAsString;

        [JsonProperty("Type")]
        public string Type;
    }
}
