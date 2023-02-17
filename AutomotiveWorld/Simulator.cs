using AutomotiveWorld.AzureClients;
using AutomotiveWorld.Builders;
using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Entities;
using AutomotiveWorld.Models;
using AutomotiveWorld.Models.Parts;
using AutomotiveWorld.Network;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
                if (!await MicrosoftSentinelClient.AddAllDefaultRules())
                {
                    throw new Exception("Failed to create Microsoft Sentinel Rules");
                }

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

            Random Rand = new();

            SimulatorEventType simulatorEventType = (SimulatorEventType)simulatorEventTypes.GetValue(Rand.Next(simulatorEventTypes.Length));
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

            for (int i = 0; i < 5; i++)
            {
                Assignment assignment = new()
                {
                    TotalKilometers = Rand.Next(Constants.Assignment.TotalKilometerMinValue, Constants.Assignment.TotalKilometerMaxValue),
                    ScheduledTime = DateTime.UtcNow.AddMinutes(Constants.Assignment.ScheduledTimeOffsetInMinutes)
                };

                var instanceId = assignment.Id;

                await durableOrchestrationClient.StartNewAsync(nameof(FleetManagerAssignOrchestrator), instanceId, assignment);
            }
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

                // FIXME Assign must return bool to know if assignment is null TryAssign, and enqueue assignment otherwise
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
