using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomotiveWorld.Entities
{
    public interface IDriver
    {
        Task Create(DriverDto driverDto);

        Task Assign(Assignment assignment);

        Task Unassign();

        Task<bool> StartDriving();

        Task<bool> Driving();

        Task StopDriving();

        Task<bool> IsAvailable();

        Task Delete();
    }
}
