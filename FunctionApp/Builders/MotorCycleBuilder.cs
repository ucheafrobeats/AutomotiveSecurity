using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using System;

namespace AutomotiveWorld.Builders
{
    class MotorCycleBuilder : VehicleBuilder
    {
        private static readonly TireSideType[] _tireSideTypes = {
            TireSideType.Front,
            TireSideType.Rear
        };

        public MotorCycleBuilder(string vin)
            : base(VehicleType.MotorCycle, vin)
        {
        }

        public override void BuildFrame()
        {
            Vehicle[VehiclePartType.Frame] = new Frame();
        }

        public override void BuildEngine()
        {
            Random r = new();

            Engine engine = new()
            {
                Displacement = r.Next(1, 30) * 50,
                Type = EngineType.ESS
            };

            Vehicle[VehiclePartType.Engine] = engine;
        }

        public override void BuildTires()
        {
            Tires tires = new();

            foreach (TireSideType tireSideType in _tireSideTypes)
            {
                Random r = new();
                Tire tire = new()
                {
                    Pressure = r.Next(28, 40),
                    Year = r.Next(DateTime.Now.Year - 2, DateTime.Now.Year)
                };

                typeof(Tires).GetProperty($"{tireSideType}").SetValue(tires, tire);
            }
        }

        public override void BuildDoors()
        {
            Vehicle[VehiclePartType.Door] = null; // "0";
        }
    }
}
