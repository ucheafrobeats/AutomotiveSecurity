using AutomotiveWorld.Models.Telemetry;
using AutomotiveWorld.Network;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace AutomotiveWorld.Entities
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class EntityBase
    {
        [JsonIgnore]
        protected ILogger Logger { get; }

        [JsonIgnore]
        protected AzureLogAnalyticsClient AzureLogAnalyticsClient;

        [JsonIgnore]
        protected static readonly Random Rand = new();

        [JsonProperty("id")]
        public string Id { get; set; }

        public EntityBase(
            ILogger logger,
            AzureLogAnalyticsClient azureLogAnalyticsClient)
        {
            Logger = logger;
            AzureLogAnalyticsClient = azureLogAnalyticsClient;
        }

        public Task Delete()
        {
            Entity.Current.DeleteState();

            return Task.CompletedTask;
        }

        public async Task SendTelemetry(CustomLogTelemetry customLogTelemetry = null)
        {
            customLogTelemetry ??= new()
            {
                EntityId = Id,
                JsonAsString = JsonConvert.SerializeObject(this),
                Type = GetType().Name
            };

            string telemetry = JsonConvert.SerializeObject(customLogTelemetry);
            string tableName = Environment.GetEnvironmentVariable("LOG_ANALYTICS_WORKSPACE_TABLE_NAME", EnvironmentVariableTarget.Process);
            await AzureLogAnalyticsClient.Post(tableName, telemetry);
        }
    }
}
