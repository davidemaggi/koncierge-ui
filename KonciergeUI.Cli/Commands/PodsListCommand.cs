using KonciergeUI.Cli.Helpers;
using KonciergeUI.Cli.Infrastructure;
using KonciergeUI.Core.Abstractions;
using KonciergeUI.Models.Kube;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace KonciergeUI.Cli.Commands;

public class PodsListSettings : CommandSettings
{
    [CommandOption("-n|--namespace <NAMESPACE>")]
    [Description("Filter by namespace (interactive selection if not specified)")]
    public string? Namespace { get; set; }

    [CommandOption("-c|--cluster <CLUSTER>")]
    [Description("Cluster name (uses selected cluster if not specified)")]
    public string? Cluster { get; set; }
    
    [CommandOption("-a|--all")]
    [Description("Show pods from all namespaces without prompting")]
    public bool AllNamespaces { get; set; }
}

public class PodsListCommand : AsyncCommand<PodsListSettings>
{
    private readonly IKubeRepository _kubeRepository;
    private readonly IClusterDiscoveryService _clusterDiscovery;
    private readonly CliState _state;

    public PodsListCommand(IKubeRepository kubeRepository, IClusterDiscoveryService clusterDiscovery, CliState state)
    {
        _kubeRepository = kubeRepository;
        _clusterDiscovery = clusterDiscovery;
        _state = state;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, PodsListSettings settings)
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

        List<PodInfo> pods;
        try
        {
            pods = await AnsiConsole.Status()
                .StartAsync($"Fetching pods from [cyan]{cluster.Name.EscapeMarkup()}[/]...", async ctx =>
                {
                    return await _kubeRepository.ListPodsAsync(cluster, namespaceFilter);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error fetching pods: {ex.Message.EscapeMarkup()}[/]");
            return 1;
        }

        if (pods.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No pods found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Name[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Namespace[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Status[/]").Centered())
            .AddColumn(new TableColumn("[bold]Ports[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Restarts[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Age[/]").RightAligned());

        foreach (var pod in pods.OrderBy(p => p.Namespace).ThenBy(p => p.Name))
        {
            var statusColor = pod.Status switch
            {
                PodStatus.Running => "green",
                PodStatus.Pending => "yellow",
                PodStatus.Failed => "red",
                PodStatus.CrashLoopBackOff => "red",
                PodStatus.ImagePullBackOff => "red",
                _ => "dim"
            };

            var ports = pod.Ports.Any()
                ? string.Join(", ", pod.Ports.Select(p => $"{p.Port}/{p.Protocol}"))
                : "[dim]-[/]";

            var age = pod.StartTime.HasValue
                ? FormatAge(DateTimeOffset.UtcNow - pod.StartTime.Value)
                : "-";

            table.AddRow(
                $"[cyan]{pod.Name.EscapeMarkup()}[/]",
                pod.Namespace,
                $"[{statusColor}]{pod.Status}[/]",
                ports,
                pod.RestartCount.ToString(),
                age
            );
        }

        AnsiConsole.Write(new Panel(table)
            .Header($"[bold]Pods in {cluster.Name.EscapeMarkup()}[/]")
            .BorderColor(Color.Blue));

        AnsiConsole.MarkupLine($"\n[dim]Total: {pods.Count} pod(s)[/]");

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

        // No cluster selected, prompt user
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

