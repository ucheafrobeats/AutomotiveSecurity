using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using System;

namespace AutomotiveWorld.Builders
{
    class ScooterBuilder : VehicleBuilder
    {
        private static readonly TireSideType[] _tireSideTypes = {
            TireSideType.Front,
            TireSideType.Rear
        };

        public ScooterBuilder(Vin vin) : base(vin)
        {
        }

        public override void BuildFrame()
        {
            VehicleDto[VehiclePartType.Frame] = new Frame();
        }

        public override void BuildEngine()
        {
            Random r = new();

            Engine engine = new()
            {
                Displacement = r.Next(1, 5) * 50,
                Type = EngineType.ESS
            };

            VehicleDto[VehiclePartType.Engine] = engine;
        }

        public override void BuildTires()
        {
            Tires tires = new();

            foreach (TireSideType tireSideType in _tireSideTypes)
            {
                Random r = new();
                Tire tire = new()
                {
                    Pressure = r.Next(40, 50),
                    Year = r.Next(DateTime.Now.Year - 3, DateTime.Now.Year)
                };

                typeof(Tires).GetProperty($"{tireSideType}").SetValue(tires, tire);
            }
        }

        public override void BuildDoors()
        {
            VehicleDto[VehiclePartType.Door] = null; //  "0";
        }
    }
}
