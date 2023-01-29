using AutomotiveWorld.Builders;
using AutomotiveWorld.Models;
using System;

namespace AutomotiveWorld
{
    public class CarsFactory
    {
        private VehicleBuilder _vehicleBuilder;

        // Builder uses a complex series of steps
        public void Construct(VehicleBuilder vehicleBuilder)
        {
            _vehicleBuilder = vehicleBuilder;

            _vehicleBuilder.BuildFrame();
            _vehicleBuilder.BuildEngine();
            _vehicleBuilder.BuildTires();
            _vehicleBuilder.BuildDoors();
        }

        public void ShowVehicle()
        {
            _vehicleBuilder.Vehicle.Show();
        }

        public static Vehicle Generate()
        {
            return null;
            //return new Vehicle
            //{
            //    Vin = "4S3BMHB68B3286060",
            //    SerialNumber = "286060",
            //    Year = 2018,
            //    Make = Make.Honda,
            //    Model = "Civic",
            //    TrimLevel = "LX",
            //    Style = Style.Sedan,
            //    Color = "Black"
            //};
        }
    }
}
