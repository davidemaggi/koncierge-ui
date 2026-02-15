using KonciergeUI.Cli.Helpers;
using KonciergeUI.Cli.Infrastructure;
using KonciergeUI.Core.Abstractions;
using KonciergeUI.Models.Kube;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace KonciergeUI.Cli.Commands;

public class ServicesListSettings : CommandSettings
{
    [CommandOption("-n|--namespace <NAMESPACE>")]
    [Description("Filter by namespace (interactive selection if not specified)")]
    public string? Namespace { get; set; }

    [CommandOption("-c|--cluster <CLUSTER>")]
    [Description("Cluster name (uses selected cluster if not specified)")]
    public string? Cluster { get; set; }
    
    [CommandOption("-a|--all")]
    [Description("Show services from all namespaces without prompting")]
    public bool AllNamespaces { get; set; }
}

public class ServicesListCommand : AsyncCommand<ServicesListSettings>
{
    private readonly IKubeRepository _kubeRepository;
    private readonly IClusterDiscoveryService _clusterDiscovery;
    private readonly CliState _state;

    public ServicesListCommand(IKubeRepository kubeRepository, IClusterDiscoveryService clusterDiscovery, CliState state)
    {
        _kubeRepository = kubeRepository;
        _clusterDiscovery = clusterDiscovery;
        _state = state;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ServicesListSettings settings)
    {
        var cluster = await GetClusterAsync(settings.Cluster);
        if (cluster == null) return 1;

        // Determine namespace filter
        string? namespaceFilter;
        if (settings.AllNamespaces)
        {
            namespaceFilter = null;
        }
        else if (!string.IsNullOrEmpty(settings.Namespace))
        {
            namespaceFilter = settings.Namespace;
        }
        else if (!string.IsNullOrEmpty(_state.NamespaceFilter))
        {
            namespaceFilter = _state.NamespaceFilter;
        }
        else
        {
            // Interactive namespace selection
            namespaceFilter = await PromptHelpers.SelectNamespaceAsync(
                _kubeRepository, 
                cluster, 
                includeAllOption: true,
                defaultNamespace: cluster.DefaultNamespace);
        }

        List<ServiceInfo> services;
        try
        {
            services = await AnsiConsole.Status()
                .StartAsync($"Fetching services from [cyan]{cluster.Name.EscapeMarkup()}[/]...", async ctx =>
                {
                    return await _kubeRepository.ListServicesAsync(cluster, namespaceFilter);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error fetching services: {ex.Message.EscapeMarkup()}[/]");
            return 1;
        }

        if (services.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No services found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Name[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Namespace[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Type[/]").Centered())
            .AddColumn(new TableColumn("[bold]Cluster IP[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Ports[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Age[/]").RightAligned());

        foreach (var svc in services.OrderBy(s => s.Namespace).ThenBy(s => s.Name))
        {
            var typeColor = svc.Type switch
            {
                ServiceType.LoadBalancer => "green",
                ServiceType.NodePort => "yellow",
                ServiceType.ClusterIP => "blue",
                _ => "dim"
            };

            var ports = svc.Ports.Any()
                ? string.Join(", ", svc.Ports.Select(p =>
                    p.NodePort.HasValue
                        ? $"{p.Port}:{p.TargetPort}/{p.Protocol} (NodePort:{p.NodePort})"
                        : $"{p.Port}:{p.TargetPort}/{p.Protocol}"))
                : "[dim]-[/]";

            var age = svc.CreatedAt.HasValue
                ? FormatAge(DateTimeOffset.UtcNow - svc.CreatedAt.Value)
                : "-";

            table.AddRow(
                $"[cyan]{svc.Name.EscapeMarkup()}[/]",
                svc.Namespace,
                $"[{typeColor}]{svc.Type}[/]",
                svc.ClusterIp ?? "-",
                ports,
                age
            );
        }

        AnsiConsole.Write(new Panel(table)
            .Header($"[bold]Services in {cluster.Name.EscapeMarkup()}[/]")
            .BorderColor(Color.Blue));

        AnsiConsole.MarkupLine($"\n[dim]Total: {services.Count} service(s)[/]");

        return 0;
    }

    private async Task<ClusterConnectionInfo?> GetClusterAsync(string? clusterName)
    {
        if (!string.IsNullOrEmpty(clusterName))
        {
            var clusters = await _clusterDiscovery.DiscoverClustersAsync();
            var cluster = clusters.FirstOrDefault(c =>
                c.Name.Equals(clusterName, StringComparison.OrdinalIgnoreCase) ||
                c.ContextName.Equals(clusterName, StringComparison.OrdinalIgnoreCase));

            if (cluster == null)
            {
                AnsiConsole.MarkupLine($"[red]Cluster '{clusterName.EscapeMarkup()}' not found.[/]");
                return null;
            }
            return cluster;
        }

        if (_state.SelectedCluster != null)
            return _state.SelectedCluster;

        var allClusters = await _clusterDiscovery.DiscoverClustersAsync();
        if (allClusters.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No clusters found.[/]");
            return null;
        }

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [cyan]cluster[/]:")
                .AddChoices(allClusters.Select(c => $"{c.Name} ({c.ContextName})"))
        );

        var selectedName = selection.Split(" (")[0];
        var selected = allClusters.First(c => c.Name == selectedName);
        _state.SelectedCluster = selected;
        return selected;
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalDays >= 1)
            return $"{(int)age.TotalDays}d";
        if (age.TotalHours >= 1)
            return $"{(int)age.TotalHours}h";
        if (age.TotalMinutes >= 1)
            return $"{(int)age.TotalMinutes}m";
        return $"{(int)age.TotalSeconds}s";
    }
}

