using AutomotiveWorld.Builders;
using AutomotiveWorld.Entities;
using AutomotiveWorld.Models;
using AutomotiveWorld.Network;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AutomotiveWorld
{
    public class Simulator
    {
        public const string TimerScheduleExpression = "%SimulatorScheduleExpression%";

        private const int MaxVehicles = 10;

        private const int VehicleMinYear = 2018;

        private readonly ILogger<Simulator> Logger;

        private readonly AzureLogAnalyticsClient AzureLogAnalyticsClient;

        private readonly VinGenerator VinGenerator;

        public string InstanceId { get; private set; }

        public Simulator(ILogger<Simulator> log, AzureLogAnalyticsClient azureLogAnalyticsClient, VinGenerator vinGenerator)
        {
            InstanceId = nameof(Simulator);
            Logger = log;
            AzureLogAnalyticsClient = azureLogAnalyticsClient;
            VinGenerator = vinGenerator;
        }

        [FunctionName(nameof(OrchestratorTimer))]
        public async Task OrchestratorTimer(
            [TimerTrigger(Simulator.TimerScheduleExpression, RunOnStartup = true)] TimerInfo timerInfo,
            [DurableClient] IDurableOrchestrationClient client)
        {
            Logger.LogInformation($"{nameof(OrchestratorTimer)}, instanceId=[{InstanceId}] has started");

            try
            {
                // Check if an instance with the specified ID already exists or an existing one stopped running(completed/failed/terminated).
                var orchestratorInstance = await client.GetStatusAsync(InstanceId);
                if (orchestratorInstance == null
                    || orchestratorInstance.RuntimeStatus == OrchestrationRuntimeStatus.Completed
                    || orchestratorInstance.RuntimeStatus == OrchestrationRuntimeStatus.Failed
                    || orchestratorInstance.RuntimeStatus == OrchestrationRuntimeStatus.Terminated)
                {
                    // Trigger singleton orchestrator function
                    _ = await client.StartNewAsync(
                        orchestratorFunctionName: nameof(Orchestrator),
                        instanceId: InstanceId);
                }
                else
                {
                    Logger.LogError($"{nameof(Simulator)} is not in a runnable state {orchestratorInstance?.RuntimeStatus}");
                }

                Logger.LogInformation($"{nameof(OrchestratorTimer)} finished successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception occurred in {nameof(OrchestratorTimer)} function, error=[{ex}]");
                throw;
            }
        }

        [FunctionName(nameof(Orchestrator))]
        public async Task Orchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            try
            {
                Logger.LogInformation($"{nameof(Orchestrator)} has started");

                var vehicles = await context.CallActivityAsync<ImmutableDictionary<string, Vehicle>>(
                    functionName: nameof(ActivityGetVehicles),
                    input: null);
                int vehiclesCount = vehicles.Count;

                if (vehiclesCount < MaxVehicles)
                {
                    await context.CallActivityAsync<int>(
                    functionName: nameof(ActivityRegisterVehicles),
                    input: vehiclesCount);
                }

                Logger.LogInformation($"{nameof(Orchestrator)} finished successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception occurred in {nameof(Orchestrator)} function, error=[{ex}]");
                throw;
            }
        }

        [FunctionName(nameof(ActivityRegisterVehicles))]
        public async Task<int> ActivityRegisterVehicles(
            [ActivityTrigger] int vehiclesCount,
            [DurableClient] IDurableEntityClient client)
        {
            for (int i = vehiclesCount; i < MaxVehicles; i++)
            {
                Vin vin = await VinGenerator.Next(VehicleMinYear, DateTime.Now.Year);
                VehicleDto vehicleDto = VehicleFactory.Create(vin);

                await client.SignalEntityAsync<IVehicle>(vin.Value, proxy => proxy.Create(vehicleDto));

                Logger.LogInformation($"Added {vehicleDto.Vin}");
            }

            return 0;
        }


        [FunctionName(nameof(ActivityGetVehicles))]
        public async Task<ImmutableDictionary<string, Vehicle>> ActivityGetVehicles(
        [ActivityTrigger] IDurableActivityContext context,
        [DurableClient] IDurableEntityClient client)
        {
            Dictionary<string, Vehicle> vehicles = new();

            Type type = typeof(Vehicle);
            string typeName = type.Name;

            using var source = new CancellationTokenSource();
            var query = new EntityQuery
            {
                PageSize = 100,
                FetchState = true,
                EntityName = typeName
            };

            do
            {
                // Paginate over all entities
                var result = await client.ListEntitiesAsync(query, source.Token);
                if (result?.Entities == null)
                {
                    break;
                }

                foreach (var durableEntityStatus in result.Entities)
                {
                    var entityId = durableEntityStatus.EntityId.EntityKey;

                    if (durableEntityStatus.State == null)
                    {
                        // entity state might be null for instances marked as deleted and before being purged
                        continue;
                    }

                    try
                    {
                        var exporter = durableEntityStatus.State.ToObject<Vehicle>();
                        vehicles.Add(entityId, exporter);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to deserialize object, type=[{typeName}], entityId=[{entityId}], error=[{ex}]");
                    }
                }

                query.ContinuationToken = result.ContinuationToken;
            }
            while (query.ContinuationToken != null);

            return vehicles.ToImmutableDictionary();
        }
    }
}
