using KonciergeUI.Cli.Infrastructure;
using KonciergeUI.Core.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace KonciergeUI.Cli.Commands;

public class ClusterListCommand : AsyncCommand
{
    private readonly IClusterDiscoveryService _clusterDiscovery;

    public ClusterListCommand(IClusterDiscoveryService clusterDiscovery)
    {
        _clusterDiscovery = clusterDiscovery;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var clusters = await AnsiConsole.Status()
            .StartAsync("Discovering clusters...", async ctx =>
            {
                return await _clusterDiscovery.DiscoverClustersAsync();
            });

        if (clusters.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No clusters found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Name[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Context[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Server[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Namespace[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Current[/]").Centered());

        foreach (var cluster in clusters)
        {
            var currentMarker = cluster.IsCurrentContext ? "[green]●[/]" : "";
            table.AddRow(
                $"[cyan]{cluster.Name.EscapeMarkup()}[/]",
                cluster.ContextName.EscapeMarkup(),
                (cluster.ClusterUrl ?? "-").EscapeMarkup(),
                cluster.DefaultNamespace ?? "default",
                currentMarker
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {clusters.Count} cluster(s)[/]");

        return 0;
    }
}

