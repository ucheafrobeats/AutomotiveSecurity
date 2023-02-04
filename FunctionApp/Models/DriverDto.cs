using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace AutomotiveWorld.Models
{
    public class DriverDto
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty("isAvailable")]
        public bool IsAvailable { get; set; } = true;
    }
}
