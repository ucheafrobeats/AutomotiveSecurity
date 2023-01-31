﻿using AutomotiveWorld.Models.Parts;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AutomotiveWorld.Models
{
    public class Vin
    {
        /** Using https://vpic.nhtsa.dot.gov/api/ results map
        ** Referense: https://vpic.nhtsa.dot.gov/api/vehicles/decodevin/4S3BMHB68B3286050?format=json
        **/

        private readonly IDictionary<string, string> _nhtsaMap;

        [JsonProperty("value", Required = Required.Always)]
        public string Value { get; private set; }


        public Vin(string vin, IDictionary<string, string> nhtsaMap)
        {
            _nhtsaMap = nhtsaMap;
            Value = vin;
        }

        public int ModelYear { get { return _nhtsaMap.TryGetValue("Model Year", out string value) ? Convert.ToInt32(value) : 0; } }

        public string Model { get { return _nhtsaMap.TryGetValue("Model", out string value) ? value : null; } }

        public string Make { get { return _nhtsaMap.TryGetValue("Make", out string value) ? value : null; } }

        public string ManufacturerName { get { return _nhtsaMap.TryGetValue("Manufacturer Name", out string value) ? Normalize(value) : null; } }

        public string VehicleType { get { return _nhtsaMap.TryGetValue("Vehicle Type", out string value) ? Normalize(value) : null; } }

        public string Trim { get { return _nhtsaMap.TryGetValue("Trim", out string value) ? value : null; } }

        public string BodyClass { get { return _nhtsaMap.TryGetValue("Body Class", out string value) ? value : null; } }

        public float DisplacementL { get { return _nhtsaMap.TryGetValue("Displacement (L)", out string value) ? (float)Convert.ToDouble(value) : 0; } }

        public string SerialNumber { get { return Value[11..]; } }

        public bool IsValid()
        {
            return ModelYear != 0 &&
                !string.IsNullOrWhiteSpace(Make) &&
                !string.IsNullOrWhiteSpace(VehicleType) &&
                DisplacementL != 0;
        }

        private static string Normalize(string value)
        {
            value = value.ToLower();
            TextInfo info = CultureInfo.CurrentCulture.TextInfo;
            return info.ToTitleCase(value).Replace(" ", string.Empty);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
