using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using AutomotiveWorld.Builders;
using AutomotiveWorld.Models;
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
        private readonly ILogger<FunctionApp> _logger;

        public FunctionApp(ILogger<FunctionApp> log)
        {
            _logger = log;
        }

        [FunctionName(nameof(FunctionApp.Run))]
        [OpenApiOperation(operationId: nameof(FunctionApp.Run), tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        [FunctionName(nameof(FunctionApp.GenerateData))]
        [OpenApiOperation(operationId: nameof(FunctionApp.GenerateData), tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GenerateData(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            AzureLogAnalyticsClient azureLogAnalyticsClient = new AzureLogAnalyticsClient(
                Environment.GetEnvironmentVariable("LOG_ANALYTICS_WORKSPACE_ID", EnvironmentVariableTarget.Process),
                Environment.GetEnvironmentVariable("LOG_ANALYTICS_WORKSPACE_PRIMARY_KEY", EnvironmentVariableTarget.Process));

            CarsFactory carsFactory = new();

            // Construct and display vehicles
            //ScooterBuilder scooterBuilder = new();
            //carsFactory.Construct(scooterBuilder);
            //carsFactory.ShowVehicle();
            //Vehicle vehicle = scooterBuilder.Vehicle;


            for (int i = 0; i < 10; i++)
            {
                carsFactory.Construct(new CarBuilder("4S3BMHB68B3286050"));
                carsFactory.ShowVehicle();
            }

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

