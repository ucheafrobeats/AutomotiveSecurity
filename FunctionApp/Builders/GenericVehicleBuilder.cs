using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using System;

namespace AutomotiveWorld.Builders
{
    class GenericVehicleBuilder : VehicleBuilder
    {
        private const int PsiMinValue = 32;

        private const int PsiMaxValue = 38;

        private const int SpareTires = 0;

        private static readonly TireSideType[] TireSideTypes = {
            TireSideType.LeftFront,
            TireSideType.LeftRear,
            TireSideType.RightFront,
            TireSideType.RightRear
        };

        public GenericVehicleBuilder(Vin vin)
            : base(vin)
        {
        }

        public override void BuildTires()
        {
            base.BuildTires(TireSideTypes, PsiMinValue, PsiMaxValue, SpareTires);
        }
    }
}
