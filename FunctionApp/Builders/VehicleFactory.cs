using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Entities;
using AutomotiveWorld.Models;
using System;

namespace AutomotiveWorld.Builders
{
    public abstract class VehicleFactory
    {

        public static VehicleDto Create(Vin vin)
        {
            PsiSpec psiSpec = GetPsiSpec(vin.VehicleType);

            VehicleBuilder vehicleBuilder = vin.VehicleType switch
            {
                string a when a.Contains("Scooter") => new ScooterBuilder(vin, psiSpec),
                string a when a.Contains("Car") => new CarBuilder(vin, psiSpec),
                string a when a.Contains("Motor") => new MotorCycleBuilder(vin, psiSpec),
                string a when a.Contains("Truck") => new TruckBuilder(vin, psiSpec),
                _ => new GenericVehicleBuilder(vin, psiSpec)
            };
            vehicleBuilder.Build();

            return vehicleBuilder.VehicleDto;
        }

        public static PsiSpec GetPsiSpec(string vehicleType)
        {
            return vehicleType switch
            {
                string a when a.Contains("Scooter") => new PsiSpec(40, 50),
                string a when a.Contains("Car") => new PsiSpec(32, 38),
                string a when a.Contains("Motor") => new PsiSpec(28, 40),
                string a when a.Contains("Truck") => new PsiSpec(32, 38),
                _ => new PsiSpec(32, 38),
            };
        }

    }
}
