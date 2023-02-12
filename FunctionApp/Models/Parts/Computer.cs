using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Models.Software;
using AutomotiveWorld.Models.Software.Applications;
using AutomotiveWorld.Models.Telemetry;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AutomotiveWorld.Models.Parts
{
    public class Computer : Part
    {
        [JsonProperty("operatingSystem", Required = Required.Always)]
        public OperatingSystem OperatingSystem { get; set; }

        [JsonProperty("firmware", Required = Required.Always)]
        public Firmware Firmware { get; set; }

        [JsonProperty("fileSystem")]
        public FileSystem FileSystem { get; set; }

        [JsonProperty("peripheral")]
        public Peripheral Peripheral { get; set; }

        [JsonProperty("applications")]
        public List<Application> Applications { get; set; }

        [JsonProperty("ram")]
        public Ram Ram { get; set; }

        [JsonProperty("cpu")]
        public Cpu Cpu { get; set; }

        [JsonProperty("memory")]
        public Memory Memory { get; set; }

        [JsonProperty("events")]
        public Queue<CustomLogTelemetry> Events { get; set; } = new();

        public void NextCommand(params object[] args)
        {
            foreach (Application application in Applications)
            {
                if (MicrosoftDefenderApplication.MicrosoftDefenderName.Equals(application.Name))
                {
                    VehicleDto vehicleDto = (VehicleDto)args[0];
                    // FIXME cast is needed because durable entities serialization doesn't support abstract class by default
                    MicrosoftDefenderApplication microsoftDefenderApplication = new(application);
                    microsoftDefenderApplication.Main(this, vehicleDto);
                }
                else
                {
                    application.Main(args);
                }

            }
        }
    }
}
