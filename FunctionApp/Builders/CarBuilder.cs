using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;

namespace AutomotiveWorld.Builders
{
    class CarBuilder : VehicleBuilder
    {
        private const int PsiMinValue = 32;

        private const int PsiMaxValue = 38;

        private const int SpareTires = 1;

        private static readonly TireSideType[] TireSideTypes = {
            TireSideType.LeftFront,
            TireSideType.LeftRear,
            TireSideType.RightFront,
            TireSideType.RightRear
        };

        public CarBuilder(Vin vin)
            : base(vin)
        {
        }

        public override void BuildTires()
        {
            base.BuildTires(TireSideTypes, PsiMinValue, PsiMaxValue, SpareTires);
        }
    }
}
