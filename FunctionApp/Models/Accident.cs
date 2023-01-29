using Newtonsoft.Json;
using System;

namespace AutomotiveWorld.Models
{
    public class Accident
    {
        [JsonProperty("Timestamp")]
        public DateTime Timestamp { get; private set; } = DateTime.Now;

        [JsonProperty("description", Required = Required.Always)]
        public string Description { get; set; }
    }
}
