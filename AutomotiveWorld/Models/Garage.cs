using AutomotiveWorld.Builders;
using AutomotiveWorld.Entities;
using AutomotiveWorld.Models.Parts;
using System;
using System.Reflection;

namespace AutomotiveWorld.Models
{
    public abstract class Garage
    {
        public static void Treat(Vehicle vehicle)
        {
            if (vehicle == null)
            {
                return;
            }

            TreatTires(vehicle);
        }

        private static void TreatTires(Vehicle vehicle)
        {
            if (!vehicle.TryGetPart(VehiclePartType.Tires, out Tires tires))
            {
                return;
            }

            PsiSpec psiSpec = VehicleFactory.GetPsiSpec(vehicle.VehicleType);

            foreach (PropertyInfo tireProperty in tires.GetType().GetProperties())
            {
                Tire tire = (Tire)tires.GetType().GetProperty(tireProperty.Name).GetValue(tires, null);

                if (tire is null)
                {
                    continue;
                }

                if (tireProperty.Name.Equals(TireSideType.Spare.ToString()))
                {
                    tire.Pressure = Constants.Vehicle.Tire.SpareMinPressure;
                }
                else
                {
                    tire.Pressure = psiSpec.MaxValue;
                }

                tire.Year = DateTime.Now.Year;
                tire.IsFaulty = false;
            }

            vehicle.Parts[VehiclePartType.Tires] = tires;
        }
    }
}
