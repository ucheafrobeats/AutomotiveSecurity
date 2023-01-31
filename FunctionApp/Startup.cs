using AutomotiveWorld;
using AutomotiveWorld.Network;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
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
            builder.Services.AddSingleton<VinGenerator>();
        }
    }
}
