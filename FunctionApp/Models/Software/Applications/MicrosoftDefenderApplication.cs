using AutomotiveWorld.DataAccess;
using System;

namespace AutomotiveWorld.Models.Software.Applications
{
    public class MicrosoftDefenderApplication : Application
    {
        public const string MicrosoftDefenderName = "MicrosoftDefender";

        public MicrosoftDefenderApplication() : base(MicrosoftDefenderName, 1.0, true) { }

        public override void Main(object arg)
        {
            VehicleDto vehicleDto = arg as VehicleDto;
            Console.WriteLine("A");
        }
    }
}
