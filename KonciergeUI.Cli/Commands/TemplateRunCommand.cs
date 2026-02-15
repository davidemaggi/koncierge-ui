using KonciergeUI.Cli.Infrastructure;
using KonciergeUI.Core.Abstractions;
using KonciergeUI.Data;
using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models.Kube;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using static KonciergeUI.Models.Forwarding.Enums;

namespace KonciergeUI.Cli.Commands;

public class TemplateRunSettings : CommandSettings
{
    [CommandArgument(0, "[TEMPLATE]")]
    [Description("Template name or ID to run")]
    public string? TemplateName { get; set; }

    [CommandOption("-c|--cluster <CLUSTER>")]
    [Description("Cluster to run on (uses selected cluster if not specified)")]
    public string? Cluster { get; set; }

    [CommandOption("-w|--watch")]
    [Description("Watch mode - show logs and status updates")]
    public bool Watch { get; set; }
}

public class TemplateRunCommand : AsyncCommand<TemplateRunSettings>
{
    private readonly IPreferencesStorage _storage;
    private readonly IClusterDiscoveryService _clusterDiscovery;
    private readonly IPortForwardingService _portForwardingService;
    private readonly CliState _state;

    public TemplateRunCommand(
        IPreferencesStorage storage,
        IClusterDiscoveryService clusterDiscovery,
        IPortForwardingService portForwardingService,
        CliState state)
    {
        _storage = storage;
        _clusterDiscovery = clusterDiscovery;
        _portForwardingService = portForwardingService;
        _state = state;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, TemplateRunSettings settings)
    {
        var templates = await _storage.GetForwardTemplatesAsync();

        if (!templates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No saved templates.[/]");
            return 1;
        }

        // Select template
        ForwardTemplate template;
        if (!string.IsNullOrEmpty(settings.TemplateName))
        {
            // Try by ID first
            if (Guid.TryParse(settings.TemplateName, out var id))
            {
                template = templates.FirstOrDefault(t => t.Id == id)!;
            }
            else
            {
                template = templates.FirstOrDefault(t =>
                    t.Name.Contains(settings.TemplateName, StringComparison.OrdinalIgnoreCase))!;
            }

            if (template == null)
            {
                AnsiConsole.MarkupLine($"[red]Template '{settings.TemplateName.EscapeMarkup()}' not found.[/]");
                return 1;
            }
        }
        else
        {
            // Interactive selection
            template = AnsiConsole.Prompt(
                new SelectionPrompt<ForwardTemplate>()
                    .Title("Select a [cyan]template[/] to run:")
                    .PageSize(15)
                    .UseConverter(t => $"{t.Icon ?? "📦"} {t.Name} ({t.Forwards.Count} forwards)")
                    .AddChoices(templates.OrderByDescending(t => t.Favorite).ThenBy(t => t.Name))
            );
        }

        // Get cluster
        var cluster = await GetClusterAsync(settings.Cluster);
        if (cluster == null) return 1;

        // Check if template already running
        var existing = _portForwardingService.GetAllRunningTemplates()
            .FirstOrDefault(t => t.TemplateId == template.Id);

        if (existing != null)
        {
            AnsiConsole.MarkupLine($"[yellow]Template '{template.Name.EscapeMarkup()}' is already running on {existing.ClusterInfo.Name.EscapeMarkup()}[/]");
            if (!AnsiConsole.Confirm("Start another instance?", false))
                return 0;
        }

        // Start template
        RunningTemplate running;
        try
        {
            running = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Starting [cyan]{template.Name.EscapeMarkup()}[/] on [blue]{cluster.Name.EscapeMarkup()}[/]...", async ctx =>
                {
                    return await _portForwardingService.StartTemplateAsync(template, cluster);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to start template: {ex.Message.EscapeMarkup()}[/]");
            return 1;
        }

        // Show results
        DisplayRunningTemplate(running);

        // Watch mode
        if (settings.Watch)
        {
            AnsiConsole.MarkupLine("\n[dim]Watching... Press Ctrl+C to stop[/]\n");
            await WatchTemplateAsync(running);
        }

        return 0;
    }

    private void DisplayRunningTemplate(RunningTemplate running)
    {
        AnsiConsole.MarkupLine($"\n[green]✓[/] Template [cyan]{running.Definition.Name.EscapeMarkup()}[/] started on [blue]{running.ClusterInfo.Name.EscapeMarkup()}[/]\n");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Forward[/]")
            .AddColumn("[bold]Resource[/]")
            .AddColumn("[bold]Status[/]")
            .AddColumn("[bold]Local Address[/]");

        foreach (var forward in running.Forwards)
        {
            var statusColor = forward.Status switch
            {
                ForwardStatus.Running => "green",
                ForwardStatus.Starting => "yellow",
                ForwardStatus.Failed => "red",
                _ => "dim"
            };

            table.AddRow(
                forward.Name,
                $"{forward.Definition.ResourceType}: {forward.Definition.ResourceName}",
                $"[{statusColor}]{forward.Status}[/]",
                forward.LocalAddress ?? $"localhost:{forward.BoundLocalPort}"
            );
        }

        AnsiConsole.Write(table);

        // Show resolved secrets
        var allSecrets = running.Forwards.SelectMany(f => f.ResolvedSecrets).ToList();
        if (allSecrets.Any())
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule("[bold]Resolved Secrets & ConfigMaps[/]").RuleStyle("dim"));

            var secretsTable = new Table()
                .Border(TableBorder.Simple)
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Type[/]")
                .AddColumn("[bold]Value[/]");

            foreach (var secret in allSecrets)
            {
                var value = secret.IsSensitive
                    ? $"[dim]{MaskValue(secret.Value ?? "")}[/] [yellow][[click to copy]][/]"
                    : secret.Value ?? "-";

                secretsTable.AddRow(
                    secret.Reference.Name ?? secret.Reference.Key,
                    secret.Reference.SourceType.ToString(),
                    value
                );
            }

            AnsiConsole.Write(secretsTable);
        }
    }

    private async Task WatchTemplateAsync(RunningTemplate running)
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                // Refresh status
                var current = await _portForwardingService.GetRunningTemplateAsync(running.TemplateId);
                if (current == null)
                {
                    AnsiConsole.MarkupLine("\n[yellow]Template stopped.[/]");
                    break;
                }

                // Show logs for each forward
                foreach (var forward in current.Forwards)
                {
                    var logs = await _portForwardingService.GetForwardLogsAsync(forward.Id, 5);
                    if (logs.Any())
                    {
                        AnsiConsole.MarkupLine($"\n[dim]--- Logs for {forward.Name.EscapeMarkup()} ---[/]");
                        foreach (var log in logs)
                        {
                            AnsiConsole.MarkupLine($"[dim]{log.EscapeMarkup()}[/]");
                        }
                    }
                }

                await Task.Delay(2000, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("\n[dim]Watch stopped.[/]");
        }
    }

    private static string MaskValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Length <= 4) return new string('*', value.Length);
        return value[..2] + new string('*', Math.Min(value.Length - 4, 10)) + value[^2..];
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
}

