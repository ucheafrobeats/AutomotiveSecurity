using AutomotiveWorld.Models.Parts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace AutomotiveWorld.Models
{
    public class Vehicle
    {
        private readonly Dictionary<VehiclePartType, Part> _parts = new();

        private readonly VehicleType _vehicleType;

        [JsonProperty("accident")]
        public List<Accident> Accident { get; private set; } = new();

        [JsonProperty("battery")]
        public Battery Battery { get; set; } = new();

        [JsonProperty("color", Required = Required.Always)]
        public string Color { get; set; }

        [JsonProperty("make", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public VehicleMake Make { get; set; }

        [JsonProperty("model", Required = Required.Always)]
        public string Model { get; set; }

        [JsonProperty("kilometer")]
        public int Kilometer { get; private set; }

        [JsonProperty("serialNumber", Required = Required.Always)]
        public string SerialNumber { get; set; }

        [JsonProperty("style", Required = Required.Always)]
        public VehicleStyle Style { get; set; }

        [JsonProperty("trimLevel", Required = Required.Always)]
        public string TrimLevel { get; set; }

        [JsonProperty("vin", Required = Required.Always)]
        public Vin Vin { get; set; }

        [JsonProperty("year", Required = Required.Always)]
        public int Year { get; set; }

        public Vehicle(VehicleType vehicleType, string vin)
        {
            _vehicleType = vehicleType;
            Vin = new Vin(vin);
        }

        public Part this[VehiclePartType key]
        {
            get { return _parts.TryGetValue(key, out Part value) ? value : null; }
            set { _parts[key] = value; }
        }

        public void Show()
        {
            Console.WriteLine("\n---------------------------");
            Console.WriteLine("Vehicle Type: {0}, {1}", _vehicleType, Vin);
            Console.WriteLine(" Frame  : {0}", this[VehiclePartType.Frame]);
            Console.WriteLine(" Engine : {0}", JsonConvert.SerializeObject(_parts[VehiclePartType.Engine]));
            Console.WriteLine(" #Wheels: {0}", JsonConvert.SerializeObject(_parts[VehiclePartType.Tires]));
            Console.WriteLine(" #Doors : {0}", this[VehiclePartType.Door]);
        }
    }
}
