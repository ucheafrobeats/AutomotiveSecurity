using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using AutomotiveWorld.Models.Software;

namespace AutomotiveWorld.Builders
{
    class CarBuilder : VehicleBuilder
    {
        public const int SpareTires = 1;

        private static readonly TireSideType[] TireSideTypes = {
            TireSideType.LeftFront,
            TireSideType.LeftRear,
            TireSideType.RightFront,
            TireSideType.RightRear
        };

        public CarBuilder(Vin vin, PsiSpec psiSpec)
            : base(vin, psiSpec)
        {
        }

        public override void Build()
        {
            base.Build();

            VehicleDto[VehiclePartType.Multimedia] = MultimediaGenerator.Next();
        }

        public override void BuildTires()
        {

            base.BuildTires(TireSideTypes, SpareTires);
        }
    }
}
