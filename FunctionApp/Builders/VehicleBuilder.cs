using AutomotiveWorld.Entities;
using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using System;
using System.Collections.Generic;

namespace AutomotiveWorld.Builders
{
    public abstract class VehicleBuilder
    {
        private static Random Rand = new();
        private static readonly Color[] Colors = (Color[])Enum.GetValues(typeof(Color));

        public VehicleDto VehicleDto { get; private set; }

        public Vin Vin { get; private set; }

        public VehicleBuilder(Vin vin)
        {
            Vin = vin;
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

        public virtual void BuildEngine()
        {
            if (Convert.ToBoolean(Vin.DisplacementL))
            {
                Engine engine = new()
                {
                    Displacement = Vin.DisplacementL,
                    Type = Vin.TryGetValue("Fuel Type - Primary", out string fuelTypePrimary) ? EngineTypeFactory.FromString(fuelTypePrimary) : EngineType.Unknown,
                    Cylinders = Vin.TryGetValue("Engine Number of Cylinders", out string numberOfCylinders) ? Convert.ToInt32(numberOfCylinders) : 0,
                };

                VehicleDto[VehiclePartType.Engine] = engine;
            }
        }

        public abstract void BuildTires();

        public virtual void BuildDoors()
        {
            if (Vin.TryGetValue("Doors", out string value))
            {
                int numberOfDoors = Convert.ToInt32(value);

                List<Door> doors = new List<Door>(numberOfDoors);
                for (int i = 0; i < numberOfDoors; i++)
                {
                    doors.Add(new Door());
                }

                VehicleDto.Parts[VehiclePartType.Doors] = doors;
            }
        }
    }
}
