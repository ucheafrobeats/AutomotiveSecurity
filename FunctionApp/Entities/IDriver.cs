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

        Task AssignCar();

        Task Drive();

        Task ScheduleNextTrip();

        Task Delete();
    }
}
