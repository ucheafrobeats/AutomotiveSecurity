using AutomotiveWorld.Builders;
using AutomotiveWorld.Entities;
using AutomotiveWorld.Models;
using AutomotiveWorld.Network;
using Azure.Identity;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure;
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
using Azure.ResourceManager.SecurityInsights;
using Azure.Core;
using Azure.ResourceManager.SecurityInsights.Models;
using System.Data;
using System.Collections;
using System.Reflection;
using AutomotiveWorld.AzureClients;
using AutomotiveWorld.DataAccess;
using Microsoft.AspNetCore.JsonPatch.Operations;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace AutomotiveWorld
{
    public class Simulator
    {
        public const string TimerScheduleExpression = "%SimulatorScheduleExpression%";

        private static readonly Random Rand = new();

        private const int MaxVehicles = 3;

        private const int MaxDrivers = 2;

        private const int AssignmentTotalKilometerMinValue = 50;

        private const int AssignmentTotalKilometerMaxValue = 2000;

        private const int AssignmentScheduledTimeOffsetInMinutes = 1;

        private const int VehicleMinYear = 2018;

        private readonly ILogger<Simulator> Logger;

        private readonly AzureLogAnalyticsClient AzureLogAnalyticsClient;

        private readonly MicrosoftSentinelClient MicrosoftSentinelClient;

        private readonly EntitiesRepository EntitiesRepository;

        private readonly VinGenerator VinGenerator;

        public string InstanceId { get; private set; }

        public Simulator(
            ILogger<Simulator> log,
            AzureLogAnalyticsClient azureLogAnalyticsClient,
            MicrosoftSentinelClient microsoftSentinelClient,
            EntitiesRepository vehicleRepository,
            VinGenerator vinGenerator)
        {
            InstanceId = nameof(FleetManagerOrchestrator);
            Logger = log;
            AzureLogAnalyticsClient = azureLogAnalyticsClient;
            MicrosoftSentinelClient = microsoftSentinelClient;
            EntitiesRepository = vehicleRepository;
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
                //if (!await MicrosoftSentinelClient.AddAllDefaultRules())
                //{
                //    throw new Exception("Failed to create Microsoft Sentinel Rules");
                //}

                // Check if an instance with the specified ID already exists or an existing one stopped running(completed/failed/terminated).
                var orchestratorInstance = await client.GetStatusAsync(InstanceId);
                if (orchestratorInstance == null
                    || orchestratorInstance.RuntimeStatus == OrchestrationRuntimeStatus.Completed
                    || orchestratorInstance.RuntimeStatus == OrchestrationRuntimeStatus.Failed
                    || orchestratorInstance.RuntimeStatus == OrchestrationRuntimeStatus.Terminated)
                {
                    // Trigger singleton orchestrator function
                    _ = await client.StartNewAsync(
                        orchestratorFunctionName: nameof(FleetManagerOrchestrator),
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

        //[FunctionName(nameof(Orchestrator))]
        //public async Task Orchestrator(
        //    [OrchestrationTrigger] IDurableOrchestrationContext context)
        //{
        //    await context.CallSubOrchestratorAsync(nameof(FleetManagerOrchestrator), "CompanyNameFleetManagerOrchestrator", "CompanyName");
        //}

        [FunctionName(nameof(FleetManagerOrchestrator))]
        public async Task FleetManagerOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            string companyName = context.GetInput<string>();

            if (!context.IsReplaying)
            {
                Logger.LogInformation($"{nameof(FleetManagerOrchestrator)} has started");
            }

            try
            {
                var tasks = new List<Task<int>>
                {
                    context.CallActivityAsync<int>(nameof(ActivityAcquireVehicles), null),
                    context.CallActivityAsync<int>(nameof(ActivityAcquireDrivers), null)
                };
                await Task.WhenAll(tasks);

                //Logger.LogDebug($"Calling {nameof(FleetManagerAssignSubOrchestrator)} function");
                //await context.CallSubOrchestratorAsync(nameof(FleetManagerAssignSubOrchestrator), nameof(FleetManagerAssignSubOrchestrator), null);

                Logger.LogInformation($"{nameof(FleetManagerOrchestrator)} finished successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception occurred in {nameof(FleetManagerOrchestrator)} function, error=[{ex}]");
                throw;
            }
            finally
            {
                //DateTime deadline = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(30));
                //await context.CreateTimer(deadline, CancellationToken.None);

                //context.StartNewOrchestration(nameof(Simulator.FleetManagerOrchestrator), null, "CompanyNameFleetManagerOrchestrator");
            }
        }

        [FunctionName(nameof(SimulateEventTrigger))]
        public async Task SimulateEventTrigger(
            [TimerTrigger("%SimulateEventTriggerScheduleExpression%", RunOnStartup = true)] TimerInfo timerInfo,
            [DurableClient] IDurableOrchestrationClient durableOrchestrationClient,
            [DurableClient] IDurableEntityClient durableEntityClient)
        {
            Array simulatorEventTypes = (SimulatorEventType[])Enum.GetValues(typeof(SimulatorEventType));

            SimulatorEventType simulatorEventType = (SimulatorEventType)simulatorEventTypes.GetValue(Rand.Next(simulatorEventTypes.Length));
            switch (simulatorEventType)
            {
                case SimulatorEventType.AcquireDriver:
                    await AcquireDriver(durableEntityClient);
                    break;
                case SimulatorEventType.AcquireVehicle:
                    await AcquireVehicle(durableEntityClient);
                    break;
                case SimulatorEventType.NewAssignment:
                    Assignment assignment = new()
                    {
                        TotalKilometers = Rand.Next(AssignmentTotalKilometerMinValue, AssignmentTotalKilometerMaxValue),
                        ScheduledTime = DateTime.UtcNow.AddMinutes(AssignmentScheduledTimeOffsetInMinutes)
                    };

                    var instanceId = assignment.Id;

                    await durableOrchestrationClient.StartNewAsync(nameof(FleetManagerAssignOrchestrator), instanceId, assignment);
                    break;
            }
        }

        //[FunctionName(nameof(FleetManagerEternalOrchestrationTrigger))]
        //public static async Task<HttpResponseMessage> FleetManagerEternalOrchestrationTrigger(
        //    [HttpTrigger(AuthorizationLevel.Function, "get", Route = nameof(FleetManagerEternalOrchestrationTrigger))] HttpRequestMessage request,
        //    [DurableClient] IDurableOrchestrationClient client)
        //{
        //    Assignment assignment = new()
        //    {
        //        TotalKilometers = 100,
        //        ScheduledTime = DateTime.UtcNow.AddMinutes(3)
        //    };

        //    var instanceId = assignment.Id;

        //    await client.StartNewAsync(nameof(FleetManagerAssignOrchestrator), instanceId, assignment);
        //    return client.CreateCheckStatusResponse(request, instanceId);
        //}

        [FunctionName(nameof(FleetManagerAssignOrchestrator))]
        public async Task FleetManagerAssignOrchestrator(
           [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            Assignment assignment = context.GetInput<Assignment>();

            if (!context.IsReplaying)
            {
                Logger.LogInformation($"{nameof(FleetManagerAssignOrchestrator)} started, assignmentId=[{assignment.Id}]");
            }

            DriverDto driverDto = null;
            VehicleDto vehicleDto = null;

            driverDto = await context.CallActivityAsync<DriverDto>(nameof(ActivityGetAvailableDriver), null);
            vehicleDto = await context.CallActivityAsync<VehicleDto>(nameof(ActivityGetAvailableVehicle), null);

            if (driverDto is null || vehicleDto is null)
            {
                if (!context.IsReplaying)
                {
                    string availabilityMessage = driverDto is null ? "no available Driver" : "no available Vehicle";
                    Logger.LogInformation($"Cannot assign task, {availabilityMessage}, assignmentId=[{assignment.Id}]");
                }

                DateTime dueTime = context.CurrentUtcDateTime.AddMinutes(1);
                await context.CreateTimer(dueTime, CancellationToken.None);

                context.ContinueAsNew(assignment);
                return;
            }

            EntityId driverEntityId = new(nameof(Driver), driverDto.Id);
            EntityId vehicleEntity = new(nameof(Vehicle), vehicleDto.Id);

            IDriver driverProxy = context.CreateEntityProxy<IDriver>(driverEntityId);
            IVehicle vehicleProxy = context.CreateEntityProxy<IVehicle>(vehicleEntity);

            using (await context.LockAsync(driverEntityId, vehicleEntity))
            {
                assignment.DriverDto = driverDto;
                assignment.VehicleDto = vehicleDto;

                // FIXME Assign must return bool to know if assignment is null TryAssign, and enqueue assignment otherwise
                await driverProxy.Assign(assignment);
                await vehicleProxy.Assign(assignment);
            }

            context.SignalEntity(driverEntityId, assignment.ScheduledTime, nameof(Driver.StartDriving));

            Logger.LogInformation($"{nameof(FleetManagerAssignOrchestrator)} finished successfully, assignmentId=[{assignment.Id}]");
        }

        private async Task AcquireDriver(IDurableEntityClient client)
        {
            DriverDto driverDto = DriverGenerator.GenerateDriverDto();

            await client.SignalEntityAsync<IDriver>(driverDto.Id, proxy => proxy.Create(driverDto));

            Logger.LogInformation($"Acquired driver, id=[{driverDto.Id}]");
        }

        private async Task AcquireVehicle(IDurableEntityClient client)
        {
            Vin vin = await VinGenerator.Next(VehicleMinYear, DateTime.Now.Year);
            VehicleDto vehicleDto = VehicleFactory.Create(vin);

            await client.SignalEntityAsync<IVehicle>(vin.Value, proxy => proxy.Create(vehicleDto));

            Logger.LogInformation($"Acquired vehicle, id=[{vehicleDto.Id}]");
        }

        [FunctionName(nameof(ActivityAcquireDrivers))]
        public async Task<int> ActivityAcquireDrivers(
            [ActivityTrigger] IDurableActivityContext context,
            [DurableClient] IDurableEntityClient client)
        {
            int driversCount = await EntitiesRepository.Count<Driver>(client);

            for (int i = driversCount; i < MaxDrivers; i++)
            {
                await AcquireDriver(client);
            }

            return 0;
        }

        [FunctionName(nameof(ActivityAcquireVehicles))]
        public async Task<int> ActivityAcquireVehicles(
            [ActivityTrigger] IDurableActivityContext context,
            [DurableClient] IDurableEntityClient client)
        {
            int vehiclesCount = await EntitiesRepository.Count<Vehicle>(client);

            for (int i = vehiclesCount; i < MaxVehicles; i++)
            {
                await AcquireVehicle(client);
            }

            return 0;
        }


        [FunctionName(nameof(ActivityGetAvailableVehicle))]
        public async Task<VehicleDto> ActivityGetAvailableVehicle(
            [ActivityTrigger] IDurableActivityContext context,
            [DurableClient] IDurableEntityClient client)
        {
            return await EntitiesRepository.GetFirstAvailable<Vehicle, VehicleDto>(client);
        }

        [FunctionName(nameof(ActivityGetAvailableDriver))]
        public async Task<DriverDto> ActivityGetAvailableDriver(
            [ActivityTrigger] IDurableActivityContext context,
            [DurableClient] IDurableEntityClient client)
        {
            return await EntitiesRepository.GetFirstAvailable<Driver, DriverDto>(client);
        }


        //    [FunctionName(nameof(Simulator.Run))]
        //    [OpenApiOperation(operationId: nameof(Simulator.Run), tags: new[] { "name" })]
        //    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        //    [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        //    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        //    public async Task<IActionResult> Run(
        //       [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        //    {
        //        log.LogInformation("C# HTTP trigger function processed a request.");

        //        string name = req.Query["name"];


        //        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        //        dynamic data = JsonConvert.DeserializeObject(requestBody);
        //        name = name ?? data?.name;

        //        string responseMessage = string.IsNullOrEmpty(name)
        //            ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
        //            : $"Hello, {name}. This HTTP triggered function executed successfully.";

        //        return new OkObjectResult(responseMessage);
        //    }
        //}
    }
}
