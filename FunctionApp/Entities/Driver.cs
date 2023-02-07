using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using AutomotiveWorld.Network;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace AutomotiveWorld.Entities
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Driver : EntityBase, IDriver
    {
        public const int RestTimeInMinutes = 5;

        [JsonIgnore]
        protected EntitiesRepository EntitiesRepository { get; }

        private readonly IDurableEntityClient DurableEntityClient;

        private readonly IDurableOrchestrationClient DurableOrchestrationClient;

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tachograph")]
        public double Tachograph { get; set; } = 0;

        [JsonProperty("restTime")]
        public TimeSpan RestTime { get; set; } = TimeSpan.FromMinutes(RestTimeInMinutes);

        [JsonProperty("assignment")]
        public Assignment Assignment { get; set; }

        [JsonProperty("isAvailable")]
        public bool _isAvailable { get { return Assignment is null; } }

        public Driver(
            ILogger<Driver> logger,
            AzureLogAnalyticsClient azureLogAnalyticsClient,
            EntitiesRepository entitiesRepository,
            IDurableEntityClient durableEntityClient,
            IDurableOrchestrationClient durableOrchestrationClient) : base(
                logger,
                azureLogAnalyticsClient)
        {
            EntitiesRepository = entitiesRepository;
            DurableEntityClient = durableEntityClient;
            DurableOrchestrationClient = durableOrchestrationClient;
        }

        public Task<bool> IsAvailable()
        {
            return Task.FromResult(_isAvailable);
        }

        public Task Create(DriverDto driverDto)
        {
            Id = driverDto.Id;
            Name = driverDto.Name;

            return Task.CompletedTask;
        }

        public Task Assign(Assignment assignment)
        {
            Assignment = assignment;

            Logger.LogInformation($"Driver has been assigned, assignmentId=[{assignment.Id}], driverId=[{assignment.DriverDto.Id}], vehicleId=[{assignment.VehicleDto.Id}]");
            return Task.CompletedTask;
        }

        public Task<bool> StartDriving()
        {
            if (Assignment is null)
            {
                Logger.LogWarning($"Driver has no assignment, id=[{Id}]");
                return Task.FromResult(false);
            }

            EntityId vehicleEntityId = new(nameof(Vehicle), Assignment.VehicleDto.Id);
            Entity.Current.SignalEntity<IVehicle>(vehicleEntityId, e => e.StartEngine());

            Entity.Current.SignalEntity<IDriver>(Id, e => e.Driving());
            return Task.FromResult(true);
        }

        public async Task<bool> Driving()
        {
            if (Assignment is null)
            {
                Logger.LogWarning($"Driver has no assignment, Id=[{Id}]");
                Entity.Current.SignalEntity<IDriver>(Id, e => e.StopDriving());
                return false;
            }

            VehicleDto vehicleDto = Assignment.VehicleDto;

            if (!vehicleDto.TryGetPart(VehiclePartType.Engine, out Engine engine))
            {
                Logger.LogCritical("Vehicle has no engine");
            }

            // Calculate new Kilometer
            double distance = Math.Ceiling(engine.Displacement + engine.Cylinders);
            if (Assignment.CurrentDistance + distance > Assignment.TotalKilometers)
            {
                distance -= Assignment.CurrentDistance + distance - Assignment.TotalKilometers;
            }
            Assignment.CurrentDistance += distance;
            Tachograph += distance;
            Entity.Current.SignalEntity<IVehicle>(vehicleDto.Id, e => e.UpdateTrip(distance));
            Logger.LogInformation($"Assignment status, id=[{Assignment.Id}], driverId=[{Assignment.DriverDto.Id}], vehicleId=[{Assignment.VehicleDto.Id}], [{Assignment.CurrentDistance}/{Assignment.TotalKilometers}]");

            await SendTelemetry();

            if (Assignment.CurrentDistance == Assignment.TotalKilometers)
            {
                Logger.LogInformation($"Finalizing assignment id=[{Assignment.Id}], driverId=[{Assignment.DriverDto.Id}], vehicleId=[{Assignment.VehicleDto.Id}]");
                Entity.Current.SignalEntity<IDriver>(Id, e => e.StopDriving());
                return false;
            }

            // Calculate new trip time
            double tripOffset = engine.Cylinders != 0 ? engine.Displacement / engine.Cylinders : engine.Displacement;
            TimeSpan tripTimeSpan = TimeSpan.FromSeconds(tripOffset);

            Entity.Current.SignalEntity<IDriver>(Id, DateTime.UtcNow + tripTimeSpan, e => e.Driving());

            return true;
        }

        public Task StopDriving()
        {
            if (Assignment is null)
            {
                Logger.LogWarning($"Driver has no assignment, Id=[{Id}]");
                return Task.CompletedTask;
            }

            Logger.LogInformation($"Stop driving, driverId=[{Assignment.DriverDto.Id}], vehicleId=[{Assignment.VehicleDto.Id}]");

            Entity.Current.SignalEntity<IVehicle>(Assignment.VehicleDto.Id, e => e.TurnOffEngine());
            Entity.Current.SignalEntity<IDriver>(Id, e => e.Unassign());
            return Task.CompletedTask;
        }

        public Task Unassign()
        {
            if (Assignment is null)
            {
                Logger.LogWarning($"Driver has no assignment, Id=[{Id}]");
                return Task.CompletedTask;
            }

            Logger.LogInformation($"Driver unassign, driverId=[{Assignment.DriverDto.Id}], vehicleId=[{Assignment.VehicleDto.Id}]");
            Assignment = null;
            return Task.CompletedTask;
        }

        [FunctionName(nameof(Driver))]
        public static Task Run(
            [EntityTrigger] IDurableEntityContext context,
            [DurableClient] IDurableEntityClient durableEntityClient,
            [DurableClient] IDurableOrchestrationClient durableOrchestrationClient)
        {
            return context.DispatchAsync<Driver>(durableEntityClient, durableOrchestrationClient);
        }
    }
}
