using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using AutomotiveWorld.Models.Software;
using System.Collections.Generic;

namespace AutomotiveWorld.Models.Parts
{
    public class Multimedia : Part
    {
        [JsonProperty("operatingSystem", Required = Required.Always)]
        public OperatingSystem OperatingSystem { get; set; }

        [JsonProperty("firmware", Required = Required.Always)]
        public Firmware Firmware { get; set; }

        [JsonProperty("fileSystem")]
        public FileSystem FileSystem { get; set; }

        [JsonProperty("peripheral")]
        public Peripheral Peripheral { get; set; }

        [JsonProperty("applications")]
        public List<Application> Applications { get; set; }
    }
}
