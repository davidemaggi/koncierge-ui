using KonciergeUI.Core.Abstractions;
using KonciergeUI.Data;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace KonciergeUI.Cli.Commands;

public class TemplateStopSettings : CommandSettings
{
    [CommandArgument(0, "[TEMPLATE]")]
    [Description("Template name or ID to stop")]
    public string? TemplateName { get; set; }

    [CommandOption("-a|--all")]
    [Description("Stop all running templates")]
    public bool StopAll { get; set; }
}

public class TemplateStopCommand : AsyncCommand<TemplateStopSettings>
{
    private readonly IPortForwardingService _portForwardingService;
    private readonly IPreferencesStorage _storage;

    public TemplateStopCommand(IPortForwardingService portForwardingService, IPreferencesStorage storage)
    {
        _portForwardingService = portForwardingService;
        _storage = storage;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, TemplateStopSettings settings)
    {
        var runningTemplates = _portForwardingService.GetAllRunningTemplates();

        if (!runningTemplates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No templates currently running.[/]");
            return 0;
        }

        if (settings.StopAll)
        {
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("Stopping all templates...", maxValue: runningTemplates.Count);
                    
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

        if (!string.IsNullOrEmpty(settings.TemplateName))
        {
            // Try by ID first
            if (Guid.TryParse(settings.TemplateName, out templateId))
            {
                if (!runningTemplates.Any(t => t.TemplateId == templateId))
                {
                    AnsiConsole.MarkupLine($"[red]Template with ID '{settings.TemplateName.EscapeMarkup()}' is not running.[/]");
                    return 1;
                }
            }
            else
            {
                // Try by name
                var byName = runningTemplates.FirstOrDefault(t =>
                    t.Definition.Name.Contains(settings.TemplateName, StringComparison.OrdinalIgnoreCase));
                
                if (byName == null)
                {
                    AnsiConsole.MarkupLine($"[red]Template '{settings.TemplateName.EscapeMarkup()}' is not running.[/]");
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
                    .PageSize(15)
                    .AddChoices(runningTemplates.Select(t =>
                        $"{t.Definition.Icon ?? "📦"} {t.Definition.Name} on {t.ClusterInfo.Name} ({t.Forwards.Count} forwards)"
                    ))
            );

            var selectedName = selection.Split(" on ")[0].TrimStart();
            // Remove icon if present
            if (selectedName.Length > 2 && char.IsHighSurrogate(selectedName[0]))
                selectedName = selectedName[2..].TrimStart();
            
            var selected = runningTemplates.First(t => t.Definition.Name == selectedName || 
                selection.Contains(t.Definition.Name));
            templateId = selected.TemplateId;
        }

        var template = await _portForwardingService.GetRunningTemplateAsync(templateId);
        if (template == null)
        {
            AnsiConsole.MarkupLine("[red]Template not found or already stopped.[/]");
            return 1;
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Stopping [cyan]{template.Definition.Name}[/]...", async ctx =>
            {
                await _portForwardingService.StopTemplateAsync(templateId);
            });

        AnsiConsole.MarkupLine($"[green]✓[/] Stopped: {template.Definition.Name}");
        return 0;
    }
}

