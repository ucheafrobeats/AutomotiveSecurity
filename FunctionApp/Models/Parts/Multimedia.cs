using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using AutomotiveWorld.Models.Software;

namespace AutomotiveWorld.Models.Parts
{
    public class Multimedia : Part
    {
        [JsonProperty("operatingSystem", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public OperatingSystem OperatingSystem { get; set; }

        [JsonProperty("firmware", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public Firmware Firmware { get; set; }

        [JsonProperty("fileSystem")]
        [JsonConverter(typeof(StringEnumConverter))]
        public FileSystem FileSystem { get; set; }
    }
}
