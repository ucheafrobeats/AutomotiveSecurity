using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutomotiveWorld.Entities
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Vehicle : IVehicle
    {
        [JsonIgnore]
        protected ILogger Logger { get; }

        [JsonIgnore]
        private static Random Rand = new();

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

        [JsonProperty("kilometer")]
        public int Kilometer { get; set; }

        [JsonProperty("serialNumber", Required = Required.Always)]
        public string SerialNumber { get; set; }

        [JsonProperty("style")]
        public string Style { get; set; }

        [JsonProperty("trimLevel")]
        public string TrimLevel { get; set; }

        [JsonProperty("vin")]
        public string Vin { get; set; }

        [JsonProperty("year", Required = Required.Always)]
        public int Year { get; set; }

        [JsonProperty("currentTripTime")]
        public DateTime CurrentTripTime { get; set; }

        [JsonProperty("nextTripTime")]
        public DateTime NextTripTime { get; set; }

        public Vehicle(ILogger<Vehicle> logger)
        {
            Logger = logger;
        }

        public object this[VehiclePartType key]
        {
            get { return Parts.TryGetValue(key, out object value) ? value : null; }
            set { Parts[key] = value; }
        }

        public Task Create(VehicleDto vehicleDto)
        {
            Parts.AddRange(vehicleDto.Parts);
            Accidents.AddRange(vehicleDto.Accidents);
            Battery = vehicleDto.Battery;
            Color = vehicleDto.Color;
            Make = vehicleDto.Make;
            Model = vehicleDto.Model;
            Kilometer = vehicleDto.Kilometer;
            SerialNumber = vehicleDto.SerialNumber;
            Style = vehicleDto.Style;
            TrimLevel = vehicleDto.TrimLevel;
            Vin = vehicleDto.Vin;
            Year = vehicleDto.Year;

            NextTripTime = DateTime.UtcNow.AddSeconds(10);

            Entity.Current.SignalEntity<IVehicle>(Vin, NextTripTime, e => e.Trip());

            return Task.CompletedTask;
        }

        public Task Delete()
        {
            Entity.Current.DeleteState();

            return Task.CompletedTask;
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

        public async Task Trip()
        {
            try
            {
                Kilometer += 1;
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

        //public override string ToString()
        //{
        //    return JsonConvert.SerializeObject(this);
        //}

        //public void Show()
        //{
        //    Console.WriteLine("\n---------------------------");
        //    Console.WriteLine("Vehicle Vin: {0}", Vin);
        //    Console.WriteLine(" Frame  : {0}", this[VehiclePartType.Frame]);
        //    Console.WriteLine(" Engine : {0}", JsonConvert.SerializeObject(Parts[VehiclePartType.Engine]));
        //    Console.WriteLine(" #Wheels: {0}", JsonConvert.SerializeObject(Parts[VehiclePartType.Tires]));
        //    Console.WriteLine(" #Doors : {0}", this[VehiclePartType.Door]);
        //}

        [FunctionName(nameof(Vehicle))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
        {
            return ctx.DispatchAsync<Vehicle>();
        }
    }
}
