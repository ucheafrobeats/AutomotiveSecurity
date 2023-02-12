using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;

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
                string a when a.Contains("Scooter") => Constants.Vehicle.Psi.Scooter,
                string a when a.Contains("Car") => Constants.Vehicle.Psi.Car,
                string a when a.Contains("Motor") => Constants.Vehicle.Psi.Motor,
                string a when a.Contains("Truck") => Constants.Vehicle.Psi.Truck,
                _ => Constants.Vehicle.Psi.Default,
            };
        }
    }
}
