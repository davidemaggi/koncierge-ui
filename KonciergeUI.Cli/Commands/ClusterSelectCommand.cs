using KonciergeUI.Cli.Infrastructure;
using KonciergeUI.Core.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace KonciergeUI.Cli.Commands;

public class ClusterSelectSettings : CommandSettings
{
    [CommandArgument(0, "[NAME]")]
    [Description("Cluster name or context to select")]
    public string? ClusterName { get; set; }
}

public class ClusterSelectCommand : AsyncCommand<ClusterSelectSettings>
{
    private readonly IClusterDiscoveryService _clusterDiscovery;
    private readonly CliState _state;

    public ClusterSelectCommand(IClusterDiscoveryService clusterDiscovery, CliState state)
    {
        _clusterDiscovery = clusterDiscovery;
        _state = state;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ClusterSelectSettings settings)
    {
        var clusters = await AnsiConsole.Status()
            .StartAsync("Discovering clusters...", async ctx =>
            {
                return await _clusterDiscovery.DiscoverClustersAsync();
            });

        if (clusters.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No clusters found.[/]");
            return 1;
        }

        // If cluster name provided, try to match it
        if (!string.IsNullOrEmpty(settings.ClusterName))
        {
            var cluster = clusters.FirstOrDefault(c =>
                c.Name.Equals(settings.ClusterName, StringComparison.OrdinalIgnoreCase) ||
                c.ContextName.Equals(settings.ClusterName, StringComparison.OrdinalIgnoreCase));

            if (cluster == null)
            {
                AnsiConsole.MarkupLine($"[red]Cluster '{settings.ClusterName.EscapeMarkup()}' not found.[/]");
                return 1;
            }

            _state.SelectedCluster = cluster;
            AnsiConsole.MarkupLine($"[green]✓[/] Selected cluster: [cyan]{cluster.Name.EscapeMarkup()}[/]");
            return 0;
        }

        // Interactive selection
        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [cyan]cluster[/]:")
                .PageSize(15)
                .HighlightStyle(new Style(Color.Cyan1))
                .AddChoices(clusters.Select(c =>
                    c.IsCurrentContext
                        ? $"{c.Name.EscapeMarkup()} ({c.ContextName.EscapeMarkup()}) [[current]]"
                        : $"{c.Name.EscapeMarkup()} ({c.ContextName.EscapeMarkup()})"
                ))
        );

        var selectedName = selection.Split(" (")[0];
        var selectedCluster = clusters.First(c => c.Name == selectedName);

        _state.SelectedCluster = selectedCluster;
        AnsiConsole.MarkupLine($"\n[green]✓[/] Selected cluster: [cyan]{selectedCluster.Name.EscapeMarkup()}[/]");

        // Optionally select namespace
        if (AnsiConsole.Confirm("Would you like to set a namespace filter?", false))
        {
            _state.NamespaceFilter = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter namespace:")
                    .DefaultValue(selectedCluster.DefaultNamespace ?? "default")
            );
            AnsiConsole.MarkupLine($"[green]✓[/] Namespace filter: [cyan]{_state.NamespaceFilter.EscapeMarkup()}[/]");
        }

        return 0;
    }
}

