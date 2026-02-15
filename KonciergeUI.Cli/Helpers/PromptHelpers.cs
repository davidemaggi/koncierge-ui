using KonciergeUI.Core.Abstractions;
using KonciergeUI.Models.Kube;
using Spectre.Console;

namespace KonciergeUI.Cli.Helpers;

/// <summary>
/// Helper class for common CLI selection prompts with filtering
/// </summary>
public static class PromptHelpers
{
    /// <summary>
    /// Prompts the user to select a namespace from the available namespaces in the cluster.
    /// Includes an option to select all namespaces.
    /// </summary>
    public static async Task<string?> SelectNamespaceAsync(
        IKubeRepository kubeRepository,
        ClusterConnectionInfo cluster,
        bool includeAllOption = true,
        string? defaultNamespace = null)
    {
        List<string> namespaces;
        
        try
        {
            namespaces = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Loading namespaces...", async ctx =>
                {
                    return await kubeRepository.ListNamespacesAsync(cluster);
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not load namespaces: {ex.Message.EscapeMarkup()}[/]");
            // Fallback to text prompt
            return AnsiConsole.Prompt(
                new TextPrompt<string>("Enter namespace:")
                    .DefaultValue(defaultNamespace ?? "default")
            );
        }

        if (!namespaces.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No namespaces found.[/]");
            return defaultNamespace ?? "default";
        }

        var choices = new List<string>();
        
        if (includeAllOption)
        {
            choices.Add("(All namespaces)");
        }
        
        choices.AddRange(namespaces);

        // Determine the default selection
        string? defaultSelection = null;
        if (!string.IsNullOrEmpty(defaultNamespace) && namespaces.Contains(defaultNamespace))
        {
            defaultSelection = defaultNamespace;
        }
        else if (namespaces.Contains("default"))
        {
            defaultSelection = "default";
        }

        var prompt = new SelectionPrompt<string>()
            .Title("Select a [cyan]namespace[/]:")
            .PageSize(15)
            .MoreChoicesText("[grey](Move up and down to reveal more namespaces)[/]")
            .EnableSearch()
            .SearchPlaceholderText("[grey](Type to filter)[/]")
            .AddChoices(choices);

        var selection = AnsiConsole.Prompt(prompt);

        if (selection == "(All namespaces)")
        {
            return null; // null means all namespaces
        }

        return selection;
    }

    /// <summary>
    /// Prompts the user to select a namespace (required, no "all" option).
    /// </summary>
    public static async Task<string> SelectNamespaceRequiredAsync(
        IKubeRepository kubeRepository,
        ClusterConnectionInfo cluster,
        string? defaultNamespace = null)
    {
        var result = await SelectNamespaceAsync(kubeRepository, cluster, includeAllOption: false, defaultNamespace);
        return result ?? defaultNamespace ?? "default";
    }
}

