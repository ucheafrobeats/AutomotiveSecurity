using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using System.Threading.Tasks;

namespace AutomotiveWorld.Entities
{
    public interface IVehicle
    {
        Task SetPart(PartDto partDto);

        Task Create(VehicleDto vehicleDto);

        Task Delete();

        Task StartEngine();

        Task TurnOffEngine();

        Task<bool> IsAvailable();

        Task Maintenance();

        Task Park();

        Task UpdateTrip(double kilometers);

        Task<bool> Assign(Assignment assignment);

        Task Unassign();
    }
}
