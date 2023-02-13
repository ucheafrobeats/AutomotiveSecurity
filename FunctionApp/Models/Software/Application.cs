using Newtonsoft.Json;
using System;

namespace AutomotiveWorld.Models.Software
{
    public class Application
    // FIXME make this class abstract and update durbale function data persistence to use TypeNameHandling.All by default
    {
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty("version", Required = Required.Always)]
        public double Version { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonConstructor]
        public Application(string name, double version, bool enabled)
        {
            Name = name;
            Version = version;
            Enabled = enabled;
        }

        public virtual void Main(params object[] args)
        {
            if (Enabled)
            {
                return;
            }
        }
    }
}
