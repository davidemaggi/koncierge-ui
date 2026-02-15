using KonciergeUI.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace KonciergeUI.Cli.Commands;

public class TemplateListCommand : AsyncCommand
{
    private readonly IPreferencesStorage _storage;

    public TemplateListCommand(IPreferencesStorage storage)
    {
        _storage = storage;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var templates = await _storage.GetForwardTemplatesAsync();

        if (!templates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No saved templates.[/]");
            AnsiConsole.MarkupLine("[dim]Use 'koncierge template create' to create one.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Name[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Description[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Forwards[/]").Centered())
            .AddColumn(new TableColumn("[bold]Tags[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Favorite[/]").Centered())
            .AddColumn(new TableColumn("[bold]ID[/]").LeftAligned());

        foreach (var template in templates.OrderByDescending(t => t.Favorite).ThenBy(t => t.Sorting).ThenBy(t => t.Name))
        {
            var tags = template.Tags?.Any() == true
                ? string.Join(", ", template.Tags)
                : "[dim]-[/]";

            var favorite = template.Favorite ? "[yellow]★[/]" : "";

            table.AddRow(
                $"{template.Icon ?? "📦"} [cyan]{template.Name.EscapeMarkup()}[/]",
                (template.Description ?? "-").EscapeMarkup(),
                template.Forwards.Count.ToString(),
                tags,
                favorite,
                $"[dim]{template.Id.ToString()[..8]}...[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[dim]Total: {templates.Count} template(s)[/]");

        return 0;
    }
}

