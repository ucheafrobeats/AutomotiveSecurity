using AutomotiveWorld.Models;
using System.Threading.Tasks;

namespace AutomotiveWorld.Entities
{
    public interface IVehicle
    {
        Task Create(VehicleDto vehicleDto);

        Task Delete();

        Task Trip();
    }
}
