using AutomotiveWorld.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AutomotiveWorld.DataAccess
{
    public class PartDto
    {
        [JsonProperty("type", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public VehiclePartType Type { get; set; }


        [JsonProperty("part", Required = Required.Always)]
        public object Part { get; set; }
    }
}
