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
            VehicleBuilder vehicleBuilder;

            switch (vin.VehicleType)
            {
                case string a when a.Contains("Scooter"):
                    vehicleBuilder = new ScooterBuilder(vin);
                    break;
                case string a when a.Contains("Car"):
                    vehicleBuilder = new CarBuilder(vin);
                    break;
                case string a when a.Contains("Motor"):
                    vehicleBuilder = new MotorCycleBuilder(vin);
                    break;
                case string a when a.Contains("Truck"):
                    vehicleBuilder = new TruckBuilder(vin);
                    break;
                default:
                    vehicleBuilder = new GenericVehicleBuilder(vin);
                    break;
            }

            vehicleBuilder.Build();

            return vehicleBuilder.VehicleDto;
        }
    }
}
