using KonciergeUI.Core.Abstractions;
using KonciergeUI.Core.Clusters;
using KonciergeUI.Data;
using KonciergeUI.Kube;
using KonciergeUI.Kube.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KonciergeUI.Cli.Infrastructure;

public static class ServiceConfiguration
{
    public static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning);
            builder.AddConsole();
        });

        // Data
        services.AddSingleton<IPreferencesStorage, JsonFilePreferencesStorage>();

        // Core services
        services.AddSingleton<IClusterDiscoveryService, ClusterDiscoveryService>();
        services.AddSingleton<IKubeRepository, KubeRepository>();
        services.AddSingleton<IPortForwardingService, PortForwardingService>();

        // CLI State
        services.AddSingleton<CliState>();

        return services;
    }
}

