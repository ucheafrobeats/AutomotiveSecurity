using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Entities;
using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using System;
using System.Collections.Generic;

namespace AutomotiveWorld.Builders
{
    public abstract class VehicleBuilder
    {
        private static readonly Random Rand = new();

        private static readonly Color[] Colors = (Color[])Enum.GetValues(typeof(Color));

        private const int TierMinYearOffset = 3;

        private const int SpareTierMinYearOffset = 5;

        public VehicleDto VehicleDto { get; private set; }

        public Vin Vin { get; private set; }

        public VehicleBuilder(Vin vin)
        {
            Vin = vin;
            VehicleDto = new VehicleDto()
            {
                Id = vin.Value,
                Make = vin.Make,
                Model = vin.Model,
                SerialNumber = vin.SerialNumber,
                Style = vin.BodyClass,
                TrimLevel = vin.Trim,
                Year = vin.ModelYear,
            };
        }

        public virtual void Build()
        {
            BuildFrame();
            BuildEngine();
            BuildTires();
            BuildDoors();

            Paint();
        }

        public virtual void Paint(Color? color = null)
        {
            VehicleDto.Color = color is null ? Colors[Rand.Next(Colors.Length)] : color.Value;
        }

        public virtual void BuildFrame()
        {
            VehicleDto[VehiclePartType.Frame] = new Frame();
        }

        public abstract void BuildTires();

        public virtual void BuildEngine()
        {
            Engine engine = new()
            {
                Displacement = Vin.DisplacementL,
                Type = Vin.TryGetValue("Fuel Type - Primary", out string fuelTypePrimary) ? EngineTypeFactory.FromString(fuelTypePrimary) : EngineType.Unknown,
                Cylinders = Vin.TryGetValue("Engine Number of Cylinders", out string numberOfCylinders) ? Convert.ToInt32(numberOfCylinders) : 0,
            };

            VehicleDto[VehiclePartType.Engine] = engine;
        }

        protected virtual void BuildTires(TireSideType[] tireSideTypes, int psiMinValue, int psiMaxValue, int spareTires = 0)
        {
            Tires tires = new();

            foreach (TireSideType tireSideType in tireSideTypes)
            {
                Tire tire = new()
                {
                    Pressure = Rand.Next(psiMinValue, psiMaxValue),
                    Year = Rand.Next(DateTime.Now.Year - TierMinYearOffset, DateTime.Now.Year)
                };

                typeof(Tires).GetProperty($"{tireSideType}").SetValue(tires, tire);
            }

            for (int i = spareTires; i > 0; i--)
            {
                tires.Spare = new Tire()
                {
                    Pressure = 60,
                    Year = Rand.Next(DateTime.Now.Year - SpareTierMinYearOffset, DateTime.Now.Year)
                };
            }

            VehicleDto[VehiclePartType.Tires] = tires;
        }

        public virtual void BuildDoors()
        {
            if (Vin.TryGetValue("Doors", out string value))
            {
                int numberOfDoors = Convert.ToInt32(value);

                List<Door> doors = new(numberOfDoors);
                for (int i = 0; i < numberOfDoors; i++)
                {
                    doors.Add(new Door());
                }

                VehicleDto.Parts[VehiclePartType.Doors] = doors;
            }
        }
    }
}
