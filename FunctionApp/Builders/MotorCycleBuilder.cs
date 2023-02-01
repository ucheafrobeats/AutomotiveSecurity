using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using System;

namespace AutomotiveWorld.Builders
{
    class MotorCycleBuilder : VehicleBuilder
    {
        private const int PsiMinValue = 28;

        private const int PsiMaxValue = 40;

        private static readonly TireSideType[] TireSideTypes = {
            TireSideType.Front,
            TireSideType.Rear
        };

        public MotorCycleBuilder(Vin vin)
            : base(vin)
        {
        }

        public override void BuildTires()
        {
            base.BuildTires(TireSideTypes, PsiMinValue, PsiMaxValue);
        }
    }
}
