using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AutomotiveWorld.Models.Software
{
    public class OperatingSystem
    {
        [JsonProperty("architecture", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public OperatingSystemArchitecture Architecture { get; set; }

        [JsonProperty("platform", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public OperatingSystemPlatform Platform { get; set; }

        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; set; }
    }
}
