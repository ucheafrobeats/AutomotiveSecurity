using Newtonsoft.Json;

namespace AutomotiveWorld.Models.Software
{
    public class Peripheral
    {
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }
    }
}
