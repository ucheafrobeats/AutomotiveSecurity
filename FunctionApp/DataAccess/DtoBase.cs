using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomotiveWorld.DataAccess
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class EntityDtoBase
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("isAvailable")]
        public bool IsAvailable { get; set; } = true;
    }
}
