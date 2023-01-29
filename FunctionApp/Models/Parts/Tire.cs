using Newtonsoft.Json;

namespace AutomotiveWorld.Models.Parts
{
    public class Tire
    {
        [JsonProperty("year", Required = Required.Always)]
        public int Year { get; set; }

        [JsonProperty("pressure", Required = Required.Always)]
        public int Pressure { get; set; }
    }
}
