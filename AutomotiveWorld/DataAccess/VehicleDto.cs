﻿using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace AutomotiveWorld.DataAccess
{
    [JsonObject(MemberSerialization.OptIn)]
    public class VehicleDto : EntityDtoBase
    {
        [JsonProperty("parts")]
        public Dictionary<VehiclePartType, object> Parts = new();

        [JsonProperty("accidents")]
        public List<Accident> Accidents { get; private set; } = new();

        [JsonProperty("battery")]
        public Battery Battery { get; set; } = new();

        [JsonProperty("color", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public Color Color { get; set; }

        [JsonProperty("make")]
        public string Make { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("kilometer")]
        public int Kilometer { get; private set; }

        [JsonProperty("serialNumber", Required = Required.Always)]
        public string SerialNumber { get; set; }

        [JsonProperty("style")]
        public string Style { get; set; }

        [JsonProperty("trimLevel")]
        public string TrimLevel { get; set; }

        [JsonProperty("vehicleType")]
        public string VehicleType { get; set; }

        [JsonProperty("year", Required = Required.Always)]
        public int Year { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        public object this[VehiclePartType key]
        {
            get { return Parts.TryGetValue(key, out object value) ? value : null; }
            set { Parts[key] = value; }
        }

        public bool TryGetPart<T>(VehiclePartType key, out T part) where T : Part
        {
            part = null;

            if (Parts.TryGetValue(key, out object value))
            {

                part = (value as JObject).ToObject<T>();
                return true;

            }

            return false;
        }
    }
}
