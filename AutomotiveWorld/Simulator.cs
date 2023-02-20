using AutomotiveWorld.AzureClients;
using AutomotiveWorld.Builders;
using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Entities;
using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using AutomotiveWorld.Network;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AutomotiveWorld
{
    public class Simulator
    {
        public const string TimerScheduleExpression = "%SimulatorScheduleExpression%";

        private const int MaxVehicles = 3;

        private const int MaxDrivers = 2;

        private readonly ILogger<Simulator> Logger;

        private readonly AzureLogAnalyticsClient AzureLogAnalyticsClient;

        private readonly MicrosoftSentinelClient MicrosoftSentinelClient;

        private readonly EntitiesRepository EntitiesRepository;

        private readonly VinGenerator VinGenerator;

        public Simulator(
            ILogger<Simulator> log,
            AzureLogAnalyticsClient azureLogAnalyticsClient,
            MicrosoftSentinelClient microsoftSentinelClient,
            EntitiesRepository vehicleRepository,
            VinGenerator vinGenerator)
        {
            Logger = log;
            AzureLogAnalyticsClient = azureLogAnalyticsClient;
            MicrosoftSentinelClient = microsoftSentinelClient;
            EntitiesRepository = vehicleRepository;
            VinGenerator = vinGenerator;
        }

        [FunctionName(nameof(SetupInventoryTimer))]
        public async Task SetupInventoryTimer(
            [TimerTrigger(Simulator.TimerScheduleExpression, RunOnStartup = true)] TimerInfo timerInfo,
            [DurableClient] IDurableOrchestrationClient client)
        {
            string instanceId = nameof(SetupInventoryOrchestrator);

            Logger.LogInformation($"{nameof(SetupInventoryTimer)}, instanceId=[{instanceId}] has started");

            try
            {
                if (!await MicrosoftSentinelClient.AddAllDefaultRules())
                {
                    throw new Exception("Failed to create Microsoft Sentinel Rules");
                }

                // Check if an instance with the specified ID already exists or an existing one stopped running(completed/failed/terminated).
                var orchestratorInstance = await client.GetStatusAsync(instanceId);
                if (orchestratorInstance == null
                    || orchestratorInstance.RuntimeStatus == OrchestrationRuntimeStatus.Completed
                    || orchestratorInstance.RuntimeStatus == OrchestrationRuntimeStatus.Failed
                    || orchestratorInstance.RuntimeStatus == OrchestrationRuntimeStatus.Terminated)
                {
                    // Trigger singleton orchestrator function
                    _ = await client.StartNewAsync(
                        orchestratorFunctionName: nameof(SetupInventoryOrchestrator),
                        instanceId: instanceId);
                }
                else
                {
                    Logger.LogError($"{nameof(Simulator)} is not in a runnable state {orchestratorInstance?.RuntimeStatus}");
                }

                Logger.LogInformation($"{nameof(SetupInventoryTimer)} finished successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception occurred in {nameof(SetupInventoryTimer)} function, error=[{ex}]");
                throw;
            }
        }

        [FunctionName(nameof(Simulator.SimulateEventStarter))]
        [OpenApiOperation(operationId: nameof(Simulator.SimulateEventStarter), tags: new[] { "simulate" })]
        [OpenApiParameter(name: "simulatorEventType", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **SimulatorEventType** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> SimulateEventStarter(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           [DurableClient] IDurableOrchestrationClient durableOrchestrationClient,
           [DurableClient] IDurableEntityClient durableEntityClient)
        {
            Logger.LogInformation($"{nameof(SimulateEventStarter)}, has started");

            if (!Enum.TryParse(req.Query["simulatorEventType"], out SimulatorEventType simulateEventType))
            {
                return new BadRequestObjectResult("Invalid argument");
            }

            await SimulateEvent(durableOrchestrationClient, durableEntityClient, simulateEventType);

            Logger.LogInformation($"{nameof(SimulateEventStarter)} finished successfully");

            return new OkObjectResult("Simulate event started");
        }

        [FunctionName(nameof(Simulator.UpgradeFirmwareStarter))]
        [OpenApiOperation(operationId: nameof(Simulator.UpgradeFirmwareStarter), tags: new[] { "firmware" })]
        [OpenApiParameter(name: "part", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Part** parameter")]
        [OpenApiParameter(name: "vendor", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Vendor** parameter")]
        [OpenApiParameter(name: "version", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Version** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> UpgradeFirmwareStarter(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           [DurableClient] IDurableOrchestrationClient client,
           ILogger log)
        {
            Logger.LogInformation($"{nameof(UpgradeFirmwareStarter)}, has started");

            string vendor = req.Query["vendor"];
            double version = Convert.ToDouble(req.Query["version"]);

            if (!Enum.TryParse(req.Query["part"], out VehiclePartType part) || string.IsNullOrEmpty(vendor) || version == 0)
            {
                return new BadRequestObjectResult("Invalid argument");
            }

            string instanceId = nameof(UpgradeFirmwareOrchestrator);

            // Check if an instance with the specified ID already exists or an existing one stopped running(completed/failed/terminated).
            var orchestratorInstance = await client.GetStatusAsync(instanceId);
            if (orchestratorInstance == null
                || orchestratorInstance.RuntimeStatus == OrchestrationRuntimeStatus.Completed
                || orchestratorInstance.RuntimeStatus == OrchestrationRuntimeStatus.Failed
                || orchestratorInstance.RuntimeStatus == OrchestrationRuntimeStatus.Terminated)
            {
                // Trigger singleton orchestrator function
                _ = await client.StartNewAsync(
                    orchestratorFunctionName: nameof(UpgradeFirmwareOrchestrator),
                    instanceId: instanceId,
                    input: (part, vendor, version));
            }
            else
            {
                Logger.LogError($"{nameof(Simulator)} is not in a runnable state {orchestratorInstance?.RuntimeStatus}");
            }

            Logger.LogInformation($"{nameof(UpgradeFirmwareStarter)} finished successfully");

            return new OkObjectResult("Upgrade started");
        }

        [FunctionName(nameof(UpgradeFirmwareOrchestrator))]
        public async Task UpgradeFirmwareOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            (VehiclePartType part, string vendor, double version) = context.GetInput<(VehiclePartType, string, double)>();

            if (!context.IsReplaying)
            {
                Logger.LogInformation($"{nameof(UpgradeFirmwareOrchestrator)} has started");
            }

            try
            {
                // Simulate upgrade version
                version += VersionGenerator.Next();

                int updated = await context.CallActivityAsync<int>(nameof(ActivityUpgradeFirmware), (part, vendor, version));

                Logger.LogInformation($"{nameof(UpgradeFirmwareOrchestrator)} finished successfully, updated=[{updated}]");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception occurred in {nameof(UpgradeFirmwareOrchestrator)} function, error=[{ex}]");
                throw;
            }
        }


        [FunctionName(nameof(SetupInventoryOrchestrator))]
        public async Task SetupInventoryOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            if (!context.IsReplaying)
            {
                Logger.LogInformation($"{nameof(SetupInventoryOrchestrator)} has started");
            }

            try
            {
                var tasks = new List<Task<int>>
                {
                    context.CallActivityAsync<int>(nameof(ActivityAcquireVehicles), null),
                    context.CallActivityAsync<int>(nameof(ActivityAcquireDrivers), null)
                };
                await Task.WhenAll(tasks);

                Logger.LogInformation($"{nameof(SetupInventoryOrchestrator)} finished successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception occurred in {nameof(SetupInventoryOrchestrator)} function, error=[{ex}]");
                throw;
            }
        }

        private async Task SimulateEvent(IDurableOrchestrationClient durableOrchestrationClient, IDurableEntityClient durableEntityClient, SimulatorEventType simulatorEventType = SimulatorEventType.Unknown)
        {
            Random Rand = new();

            if (simulatorEventType == SimulatorEventType.Unknown)
            {
                Array simulatorEventTypes = (SimulatorEventType[])Enum.GetValues(typeof(SimulatorEventType));

                simulatorEventType = (SimulatorEventType)simulatorEventTypes.GetValue(Rand.Next(simulatorEventTypes.Length));
            }

            Logger.LogInformation($"{nameof(SimulateEvent)} type=[{simulatorEventType}]");

            switch (simulatorEventType)
            {
                case SimulatorEventType.AcquireDriver:
                    await AcquireDriver(durableEntityClient);
                    break;
                case SimulatorEventType.AcquireVehicle:
                    await AcquireVehicle(durableEntityClient);
                    break;
                case SimulatorEventType.AcquireElectricVehicle:
                    await AcquireVehicle(durableEntityClient, true);
                    break;
                case SimulatorEventType.NewAssignment:
                    Assignment assignment = new()
                    {
                        TotalKilometers = Rand.Next(Constants.Assignment.TotalKilometerMinValue, Constants.Assignment.TotalKilometerMaxValue),
                        ScheduledTime = DateTime.UtcNow.AddMinutes(Constants.Assignment.ScheduledTimeOffsetInMinutes)
                    };

                    var instanceId = assignment.Id;

                    await durableOrchestrationClient.StartNewAsync(nameof(FleetManagerAssignOrchestrator), instanceId, assignment);
                    break;
                case SimulatorEventType.MultimediaExploit:
                    VehicleDto vehicleDto = await EntitiesRepository.GetFirst<Vehicle, VehicleDto>(durableEntityClient, EntitiesRepository.PredicateHasMultimediaAndAvailable);

                    if (vehicleDto == null)
                    {
                        Logger.LogInformation($"Cannot simulate {nameof(SimulatorEventType.MultimediaExploit)}, VehicleId=[null]");
                        break;
                    }

                    Logger.LogInformation($"Simulate {nameof(SimulatorEventType.MultimediaExploit)}, VehicleId=[{vehicleDto.Id}]");

                    if (vehicleDto.TryGetPart(VehiclePartType.Multimedia, out Multimedia multimedia))
                    {
                        multimedia.Peripheral.Enabled = false;
                        multimedia.Peripheral.InUse = true;
                        multimedia.FileSystem.Files.Add("toolbar.exe");

                        PartDto partDto = new()
                        {
                            Type = VehiclePartType.Multimedia,
                            Part = multimedia
                        };

                        await durableEntityClient.SignalEntityAsync<IVehicle>(vehicleDto.Id, proxy => proxy.SetPart(partDto));
                    }
                    break;
            }
        }

        [FunctionName(nameof(SimulateEventTrigger))]
        public async Task SimulateEventTrigger(
            [TimerTrigger("%SimulateEventTriggerScheduleExpression%", RunOnStartup = true)] TimerInfo timerInfo,
            [DurableClient] IDurableOrchestrationClient durableOrchestrationClient,
            [DurableClient] IDurableEntityClient durableEntityClient)
        {
            await SimulateEvent(durableOrchestrationClient, durableEntityClient);
        }

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

                bool driverHasBeenAssigned = await driverProxy.Assign(assignment);
                if (driverHasBeenAssigned)
                {
                    await vehicleProxy.Assign(assignment);
                }
            }

            if (!context.IsReplaying)
            {
                Logger.LogInformation($"Assign successfully, assignmentId=[{assignment.Id}], driverId=[{assignment.DriverDto.Id}], vehicle=[{assignment.VehicleDto.Id}], scheduledTime=[{assignment.ScheduledTime}]");
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

        private async Task AcquireVehicle(IDurableEntityClient client, bool electricVehicle = false)
        {
            Vin vin = await VinGenerator.Next(Constants.Vehicle.MinYear, DateTime.Now.Year, electricVehicle);
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

        [FunctionName(nameof(ActivityUpgradeFirmware))]
        public async Task<int> ActivityUpgradeFirmware(
            [ActivityTrigger] IDurableActivityContext context,
            [DurableClient] IDurableEntityClient client)
        {
            (VehiclePartType part, string vendor, double version) = context.GetInput<(VehiclePartType, string, double)>();

            return await EntitiesRepository.UpgradeFirmware(client, part, vendor, version, pageSize: 2);
        }

        [FunctionName(nameof(ActivityGetAvailableVehicle))]
        public async Task<VehicleDto> ActivityGetAvailableVehicle(
            [ActivityTrigger] IDurableActivityContext context,
            [DurableClient] IDurableEntityClient client)
        {
            return await EntitiesRepository.GetFirst<Vehicle, VehicleDto>(client, EntitiesRepository.PredicateIsAvailable);
        }

        [FunctionName(nameof(ActivityGetAvailableDriver))]
        public async Task<DriverDto> ActivityGetAvailableDriver(
            [ActivityTrigger] IDurableActivityContext context,
            [DurableClient] IDurableEntityClient client)
        {
            return await EntitiesRepository.GetFirst<Driver, DriverDto>(client, EntitiesRepository.PredicateIsAvailable);
        }
    }
}