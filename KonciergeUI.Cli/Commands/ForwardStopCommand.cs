using KonciergeUI.Core.Abstractions;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace KonciergeUI.Cli.Commands;

public class ForwardStopSettings : CommandSettings
{
    [CommandArgument(0, "[ID]")]
    [Description("Template ID to stop (shows interactive picker if not provided)")]
    public string? TemplateId { get; set; }

    [CommandOption("-a|--all")]
    [Description("Stop all running forwards")]
    public bool StopAll { get; set; }
}

public class ForwardStopCommand : AsyncCommand<ForwardStopSettings>
{
    private readonly IPortForwardingService _portForwardingService;

    public ForwardStopCommand(IPortForwardingService portForwardingService)
    {
        _portForwardingService = portForwardingService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ForwardStopSettings settings)
    {
        var runningTemplates = _portForwardingService.GetAllRunningTemplates();

        if (!runningTemplates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No active port forwards to stop.[/]");
            return 0;
        }

        if (settings.StopAll)
        {
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("Stopping all forwards...", maxValue: runningTemplates.Count);
                    
                    foreach (var template in runningTemplates)
                    {
                        await _portForwardingService.StopTemplateAsync(template.TemplateId);
                        task.Increment(1);
                    }
                });

            AnsiConsole.MarkupLine($"[green]✓[/] Stopped {runningTemplates.Count} template(s)");
            return 0;
        }

        Guid templateId;

        if (!string.IsNullOrEmpty(settings.TemplateId))
        {
            if (!Guid.TryParse(settings.TemplateId, out templateId))
            {
                // Try to match by name
                var byName = runningTemplates.FirstOrDefault(t =>
                    t.Definition.Name.Contains(settings.TemplateId, StringComparison.OrdinalIgnoreCase));
                
                if (byName == null)
                {
                    AnsiConsole.MarkupLine($"[red]Template '{settings.TemplateId.EscapeMarkup()}' not found.[/]");
                    return 1;
                }
                templateId = byName.TemplateId;
            }
        }
        else
        {
            // Interactive selection
            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a template to [red]stop[/]:")
                    .PageSize(10)
                    .AddChoices(runningTemplates.Select(t =>
                        $"{t.Definition.Name} ({t.Forwards.Count} forwards) - {t.ClusterInfo.Name}"
                    ))
            );

            var selectedName = selection.Split(" (")[0];
            var selected = runningTemplates.First(t => t.Definition.Name == selectedName);
            templateId = selected.TemplateId;
        }

        var template = await _portForwardingService.GetRunningTemplateAsync(templateId);
        if (template == null)
        {
            AnsiConsole.MarkupLine("[red]Template not found or already stopped.[/]");
            return 1;
        }

        await AnsiConsole.Status()
            .StartAsync($"Stopping [cyan]{template.Definition.Name}[/]...", async ctx =>
            {
                await _portForwardingService.StopTemplateAsync(templateId);
            });

        AnsiConsole.MarkupLine($"[green]✓[/] Stopped: {template.Definition.Name}");
        return 0;
    }
}

