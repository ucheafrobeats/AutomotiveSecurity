using AutomotiveWorld.Models;
using System.Threading.Tasks;

namespace AutomotiveWorld.Entities
{
    public interface IVehicle
    {
        Task Create(VehicleDto vehicleDto);

        Task Delete();

        Task Start();

        Task AddDistance(double kilometers);

        Task Assign(Assignment assignment);
    }
}
