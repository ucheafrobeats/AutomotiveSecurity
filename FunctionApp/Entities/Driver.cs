using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Models;
using AutomotiveWorld.Network;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace AutomotiveWorld.Entities
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Driver : EntityBase, IDriver
    {
        [JsonIgnore]
        protected VehicleRepository VehicleRepository { get; }

        private readonly IDurableEntityClient DurableEntityClient;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tachograph")]
        public int Tachograph { get; set; } = 0;

        [JsonProperty("currentTripTime")]
        public DateTime CurrentTripTime { get; set; }

        [JsonProperty("nextTripTime")]
        public DateTime NextTripTime { get; set; }

        public Driver(
            ILogger<Driver> logger,
            AzureLogAnalyticsClient azureLogAnalyticsClient,
            VehicleRepository vehicleRepository,
            IDurableEntityClient durableEntityClient) : base(
                logger,
                azureLogAnalyticsClient)
        {
            VehicleRepository = vehicleRepository;
            DurableEntityClient = durableEntityClient;
        }

        public Task Create(DriverDto driverDto)
        {
            Id = driverDto.Id;
            Name = driverDto.Name;

            NextTripTime = DateTime.UtcNow.AddSeconds(10);

            Entity.Current.SignalEntity<IDriver>(Id, NextTripTime, e => e.AssignCar());

            return Task.CompletedTask;
        }

        public async Task AssignCar()
        {
            VehicleDto vehicleDto = await VehicleRepository.GetAvailableVehicle(DurableEntityClient);
            Entity.Current.SignalEntity<IVehicle>(vehicleDto.Vin, NextTripTime, e => e.Trip());
            Console.WriteLine("1");
        }

        public async Task ScheduleNextTrip()
        {
            try
            {
                CurrentTripTime = NextTripTime;
                NextTripTime = CurrentTripTime.AddMinutes(2);

                Logger.LogInformation($"Scheduled next trip, key=[{Entity.Current.EntityKey}], datetime=[{NextTripTime}]");
                Entity.Current.SignalEntity<IVehicle>(Entity.Current.EntityKey, NextTripTime, proxy => proxy.Trip());
            }
            catch (Exception ex)
            {
                // Ensure to log an error when eternal loop is breaking
                Logger.LogError($"Failed to schedule next trip, error=[{ex}]");

                await Delete();
            }
        }

        public async Task Drive()
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
                await ScheduleNextTrip();
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
