using Newtonsoft.Json;

namespace AutomotiveWorld.Models.Software
{
    public class Application
    {
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;
    }
}
