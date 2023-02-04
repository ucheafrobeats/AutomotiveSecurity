using Newtonsoft.Json;
using System;

namespace AutomotiveWorld.Models
{
    public class Assignment
    {
        [JsonProperty("Id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("vehicleDto")]
        public VehicleDto VehicleDto { get; set; }

        [JsonProperty("driverDto")]
        public DriverDto DriverDto { get; set; }

        [JsonProperty("scheduledTime")]
        public DateTime ScheduledTime { get; set; }

        [JsonProperty("totalKilometers")]
        public double TotalKilometers { get; set; }

        [JsonProperty("currentKilometers")]
        public double CurrentDistance { get; set; } = 0;
    }
}
