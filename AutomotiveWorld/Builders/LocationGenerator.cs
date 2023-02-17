using AutomotiveWorld.Models.Parts;
using AutomotiveWorld.Models.Software;
using AutomotiveWorld.Models.Software.Applications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomotiveWorld.Builders
{
    public abstract class ComputerGenerator
    {
        private static readonly Random Rand = new();

        public static readonly IList<Computer> ComputerDataset = new List<Computer>()
        {
            new()
            {
                OperatingSystem = new Models.Software.OperatingSystem()
                {
                    Architecture = OperatingSystemArchitecture.Arm64,
                    Platform = OperatingSystemPlatform.Windows,
                    Version = 1.0
                },
                Firmware = new Firmware()
                {
                    Version = 1.0,
                    Vendor = "Microsoft"
                },
                FileSystem = new FileSystem(),
                Peripheral = new Peripheral()
                {
                    Name = "Usb",
                    InUse = false,
                    Enabled = false
                },
                Applications = new()
                {
                    new MicrosoftDefenderApplication()
                }
            }
        };

        public static Computer Next()
        {
            // Deepcopy
            Computer computer = ComputerDataset.ElementAt(Rand.Next(0, ComputerDataset.Count - 1));
            return JsonConvert.DeserializeObject<Computer>(JsonConvert.SerializeObject(computer));
        }
    }
}
