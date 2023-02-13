using Newtonsoft.Json;
using System.Collections.Generic;

namespace AutomotiveWorld.Models.Software
{
    public class FileSystem
    {
        [JsonProperty("files")]
        public List<string> Files { get; set; } = new();
    }
}
