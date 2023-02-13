using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Models.Parts;
using AutomotiveWorld.Models.Telemetry;
using Newtonsoft.Json;

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

            ScanMultimedia(computer, vehicleDto);
        }

        private void ScanMultimedia(Computer computer, VehicleDto vehicleDto)
        {
            if (vehicleDto.TryGetPart(VehiclePartType.Multimedia, out Multimedia multimedia))
            {
                bool fileSystemChanged = multimedia.FileSystem.Files.Count > 0;
                if (fileSystemChanged)
                {
                    string filenames = string.Join(",", multimedia.FileSystem.Files);
                    SecurityTelemetryPayload securityTelemetryPayload = new()
                    {
                        Message = $"{nameof(Multimedia)}.{nameof(FileSystem)} files added, filenames=[{filenames}]",
                        JsonAsString = JsonConvert.SerializeObject(multimedia.FileSystem),
                    };

                    CustomLogTelemetry customLogTelemetry = new()
                    {
                        EntityId = vehicleDto.Id,
                        JsonAsString = JsonConvert.SerializeObject(securityTelemetryPayload),
                        Type = AlertTelemetryType.Security.ToString(),
                        SubType = nameof(FileSystem)
                    };

                    computer.Events.Enqueue(customLogTelemetry);
                }

                if (multimedia.Peripheral.InUse)
                {
                    SecurityTelemetryPayload securityTelemetryPayload = new()
                    {
                        Message = $"{nameof(Multimedia)}.{nameof(Peripheral)}.{multimedia.Peripheral.Name} connected",
                        JsonAsString = JsonConvert.SerializeObject(multimedia.Peripheral),
                    };

                    CustomLogTelemetry customLogTelemetry = new()
                    {
                        EntityId = vehicleDto.Id,
                        JsonAsString = JsonConvert.SerializeObject(securityTelemetryPayload),
                        Type = AlertTelemetryType.Security.ToString(),
                        SubType = nameof(Peripheral)
                    };

                    computer.Events.Enqueue(customLogTelemetry);
                }

                if (fileSystemChanged && multimedia.Peripheral.InUse)
                {
                    // Create Peripheral alert event
                    {
                        SecurityTelemetryPayload securityTelemetryPayload = new()
                        {
                            Message = $"{nameof(Multimedia)}.{nameof(Peripheral)}.{multimedia.Peripheral.Name} has been automatically disabled",
                            Action = $"Disabled {nameof(Multimedia)}.{nameof(Peripheral)}",
                            JsonAsString = JsonConvert.SerializeObject(multimedia.Peripheral),
                        };

                        CustomLogTelemetry customLogTelemetry = new()
                        {
                            EntityId = vehicleDto.Id,
                            JsonAsString = JsonConvert.SerializeObject(securityTelemetryPayload),
                            Type = AlertTelemetryType.Security.ToString(),
                            SubType = nameof(Peripheral)
                        };

                        computer.Events.Enqueue(customLogTelemetry);
                    }

                    // Create FileSystem alert event
                    {
                        string filenames = string.Join(",", multimedia.FileSystem.Files);

                        SecurityTelemetryPayload securityTelemetryPayload = new()
                        {
                            Message = $"{nameof(Multimedia)}.{nameof(FileSystem)} files were automatically deleted, filenames=[{filenames}]",
                            Action = $"Deleted files from {nameof(Multimedia)}.{nameof(FileSystem)}",
                            JsonAsString = JsonConvert.SerializeObject(multimedia.FileSystem),
                        };

                        CustomLogTelemetry customLogTelemetry = new()
                        {
                            EntityId = vehicleDto.Id,
                            JsonAsString = JsonConvert.SerializeObject(securityTelemetryPayload),
                            Type = AlertTelemetryType.Security.ToString(),
                            SubType = nameof(FileSystem)
                        };

                        computer.Events.Enqueue(customLogTelemetry);
                    }

                    // Mitigate incident
                    multimedia.Peripheral.InUse = false;
                    multimedia.FileSystem.Files.Clear();
                }
            }
        }
    }
}
