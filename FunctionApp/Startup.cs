using AutomotiveWorld;
using AutomotiveWorld.AzureClients;
using AutomotiveWorld.Builders;
using AutomotiveWorld.DataAccess;
using AutomotiveWorld.Network;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

[assembly: FunctionsStartup(typeof(Startup))]
namespace AutomotiveWorld
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<AzureLogAnalyticsClient>(serviceProvider =>
            {
                HttpClient httpClient = serviceProvider.GetService<HttpClient>();

                return new AzureLogAnalyticsClient(
                    workspaceId: Environment.GetEnvironmentVariable("LOG_ANALYTICS_WORKSPACE_ID", EnvironmentVariableTarget.Process),
                    sharedKey: Environment.GetEnvironmentVariable("LOG_ANALYTICS_WORKSPACE_PRIMARY_KEY", EnvironmentVariableTarget.Process),
                    httpClient: httpClient);
            });
            builder.Services.AddSingleton<MicrosoftSentinelClient>(serviceProvider =>
            {
                ILogger<MicrosoftSentinelClient> log = serviceProvider.GetService<ILogger<MicrosoftSentinelClient>>();

                return new MicrosoftSentinelClient(
                    log,
                    workspaceResourceId: Environment.GetEnvironmentVariable("LOG_ANALYTICS_WORKSPACE_RESOURCE_ID", EnvironmentVariableTarget.Process),
                    tableName: Environment.GetEnvironmentVariable("LOG_ANALYTICS_WORKSPACE_TABLE_NAME", EnvironmentVariableTarget.Process));
            });
            builder.Services.AddSingleton<VinGenerator>();
            builder.Services.AddSingleton<VehicleRepository>();
        }
    }
}
