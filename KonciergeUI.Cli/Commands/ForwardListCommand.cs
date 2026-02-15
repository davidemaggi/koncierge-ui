using KonciergeUI.Core.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using static KonciergeUI.Models.Forwarding.Enums;

namespace KonciergeUI.Cli.Commands;

public class ForwardListCommand : AsyncCommand
{
    private readonly IPortForwardingService _portForwardingService;

    public ForwardListCommand(IPortForwardingService portForwardingService)
    {
        _portForwardingService = portForwardingService;
    }

    public override Task<int> ExecuteAsync(CommandContext context)
    {
        var runningTemplates = _portForwardingService.GetAllRunningTemplates();

        if (!runningTemplates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No active port forwards.[/]");
            return Task.FromResult(0);
        }

        foreach (var template in runningTemplates)
        {
            var panel = new Panel(BuildForwardTable(template))
                .Header($"[bold cyan]{template.Definition.Name.EscapeMarkup()}[/] on [blue]{template.ClusterInfo.Name.EscapeMarkup()}[/]")
                .BorderColor(Color.Blue);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        return Task.FromResult(0);
    }

    private Table BuildForwardTable(Models.Forwarding.RunningTemplate template)
    {
        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Resource[/]")
            .AddColumn("[bold]Status[/]")
            .AddColumn("[bold]Local Address[/]")
            .AddColumn("[bold]Secrets[/]");

        foreach (var forward in template.Forwards)
        {
            var statusColor = forward.Status switch
            {
                ForwardStatus.Running => "green",
                ForwardStatus.Starting => "yellow",
                ForwardStatus.Reconnecting => "yellow",
                ForwardStatus.Failed => "red",
                _ => "dim"
            };

            var secretCount = forward.ResolvedSecrets.Count;
            var secretInfo = secretCount > 0 ? $"{secretCount} linked" : "-";

            table.AddRow(
                forward.Name,
                $"{forward.Definition.ResourceType}: {forward.Definition.ResourceName}",
                $"[{statusColor}]{forward.Status}[/]",
                forward.LocalAddress ?? $"localhost:{forward.BoundLocalPort ?? forward.Definition.LocalPort}",
                secretInfo
            );
        }

        return table;
    }
}

