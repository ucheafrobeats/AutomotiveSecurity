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

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tachograph")]
        public int Tachograph { get; set; } = 0;

        [JsonProperty("restTime")]
        public TimeSpan RestTime { get; set; } = TimeSpan.FromMinutes(RestTimeInMinutes);

        [JsonProperty("assignment")]
        public Assignment Assignment { get; set; }

        public Driver(
            ILogger<Driver> logger,
            AzureLogAnalyticsClient azureLogAnalyticsClient,
            EntitiesRepository entitiesRepository,
            IDurableEntityClient durableEntityClient) : base(
                logger,
                azureLogAnalyticsClient)
        {
            EntitiesRepository = entitiesRepository;
            DurableEntityClient = durableEntityClient;
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

            Entity.Current.SignalEntity<IDriver>(Id, Assignment.ScheduledTime, e => e.StartDriving());

            return Task.CompletedTask;
        }

        public Task<bool> StartDriving()
        {
            if (Assignment is null)
            {
                Logger.LogWarning($"Driver has no assignment");
                return Task.FromResult(false);
            }

            IsAvailable = false;

            EntityId vehicleEntityId = new(nameof(Vehicle), Assignment.VehicleDto.Id);
            Entity.Current.SignalEntity<IVehicle>(vehicleEntityId, e => e.Start());

            Entity.Current.SignalEntity<IDriver>(Id, e => e.Driving());
            return Task.FromResult(true);
        }

        public Task Driving()
        {
            if (Assignment is null)
            {
                Logger.LogWarning($"Driver has no assignment");
                return Task.CompletedTask;
            }

            if (Assignment.CurrentDistance > Assignment.TotalKilometers)
            {
                Logger.LogInformation($"Finished assignment, driverId=[{Assignment.DriverDto.Id}], vehicleId=[{Assignment.VehicleDto.Id}]");
                return Task.CompletedTask;
            }

            VehicleDto vehicleDto = Assignment.VehicleDto;
            EntityId vehicleEntityId = new(nameof(Vehicle), Assignment.VehicleDto.Id);

            Engine engine = (vehicleDto.Parts[VehiclePartType.Engine] as JObject).ToObject<Engine>();

            // Calculate new Kilometer
            double distance = Math.Ceiling(engine.Displacement + engine.Cylinders);
            Assignment.CurrentDistance += distance;
            Entity.Current.SignalEntity<IVehicle>(vehicleEntityId, e => e.AddDistance(distance));

            // Calculate new trip time
            double tripOffset = engine.Cylinders != 0 ? engine.Displacement / engine.Cylinders : engine.Displacement;
            TimeSpan tripTimeSpan = TimeSpan.FromSeconds(tripOffset);

            Entity.Current.SignalEntity<IDriver>(Id, DateTime.UtcNow + tripTimeSpan, e => e.Driving());

            return Task.CompletedTask;
        }

        //public async Task AssignCar()
        //{
        //    VehicleDto vehicleDto = await VehicleRepository.GetAvailableVehicle(DurableEntityClient);

        //    var sourceEntity = Entity.Current.EntityId;
        //    //var vehicleEntity = new EntityId(nameof(Vehicle), vehicleDto.Vin);

        //    VehicleVin = vehicleDto.Vin;
        //    Entity.Current.SignalEntity<IVehicle>(vehicleDto.Vin, e => e.SetDriverId(Id));
        //    Entity.Current.SignalEntity<IDriver>(Id, NextTripTime, e => e.AssignCar());
        //}

        //public async Task ScheduleNextTrip()
        //{
        //    try
        //    {
        //        CurrentTripTime = NextTripTime;
        //        NextTripTime = CurrentTripTime.AddMinutes(2);

        //        Logger.LogInformation($"Scheduled next trip, key=[{Entity.Current.EntityKey}], datetime=[{NextTripTime}]");
        //        Entity.Current.SignalEntity<IVehicle>(Entity.Current.EntityKey, NextTripTime, proxy => proxy.Trip());
        //    }
        //    catch (Exception ex)
        //    {
        //        // Ensure to log an error when eternal loop is breaking
        //        Logger.LogError($"Failed to schedule next trip, error=[{ex}]");

        //        await Delete();
        //    }
        //}

        public async Task Drive(int totalKilometer)
        {
            try
            {
                Tachograph += 1;
                await SendTelemetry(Id);
            }
            catch (Exception ex)
            {
                //Logger.LogError($"Exception occurred while trip, entity=[{Entity.Current.EntityKey}], error=[{ex}]");
            }
            finally
            {
                //await ScheduleNextTrip();
            }
        }

        [FunctionName(nameof(Driver))]
        public static Task Run(
            [EntityTrigger] IDurableEntityContext ctx,
            [DurableClient] IDurableEntityClient client)
        {
            return ctx.DispatchAsync<Driver>(client);
        }
    }
}
