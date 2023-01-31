using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using System;

namespace AutomotiveWorld.Builders
{
    class GenericVehicleBuilder : VehicleBuilder
    {
        private static readonly TireSideType[] _tireSideTypes = {
            TireSideType.LeftFront,
            TireSideType.LeftRear,
            TireSideType.RightFront,
            TireSideType.RightRear
        };

        public GenericVehicleBuilder(Vin vin)
            : base(vin)
        {
        }

        public override void BuildFrame()
        {
            VehicleDto[VehiclePartType.Frame] = new Frame();
        }

        //public override void BuildEngine()
        //{
        //    Random r = new();

        //    Engine engine = new()
        //    {
        //        Displacement = r.Next(10, 30) * 100,
        //        Type = EngineType.DSL
        //    };

        //    VehicleDto[VehiclePartType.Engine] = engine;
        //}

        public override void BuildTires()
        {
            Tires tires = new();
            Random r;

            foreach (TireSideType tireSideType in _tireSideTypes)
            {
                r = new();
                Tire tire = new()
                {
                    Pressure = r.Next(32, 38),
                    Year = r.Next(DateTime.Now.Year - 3, DateTime.Now.Year)
                };

                typeof(Tires).GetProperty($"{tireSideType}").SetValue(tires, tire);
            }

            // Generate Spare tire
            r = new();
            if (Convert.ToBoolean(r.Next(0, 2)))
            {
                tires.Spare = new Tire()
                {
                    Pressure = 60,
                    Year = r.Next(DateTime.Now.Year - 5, DateTime.Now.Year)
                };
            }

            VehicleDto[VehiclePartType.Tires] = tires;
        }
    }
}
