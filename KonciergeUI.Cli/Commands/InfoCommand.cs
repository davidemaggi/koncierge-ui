using KonciergeUI.Cli.Helpers;
using KonciergeUI.Cli.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace KonciergeUI.Cli.Commands;

public sealed class InfoCommand : Command
{
    public override int Execute(CommandContext context)
    {
        AsciiLogo.Write();
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Koncierge[/]");
        AnsiConsole.MarkupLine(VersionInfo.Description);
        AnsiConsole.WriteLine();

        var table = new Table().NoBorder();
        table.AddColumn("Label");
        table.AddColumn("Value");

        table.AddRow("Version", VersionInfo.DisplayVersion);
        table.AddRow("Build", VersionInfo.BuildVersion);
        table.AddRow("Channel", VersionInfo.Channel);

        AnsiConsole.Write(table);
        return 0;
    }
}
