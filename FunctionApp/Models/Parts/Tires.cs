using Newtonsoft.Json;

namespace AutomotiveWorld.Models.Parts
{
    public class Tires : Part
    {
        [JsonProperty("front", NullValueHandling = NullValueHandling.Ignore)]
        public Tire Front { get; set; }

        [JsonProperty("rear", NullValueHandling = NullValueHandling.Ignore)]
        public Tire Rear { get; set; }

        [JsonProperty("leftFront", NullValueHandling = NullValueHandling.Ignore)]
        public Tire LeftFront { get; set; }

        [JsonProperty("leftRear", NullValueHandling = NullValueHandling.Ignore)]
        public Tire LeftRear { get; set; }

        [JsonProperty("rightFront", NullValueHandling = NullValueHandling.Ignore)]
        public Tire RightFront { get; set; }

        [JsonProperty("rightRear", NullValueHandling = NullValueHandling.Ignore)]
        public Tire RightRear { get; set; }

        [JsonProperty("spare", NullValueHandling = NullValueHandling.Ignore)]
        public Tire Spare { get; set; }
    }
}
