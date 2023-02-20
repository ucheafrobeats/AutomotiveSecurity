using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using System;

namespace AutomotiveWorld.Builders
{
    class GenericVehicleBuilder : VehicleBuilder
    {
        public const int SpareTires = 0;

        private static readonly TireSideType[] TireSideTypes = {
            TireSideType.LeftFront,
            TireSideType.LeftRear,
            TireSideType.RightFront,
            TireSideType.RightRear
        };

        public GenericVehicleBuilder(Vin vin, PsiSpec psiSpec)
            : base(vin, psiSpec)
        {
        }

        public override void BuildTires()
        {
            base.BuildTires(TireSideTypes, SpareTires);
        }
    }
}
