using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
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

        [JsonProperty("year", Required = Required.Always)]
        public int Year { get; set; }

        [JsonProperty("assignment")]
        public Assignment Assignment { get; set; }

        [JsonProperty("isAvailable")]
        public bool _isAvailable { get { return Assignment is null; } }

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

                part = (value as JObject).ToObject<T>();
                return true;

            }

            return false;
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
            Year = vehicleDto.Year;

            return Task.CompletedTask;
        }

        public Task Assign(Assignment assignment)
        {
            Assignment = assignment;

            Logger.LogInformation($"Vehicle as been assigned, assignmentId=[{assignment.Id}], driverId=[{assignment.DriverDto.Id}], vehicleId=[{assignment.VehicleDto.Id}]");
            return Task.CompletedTask;
        }

        public async Task AddDistance(double kilometers)
        {
            Kilometers += kilometers;

            await SendTelemetry(Id);
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

            Entity.Current.SignalEntity<IVehicle>(Id, e => e.Unassign());

            return Task.CompletedTask;
        }

        public Task Unassign()
        {
            if (Assignment is null)
            {
                return Task.CompletedTask;
            }

            Logger.LogInformation($"Vehicle unassign, driverId=[{Assignment.DriverDto.Id}], vehicleId=[{Assignment.VehicleDto.Id}]");
            Assignment = null;
            return Task.CompletedTask;
        }


        [FunctionName(nameof(Vehicle))]
        public static Task Run(
            [EntityTrigger] IDurableEntityContext ctx)
        {
            return ctx.DispatchAsync<Vehicle>();
        }
    }
}
