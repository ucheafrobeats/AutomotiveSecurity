using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using AutomotiveWorld.Builders;
using AutomotiveWorld.Models;
using AutomotiveWorld.Network;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.IoTSecurity.Devices;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace AutomotiveWorld
{
    public class FunctionApp
    {
        private readonly ILogger<FunctionApp> Logger;

        public FunctionApp(ILogger<FunctionApp> log, VinGenerator vinGenerator)
        {
            Logger = log;
        }

        [FunctionName(nameof(FunctionApp.GenerateData))]
        [OpenApiOperation(operationId: nameof(FunctionApp.GenerateData), tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GenerateData(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            //AzureLogAnalyticsClient azureLogAnalyticsClient = new AzureLogAnalyticsClient(
            //    Environment.GetEnvironmentVariable("LOG_ANALYTICS_WORKSPACE_ID", EnvironmentVariableTarget.Process),
            //    Environment.GetEnvironmentVariable("LOG_ANALYTICS_WORKSPACE_PRIMARY_KEY", EnvironmentVariableTarget.Process));

            // Construct and display vehicles
            //ScooterBuilder scooterBuilder = new();
            //carsFactory.Construct(scooterBuilder);
            //carsFactory.ShowVehicle();
            //Vehicle vehicle = scooterBuilder.Vehicle;




            //carsFactory.Construct(new MotorCycleBuilder());
            //carsFactory.ShowVehicle();

            //Vehicle car = CarsFactory.Generate();
            //await azureLogAnalyticsClient.Post("demoURLMonitor", "{\"status\": 200, \"url\": \"https://google.com\"}");
            //await azureLogAnalyticsClient.Post("demoURLMonitorX", "{\"status\": 200, \"url\": \"https://google.com\"}");
            //string s = JsonConvert.SerializeObject(vehicle);


            return new OkObjectResult($"Generated");
        }

        [FunctionName(nameof(FunctionApp.GenerateCode))]
        [OpenApiOperation(operationId: nameof(FunctionApp.GenerateCode), tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GenerateCode(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            string url = "https://raw.githubusercontent.com/Azure/azure-rest-api-specs/main/specification/iotsecurity/resource-manager/Microsoft.IoTSecurity/preview/2021-02-01-preview/devices.json";

            //Device device = new Device();
            CodeGenerator codeGenerator = new CodeGenerator();
            Assembly x = await codeGenerator.AssemblyFromUrlAsync(url);

            return new OkObjectResult($"Executed {nameof(FunctionApp.GenerateCode)}");
        }
    }
}

