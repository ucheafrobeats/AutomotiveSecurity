﻿using AutomotiveWorld.DataAccess;
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

        public VehicleDto VehicleDto { get; private set; }

        public Vin Vin { get; private set; }

        public PsiSpec PsiSpec { get; private set; }

        public VehicleBuilder(Vin vin, PsiSpec psiSpec)
        {
            Vin = vin;
            PsiSpec = psiSpec;
            VehicleDto = new VehicleDto()
            {
                Id = vin.Value,
                Make = vin.Make,
                Model = vin.Model,
                SerialNumber = vin.SerialNumber,
                Style = vin.BodyClass,
                TrimLevel = vin.Trim,
                VehicleType = vin.VehicleType,
                Year = vin.ModelYear,
                Location = LocationGenerator.Next()
            };
        }

        public virtual void Build()
        {
            BuildComputer();
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

        public virtual void BuildComputer()
        {
            VehicleDto[VehiclePartType.Computer] = ComputerGenerator.Next();
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
                Type = Vin.EngineType,
                Cylinders = Vin.TryGetValue("Engine Number of Cylinders", out string numberOfCylinders) ? Convert.ToInt32(numberOfCylinders) : 0,
            };

            VehicleDto[VehiclePartType.Engine] = engine;
        }

        protected virtual void BuildTires(TireSideType[] tireSideTypes, int spareTires = 0)
        {
            Tires tires = new();

            foreach (TireSideType tireSideType in tireSideTypes)
            {
                Tire tire = new()
                {
                    Pressure = Rand.Next(PsiSpec.MinValue, PsiSpec.MaxValue),
                    Year = Rand.Next(DateTime.Now.Year - Constants.Vehicle.Tire.NonSpareMinYear, DateTime.Now.Year),
                    Side = tireSideType

                };

                typeof(Tires).GetProperty($"{tireSideType}").SetValue(tires, tire);
            }

            for (int i = spareTires; i > 0; i--)
            {
                tires.Spare = new Tire()
                {
                    Pressure = Constants.Vehicle.Tire.SpareMinPressure,
                    Year = Rand.Next(DateTime.Now.Year - Constants.Vehicle.Tire.SpareMinYear, DateTime.Now.Year),
                    Side = TireSideType.Spare
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
