using Newtonsoft.Json;
using System.Linq;
using System.Xml.Linq;

namespace AutomotiveWorld.Models
{
    public class Vin
    {
        [JsonProperty("value", Required = Required.Always)]
        public char[] Value { get; private set; } = new char[16];

        public Vin(string value)
        {
            Value = value.ToCharArray();
        }

        public char CountryOfManufacture { get { return Value[0]; } }

        public char VehicleManufacture { get { return Value[1]; } }

        public char VehicleType { get { return Value[2]; } }

        /// <summary>
        ///     Vehicle's brand, body style, model, series, etc
        /// </summary>
        public string VehicleInformation { get { return string.Join("", Value.Skip(3).Take(5)); } }

        public char SecurityCheckNumber { get { return Value[8]; } }

        public char ModelYear { get { return Value[9]; } }

        public char AssemblyPlant { get { return Value[10]; } }

        /// <summary>
        ///     Vehicle's serial number
        /// </summary>
        public string SerialNumber { get { return string.Join("", Value.Skip(11).Take(6)); } }

        public override string ToString()
        {
            return new string(Value);
        }
    }
}
