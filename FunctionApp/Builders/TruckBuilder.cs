using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using System;
using System.Reflection;

namespace AutomotiveWorld.Builders
{
    class TruckBuilder : VehicleBuilder
    {
        public const int SpareTires = 2;

        private static readonly TireSideType[] TireSideTypes = {
            TireSideType.LeftFront,
            TireSideType.LeftRear,
            TireSideType.RightFront,
            TireSideType.RightRear
        };

        public TruckBuilder(Vin vin, PsiSpec psiSpec)
            : base(vin, psiSpec)
        {
        }

        public override void Build()
        {
            base.Build();

            VehicleDto[VehiclePartType.Multimedia] = MultimediaGenerator.GenerateMultimedia();
        }

        public override void BuildTires()
        {
            base.BuildTires(TireSideTypes, SpareTires);
        }
    }
}
