using AutomotiveWorld.Models.Software;
using AutomotiveWorld.Models.Software.Applications;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public void NextCommand(object arg)
        {
            foreach (Application application in Applications)
            {
                if (MicrosoftDefenderApplication.MicrosoftDefenderName.Equals(application.Name))
                {
                    // FIXME cast is needed because durable entities serialization doesn't support abstract class by default
                    ((MicrosoftDefenderApplication)application).Main(arg);
                }
                else
                {
                    application.Main(arg);
                }

            }
        }
    }
}
