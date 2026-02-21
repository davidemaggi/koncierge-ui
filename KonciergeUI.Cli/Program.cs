using KonciergeUI.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using System.Text;

namespace KonciergeUI.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (OperatingSystem.IsWindows())
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }

        var services = ServiceConfiguration.ConfigureServices();
        var registrar = new TypeRegistrar(services);

        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName("koncierge");
            config.SetApplicationVersion(VersionInfo.DisplayVersion);

            config.AddCommand<Commands.InfoCommand>("info")
                .WithDescription("Show application information")
                .WithAlias("about");

            config.AddCommand<Commands.InteractiveCommand>("interactive")
                .WithDescription("Start interactive mode with full menu navigation")
                .WithAlias("i");

            config.AddBranch("cluster", cluster =>
            {
                cluster.SetDescription("Cluster management commands");

                cluster.AddCommand<Commands.ClusterListCommand>("list")
                    .WithDescription("List all available Kubernetes clusters")
                    .WithAlias("ls");

                cluster.AddCommand<Commands.ClusterSelectCommand>("select")
                    .WithDescription("Select a cluster to work with")
                    .WithAlias("s");
            });

            config.AddBranch("pods", pods =>
            {
                pods.SetDescription("Pod management commands");

                pods.AddCommand<Commands.PodsListCommand>("list")
                    .WithDescription("List pods in the selected cluster")
                    .WithAlias("ls");
            });

            config.AddBranch("services", services =>
            {
                services.SetDescription("Service management commands");

                services.AddCommand<Commands.ServicesListCommand>("list")
                    .WithDescription("List services in the selected cluster")
                    .WithAlias("ls");
            });

            config.AddBranch("forward", forward =>
            {
                forward.SetDescription("Port forwarding commands");

                forward.AddCommand<Commands.ForwardCreateCommand>("create")
                    .WithDescription("Create a new port forward")
                    .WithAlias("c");

                forward.AddCommand<Commands.ForwardListCommand>("list")
                    .WithDescription("List active port forwards")
                    .WithAlias("ls");

                forward.AddCommand<Commands.ForwardStopCommand>("stop")
                    .WithDescription("Stop a port forward")
                    .WithAlias("s");
            });

            config.AddBranch("template", template =>
            {
                template.SetDescription("Template management commands");

                template.AddCommand<Commands.TemplateListCommand>("list")
                    .WithDescription("List saved templates")
                    .WithAlias("ls");

                template.AddCommand<Commands.TemplateRunCommand>("run")
                    .WithDescription("Run a template")
                    .WithAlias("r");

                template.AddCommand<Commands.TemplateStopCommand>("stop")
                    .WithDescription("Stop a running template")
                    .WithAlias("s");

                template.AddCommand<Commands.TemplateCreateCommand>("create")
                    .WithDescription("Create a new template interactively")
                    .WithAlias("c");
            });

            config.AddBranch("secrets", secrets =>
            {
                secrets.SetDescription("Secrets and ConfigMaps commands");

                secrets.AddCommand<Commands.SecretsListCommand>("list")
                    .WithDescription("List secrets and configmaps")
                    .WithAlias("ls");
            });
        });

        return await app.RunAsync(args);
    }
}