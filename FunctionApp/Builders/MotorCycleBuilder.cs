using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using System;

namespace AutomotiveWorld.Builders
{
    class MotorCycleBuilder : VehicleBuilder
    {
        private static readonly TireSideType[] TireSideTypes = {
            TireSideType.Front,
            TireSideType.Rear
        };

        public MotorCycleBuilder(Vin vin, PsiSpec psiSpec)
            : base(vin, psiSpec)
        {
        }

        public override void BuildTires()
        {
            base.BuildTires(TireSideTypes);
        }
    }
}
