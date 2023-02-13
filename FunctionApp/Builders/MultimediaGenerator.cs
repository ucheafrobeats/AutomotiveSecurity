using AutomotiveWorld.Models.Parts;
using AutomotiveWorld.Models.Software;
using AutomotiveWorld.Models.Software.Applications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomotiveWorld.Builders
{
    public abstract class MultimediaGenerator
    {
        private static readonly Random Rand = new();

        public static readonly IList<Multimedia> MultimediasDataset = new List<Multimedia>()
        {
            new()
            {
                OperatingSystem = new Models.Software.OperatingSystem()
                {
                    Architecture = OperatingSystemArchitecture.Arm64,
                    Platform = OperatingSystemPlatform.Linux,
                    Version = 1.2
                },
                Firmware = new Firmware()
                {
                    Version = 1.0,
                    Vendor = "YourFirmware"
                },
                FileSystem = new FileSystem(),
                Peripheral = new Peripheral()
                {
                    Name = "Usb",
                    InUse = false
                },
                Applications = new()
                {
                    new DummyApplication("Youtube", 2.1, true),
                    new DummyApplication("Waze", 3.1, true)
                }
            },
            new()
            {
                OperatingSystem = new Models.Software.OperatingSystem()
                {
                    Architecture = OperatingSystemArchitecture.x32,
                    Platform = OperatingSystemPlatform.Windows,
                    Version = 7
                },
                Firmware = new Firmware()
                {
                    Version = 2.0,
                    Vendor = "MutimediaFirmware"
                },
                FileSystem = new FileSystem(),
                Peripheral = new Peripheral()
                {
                    Name = "Usb",
                    InUse = false
                },
                Applications = new()
                {
                    new DummyApplication("Youtube", 3.4, true),
                    new DummyApplication("Gps", 1.1, false)
                }
            }
        };

        public static Multimedia Next()
        {
            // Deepcopy
            Multimedia multimedia = JsonConvert.DeserializeObject<Multimedia>(
                JsonConvert.SerializeObject(MultimediasDataset.ElementAt(Rand.Next(0, MultimediasDataset.Count - 1))));

            return multimedia;
        }
    }
}
