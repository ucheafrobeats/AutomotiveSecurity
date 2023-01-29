using Newtonsoft.Json;

namespace AutomotiveWorld.Models.Software
{
    public class Firmware
    {
        [JsonProperty("vendor", Required = Required.Always)]
        public string Vendor { get; set; }

        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; set; }
    }
}
