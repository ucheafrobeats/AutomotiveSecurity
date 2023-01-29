using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AutomotiveWorld.Models.Parts
{
    public class Engine : Part
    {
        [JsonProperty("displacement", Required = Required.Always)]
        public float Displacement { get; set; }

        [JsonProperty("type", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public EngineType Type { get; set; }
    }
}
