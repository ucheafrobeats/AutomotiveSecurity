using AutomotiveWorld.Models;

namespace AutomotiveWorld.Builders
{
    public abstract class VehicleBuilder
    {
        public Vehicle Vehicle { get; private set; }

        public VehicleBuilder(VehicleType vehicleType, string vin)
        {
            Vehicle = new Vehicle(vehicleType, vin);
        }

        public abstract void BuildFrame();

        public abstract void BuildEngine();

        public abstract void BuildTires();

        public abstract void BuildDoors();
    }
}
