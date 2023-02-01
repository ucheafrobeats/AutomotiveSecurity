using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using System;
using System.Reflection;

namespace AutomotiveWorld.Builders
{
    class TruckBuilder : VehicleBuilder
    {
        private const int PsiMinValue = 32;

        private const int PsiMaxValue = 38;

        private const int SpareTires = 2;

        private static readonly TireSideType[] TireSideTypes = {
            TireSideType.LeftFront,
            TireSideType.LeftRear,
            TireSideType.RightFront,
            TireSideType.RightRear
        };

        public TruckBuilder(Vin vin)
            : base(vin)
        {
        }

        public override void Build()
        {
            base.Build();

            VehicleDto[VehiclePartType.Multimedia] = MultimediaGenerator.GenerateMultimedia();
        }

        public override void BuildTires()
        {
            base.BuildTires(TireSideTypes, PsiMinValue, PsiMaxValue, SpareTires);
        }
    }
}
