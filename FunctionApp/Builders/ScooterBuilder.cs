using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using System;

namespace AutomotiveWorld.Builders
{
    class ScooterBuilder : VehicleBuilder
    {
        private const int PsiMinValue = 40;

        private const int PsiMaxValue = 50;

        private static readonly TireSideType[] TireSideTypes = {
            TireSideType.Front,
            TireSideType.Rear
        };

        public ScooterBuilder(Vin vin) : base(vin)
        {
        }

        public override void BuildTires()
        {
            base.BuildTires(TireSideTypes, PsiMinValue, PsiMaxValue);
        }
    }
}
