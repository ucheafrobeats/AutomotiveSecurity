using AutomotiveWorld;
using AutomotiveWorld.Network;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;

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
                return new AzureLogAnalyticsClient(
                    Environment.GetEnvironmentVariable("LOG_ANALYTICS_WORKSPACE_ID", EnvironmentVariableTarget.Process),
                    Environment.GetEnvironmentVariable("LOG_ANALYTICS_WORKSPACE_PRIMARY_KEY", EnvironmentVariableTarget.Process));
            });
            builder.Services.AddSingleton<VinGenerator>();
        }
    }
}
