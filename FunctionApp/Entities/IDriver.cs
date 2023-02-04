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

        Task Drive(int totalKilometer);

        Task Assign(Assignment assignment);

        Task<bool> StartDriving();

        Task Driving();

        Task Delete();
    }
}
