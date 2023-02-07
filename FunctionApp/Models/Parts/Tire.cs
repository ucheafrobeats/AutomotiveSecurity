using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AutomotiveWorld.Models.Parts
{
    public class Tire
    {
        [JsonProperty("year", Required = Required.Always)]
        public int Year { get; set; }

        [JsonProperty("pressure", Required = Required.Always)]
        public int Pressure { get; set; }

        [JsonProperty("side", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public TireSideType Side { get; set; }
    }
}
