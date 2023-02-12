using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Models.Parts;

namespace AutomotiveWorld.Models.Software.Applications
{
    public class MicrosoftDefenderApplication : Application
    {
        public const string MicrosoftDefenderName = "MicrosoftDefender";

        public MicrosoftDefenderApplication() : base(MicrosoftDefenderName, 1.0, true) { }

        public MicrosoftDefenderApplication(Application application) : base(application.Name, application.Version, application.Enabled) { }

        public override void Main(params object[] args)
        {
            if (!Enabled)
            {
                return;
            }

            Computer computer = args[0] as Computer;
            VehicleDto vehicleDto = args[1] as VehicleDto;

            // TODO create security events
            computer.Events.Enqueue($"fromDefender, Id=[{vehicleDto.Id}]");
        }
    }
}
