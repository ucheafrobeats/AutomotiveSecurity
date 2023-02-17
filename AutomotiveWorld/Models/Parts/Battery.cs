using Newtonsoft.Json;

namespace AutomotiveWorld.Models.Parts
{
    public class Battery : Part
    {
        [JsonProperty("capacity")]
        public float Capacity { get; set; } = 100;
    }
}
