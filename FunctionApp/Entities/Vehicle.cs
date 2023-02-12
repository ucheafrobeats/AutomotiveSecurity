using AutomotiveWorld.Builders;
using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using AutomotiveWorld.Models.Telemetry;
using AutomotiveWorld.Network;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;

namespace AutomotiveWorld.Entities
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Vehicle : EntityBase, IVehicle
    {
        [JsonProperty("parts")]
        public Dictionary<VehiclePartType, object> Parts = new();

        [JsonProperty("accidents")]
        public List<Accident> Accidents { get; private set; } = new();

        [JsonProperty("battery")]
        public Battery Battery { get; set; } = new();

        [JsonProperty("color", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public Color Color { get; set; }

        [JsonProperty("make")]
        public string Make { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("kilometers")]
        public double Kilometers { get; set; }

        [JsonProperty("serialNumber", Required = Required.Always)]
        public string SerialNumber { get; set; }

        [JsonProperty("style")]
        public string Style { get; set; }

        [JsonProperty("trimLevel")]
        public string TrimLevel { get; set; }

        [JsonProperty("vehicleType")]
        public string VehicleType { get; set; }

        [JsonProperty("year", Required = Required.Always)]
        public int Year { get; set; }

        [JsonProperty("assignment")]
        public Assignment Assignment { get; set; }

        [JsonProperty("isAvailable")]
        public bool _isAvailable { get { return Assignment is null && Status == VehicleStatus.Parking; } }

        [JsonProperty("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        public VehicleStatus Status { get; set; } = VehicleStatus.Parking;

        public Vehicle(
            ILogger<Vehicle> logger,
            AzureLogAnalyticsClient azureLogAnalyticsClient) : base(
                logger,
                azureLogAnalyticsClient)
        {
        }

        public string Vin { get { return Id; } }

        public Task<bool> IsAvailable()
        {
            return Task.FromResult(_isAvailable);
        }

        public object this[VehiclePartType key]
        {
            get { return Parts.TryGetValue(key, out object value) ? value : null; }
            set { Parts[key] = value; }
        }

        public bool TryGetPart<T>(VehiclePartType key, out T part) where T : Part
        {
            part = null;

            if (Parts.TryGetValue(key, out object value))
            {
                part ??= (value as JObject)?.ToObject<T>() ?? value as T;
                return part is not null;
            }

            return false;
        }

        public async Task SetPart(PartDto partDto)
        {
            this[partDto.Type] = partDto.Part;

            await SendTelemetry();
        }

        public Task Create(VehicleDto vehicleDto)
        {
            Id = vehicleDto.Id;
            Parts.AddRange(vehicleDto.Parts);
            Accidents.AddRange(vehicleDto.Accidents);
            Battery = vehicleDto.Battery;
            Color = vehicleDto.Color;
            Make = vehicleDto.Make;
            Model = vehicleDto.Model;
            Kilometers = vehicleDto.Kilometer;
            SerialNumber = vehicleDto.SerialNumber;
            Style = vehicleDto.Style;
            TrimLevel = vehicleDto.TrimLevel;
            VehicleType = vehicleDto.VehicleType;
            Year = vehicleDto.Year;

            return Task.CompletedTask;
        }

        public async Task<bool> Assign(Assignment assignment)
        {
            if (Assignment is not null || Status != VehicleStatus.Parking)
            {
                Logger.LogError($"Cannot assign vehicle, hasAssignment=[{Assignment is not null}], status=[{VehicleStatus.Parking}]");
                return false;
            }

            Assignment = assignment;
            Status = VehicleStatus.Assigned;

            Logger.LogInformation($"Vehicle has been assigned, assignmentId=[{assignment.Id}], driverId=[{assignment.DriverDto.Id}], vehicleId=[{assignment.VehicleDto.Id}]");

            await SendTelemetry();

            return true;
        }

        public async Task UpdateTrip(double kilometers)
        {
            Kilometers += kilometers;

            await SimulateComputer();

            await SimulateTires();

            await SendTelemetry();
        }

        private Task SimulateComputer()
        {
            if (TryGetPart(VehiclePartType.Computer, out Computer computer))
            {
                computer.NextCommand(this);
            }

            return Task.CompletedTask;
        }

        private async Task SimulateTires()
        {
            if (TryGetPart(VehiclePartType.Tires, out Tires tires))
            {
                foreach (PropertyInfo tireProperty in tires.GetType().GetProperties())
                {
                    Tire tire = (Tire)tires.GetType().GetProperty(tireProperty.Name).GetValue(tires, null);

                    if (tire is null)
                    {
                        continue;
                    }

                    if (tireProperty.Name.Equals(TireSideType.Spare.ToString()))
                    {
                        tire.Pressure -= Rand.NextDouble() < Constants.SimulatorProbability.SpareTierPressureReduction ? 1 : 0;
                    }
                    else
                    {
                        tire.Pressure -= Rand.NextDouble() < Constants.SimulatorProbability.NonSpareTierPressureReduction ? 1 : 0;
                    }

                    tire.Year -= Rand.NextDouble() < Constants.SimulatorProbability.TierYearReduction ? 1 : 0;

                    bool isFaulty = await ValidateAndAlertTire(tire);
                    if (isFaulty)
                    {
                        tire.IsFaulty = true;

                        Status = VehicleStatus.Faulty;
                    }
                }

                Parts[VehiclePartType.Tires] = tires;
            }
        }

        public Task StartEngine()
        {
            if (TryGetPart(VehiclePartType.Engine, out Engine engine))
            {
                engine.Status = EngineStatus.On;
            }

            return Task.CompletedTask;
        }

        public Task TurnOffEngine()
        {
            if (TryGetPart(VehiclePartType.Engine, out Engine engine))
            {
                engine.Status = EngineStatus.Off;
            }


            return Task.CompletedTask;
        }

        public async Task Unassign()
        {
            if (Assignment is not null)
            {
                Logger.LogInformation($"Vehicle unassign, driverId=[{Assignment.DriverDto.Id}], vehicleId=[{Assignment.VehicleDto.Id}]");
                Assignment = null;
            }

            await SendTelemetry();
        }

        public Task Maintenance()
        {
            Logger.LogInformation($"Vehicle maintenance started, vehicleId=[{Id}]");

            Status = VehicleStatus.Garage;

            Garage.Treat(this);

            TimeSpan maintenanceTimeSpan = TimeSpan.FromMinutes(Rand.Next(Constants.Vehicle.Maintenance.MaintenanceTimeInMinutesMinValue, Constants.Vehicle.Maintenance.MaintenanceTimeInMinutesMaxValue));
            Entity.Current.SignalEntity<IVehicle>(Id, DateTime.UtcNow + maintenanceTimeSpan, e => e.Park());
            Logger.LogInformation($"Vehicle maintenance done, vehicleId=[{Id}]");

            return Task.CompletedTask;
        }

        public Task Park()
        {
            if (Kilometers > Constants.Vehicle.MaxTotalKilometers)
            {
                Status = VehicleStatus.OutOfService;

            }

            switch (Status)
            {
                case VehicleStatus.Assigned:
                case VehicleStatus.Garage:
                    Status = VehicleStatus.Parking;
                    Entity.Current.SignalEntity<IVehicle>(Id, e => e.Unassign());
                    Entity.Current.SignalEntity<IVehicle>(Id, e => e.TurnOffEngine());
                    break;
                case VehicleStatus.Faulty:
                    Status = VehicleStatus.Garage;
                    Logger.LogInformation($"Vehicle require maintenance, vehicleId=[{Id}]");
                    Entity.Current.SignalEntity<IVehicle>(Id, e => e.Maintenance());
                    break;
                case VehicleStatus.OutOfService:
                    Logger.LogInformation($"Vehicle marked as out of service, vehicleId=[{Id}]");
                    Entity.Current.SignalEntity<IVehicle>(Id, e => e.Delete());
                    break;
                case VehicleStatus.Parking:
                    Status = VehicleStatus.Assigned;
                    break;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Validates Tire part and send alert telemetry if faulty
        /// </summary>
        /// <param name="tire">Tire to validate</param>
        /// <returns>true iff faulty</returns>
        private async Task<bool> ValidateAndAlertTire(Tire tire)
        {
            if (tire == null || tire.IsFaulty)
            {
                return true;
            }

            PsiSpec psiSpec = VehicleFactory.GetPsiSpec(VehicleType);

            if ((tire.Side == TireSideType.Spare && (tire.Pressure < Constants.Vehicle.Tire.SpareMinPressure || tire.Year < DateTime.Now.Year - Constants.Vehicle.Tire.SpareMinYear)) ||
                (tire.Side != TireSideType.Spare && (tire.Pressure < psiSpec.MinValue || tire.Pressure > psiSpec.MaxValue || tire.Year < DateTime.Now.Year - Constants.Vehicle.Tire.NonSpareMinYear)))
            {
                Logger.LogInformation($"Tire required maintenance, vehicleId=[{Id}], side=[{tire.Side}], year=[{tire.Year}], pressure=[{tire.Pressure}]");
                tire.IsFaulty = true;

                MaintenanceTelemetryPayload maintenanceTelemetryPayload = new()
                {
                    Tire = tire,
                    PsiSpec = psiSpec
                };

                CustomLogTelemetry customLogTelemetry = new()
                {
                    EntityId = Id,
                    JsonAsString = JsonConvert.SerializeObject(maintenanceTelemetryPayload),
                    Type = AlertTelemetryType.Maintenance.ToString(),
                    SubType = nameof(Tire)
                };

                await SendTelemetry(customLogTelemetry);

                return true;
            }

            return false;
        }


        [FunctionName(nameof(Vehicle))]
        public static Task Run(
            [EntityTrigger] IDurableEntityContext ctx)
        {
            return ctx.DispatchAsync<Vehicle>();
        }
    }
}
