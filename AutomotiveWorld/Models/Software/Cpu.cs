using Newtonsoft.Json;
using System.Collections.Generic;

namespace AutomotiveWorld.Models.Software
{
    public class Cpu
    {
        [JsonProperty("commands")]
        public List<string> Commands { get; set; }
    }
}
