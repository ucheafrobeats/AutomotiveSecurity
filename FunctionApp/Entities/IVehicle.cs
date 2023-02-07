using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Models;
using System.Threading.Tasks;

namespace AutomotiveWorld.Entities
{
    public interface IVehicle
    {
        Task Create(VehicleDto vehicleDto);

        Task Delete();

        Task StartEngine();

        Task TurnOffEngine();

        Task<bool> IsAvailable();

        Task AddDistance(double kilometers);

        Task Assign(Assignment assignment);

        Task Unassign();
    }
}
