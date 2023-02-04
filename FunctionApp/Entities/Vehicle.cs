using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using AutomotiveWorld.Network;
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

        public Vehicle(
            ILogger<Vehicle> logger,
            AzureLogAnalyticsClient azureLogAnalyticsClient) : base(
                logger,
                azureLogAnalyticsClient)
        {
        }

        public string Vin
        {
            get
            {
                return Id;
            }
        }

        public object this[VehiclePartType key]
        {
            get { return Parts.TryGetValue(key, out object value) ? value : null; }
            set { Parts[key] = value; }
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

            return Task.CompletedTask;
        }

        public async Task AddDistance(double kilometers)
        {
            try
            {
                Kilometers += kilometers;

                await SendTelemetry(Vin);
            }
            catch (Exception ex)
            {
                //Logger.LogError($"Exception occurred while trip, entity=[{Entity.Current.EntityKey}], error=[{ex}]");
            }
            finally
            {
            }
        }

        public Task Start()
        {
            IsAvailable = false;

            return Task.CompletedTask;
        }

        public Task TurnOff()
        {
            IsAvailable = true;

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
