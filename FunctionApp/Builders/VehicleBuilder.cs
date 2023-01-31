using AutomotiveWorld.Entities;
using AutomotiveWorld.Models;
using System;

namespace AutomotiveWorld.Builders
{
    public abstract class VehicleBuilder
    {
        private static Random Rand = new();
        private static readonly Color[] Colors = (Color[])Enum.GetValues(typeof(Color));

        public VehicleDto VehicleDto { get; private set; }

        public VehicleBuilder(Vin vin)
        {
            VehicleDto = new VehicleDto()
            {
                Vin = vin.Value,
                Make = vin.Make,
                Model = vin.Model,
                SerialNumber = vin.SerialNumber,
                Style = vin.BodyClass,
                TrimLevel = vin.Trim,
                Year = vin.ModelYear,
            };
        }

        public void Paint()
        {
            VehicleDto.Color = Colors[Rand.Next(Colors.Length)];
        }

        public abstract void BuildFrame();

        public abstract void BuildEngine();

        public abstract void BuildTires();

        public abstract void BuildDoors();
    }
}
