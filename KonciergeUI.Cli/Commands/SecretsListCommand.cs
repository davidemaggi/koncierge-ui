using KonciergeUI.Cli.Helpers;
using KonciergeUI.Cli.Infrastructure;
using KonciergeUI.Core.Abstractions;
using KonciergeUI.Models.Kube;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace KonciergeUI.Cli.Commands;

public class SecretsListSettings : CommandSettings
{
    [CommandOption("-n|--namespace <NAMESPACE>")]
    [Description("Filter by namespace (interactive selection if not specified)")]
    public string? Namespace { get; set; }

    [CommandOption("-c|--cluster <CLUSTER>")]
    [Description("Cluster name")]
    public string? Cluster { get; set; }

    [CommandOption("-t|--type <TYPE>")]
    [Description("Filter by type: secrets, configmaps, or all")]
    public string? Type { get; set; }

    [CommandOption("-s|--show-values")]
    [Description("Show secret values (masked by default)")]
    public bool ShowValues { get; set; }
}

public class SecretsListCommand : AsyncCommand<SecretsListSettings>
{
    private readonly IKubeRepository _kubeRepository;
    private readonly IClusterDiscoveryService _clusterDiscovery;
    private readonly CliState _state;

    public SecretsListCommand(IKubeRepository kubeRepository, IClusterDiscoveryService clusterDiscovery, CliState state)
    {
        _kubeRepository = kubeRepository;
        _clusterDiscovery = clusterDiscovery;
        _state = state;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SecretsListSettings settings)
    {
        var cluster = await GetClusterAsync(settings.Cluster);
        if (cluster == null) return 1;

        // Determine namespace (required for secrets/configmaps)
        string @namespace;
        if (!string.IsNullOrEmpty(settings.Namespace))
        {
            @namespace = settings.Namespace;
        }
        else if (!string.IsNullOrEmpty(_state.NamespaceFilter))
        {
            @namespace = _state.NamespaceFilter;
        }
        else
        {
            // Interactive namespace selection (required, no "all" option for secrets)
            @namespace = await PromptHelpers.SelectNamespaceRequiredAsync(
                _kubeRepository,
                cluster,
                defaultNamespace: cluster.DefaultNamespace);
        }

        var typeFilter = settings.Type?.ToLower() ?? "all";

        // Fetch data
        List<SecretInfo> secrets = new();
        List<ConfigMapInfo> configMaps = new();

        await AnsiConsole.Status()
            .StartAsync("Fetching secrets and configmaps...", async ctx =>
            {
                if (typeFilter is "all" or "secrets" or "secret")
                {
                    try
                    {
                        secrets = await _kubeRepository.ListSecretsAsync(cluster, @namespace);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning: Could not fetch secrets: {ex.Message.EscapeMarkup()}[/]");
                    }
                }

                if (typeFilter is "all" or "configmaps" or "configmap" or "cm")
                {
                    try
                    {
                        configMaps = await _kubeRepository.ListConfigMapsAsync(cluster, @namespace);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning: Could not fetch configmaps: {ex.Message.EscapeMarkup()}[/]");
                    }
                }
            });

        // Display secrets
        if (secrets.Any())
        {
            var secretsTable = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold red]Secrets[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Keys[/]")
                .AddColumn("[bold]Type[/]");

            foreach (var secret in secrets.OrderBy(s => s.Name))
            {
                var keys = secret.Data.Any()
                    ? string.Join(", ", secret.Data.Keys.Take(5)) + (secret.Data.Count > 5 ? $" (+{secret.Data.Count - 5} more)" : "")
                    : "[dim]empty[/]";

                secretsTable.AddRow(
                    $"[cyan]{secret.Name.EscapeMarkup()}[/]",
                    keys,
                    secret.Type ?? "Opaque"
                );
            }

            AnsiConsole.Write(secretsTable);
            AnsiConsole.MarkupLine($"[dim]Total secrets: {secrets.Count}[/]\n");
        }

        // Display configmaps
        if (configMaps.Any())
        {
            var cmTable = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold blue]ConfigMaps[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Keys[/]")
                .AddColumn("[bold]Data Size[/]");

            foreach (var cm in configMaps.OrderBy(c => c.Name))
            {
                var keys = cm.Data.Any()
                    ? string.Join(", ", cm.Data.Keys.Take(5)) + (cm.Data.Count > 5 ? $" (+{cm.Data.Count - 5} more)" : "")
                    : "[dim]empty[/]";

                var dataSize = cm.Data.Values.Sum(v => v?.Length ?? 0);

                cmTable.AddRow(
                    $"[cyan]{cm.Name.EscapeMarkup()}[/]",
                    keys,
                    FormatSize(dataSize)
                );
            }

            AnsiConsole.Write(cmTable);
            AnsiConsole.MarkupLine($"[dim]Total configmaps: {configMaps.Count}[/]\n");
        }

        if (!secrets.Any() && !configMaps.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No secrets or configmaps found.[/]");
        }

        // Show values if requested
        if (settings.ShowValues && (secrets.Any() || configMaps.Any()))
        {
            if (AnsiConsole.Confirm("Show detailed values?", false))
            {
                await ShowDetailedValuesAsync(secrets, configMaps);
            }
        }

        return 0;
    }

    private async Task ShowDetailedValuesAsync(List<SecretInfo> secrets, List<ConfigMapInfo> configMaps)
    {
        var allItems = new List<string>();
        allItems.AddRange(secrets.Select(s => $"Secret: {s.Name}"));
        allItems.AddRange(configMaps.Select(c => $"ConfigMap: {c.Name}"));

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select item to view:")
                .PageSize(15)
                .AddChoices(allItems)
        );

        if (selection.StartsWith("Secret:"))
        {
            var name = selection.Replace("Secret: ", "");
            var secret = secrets.First(s => s.Name == name);

            var table = new Table()
                .Border(TableBorder.Simple)
                .Title($"[bold]Secret: {name}[/]")
                .AddColumn("[bold]Key[/]")
                .AddColumn("[bold]Value (masked)[/]");

            foreach (var (key, value) in secret.Data)
            {
                var maskedValue = MaskValue(value);
                table.AddRow(key, $"[dim]{maskedValue}[/]");
            }

            AnsiConsole.Write(table);
        }
        else if (selection.StartsWith("ConfigMap:"))
        {
            var name = selection.Replace("ConfigMap: ", "");
            var cm = configMaps.First(c => c.Name == name);

            var table = new Table()
                .Border(TableBorder.Simple)
                .Title($"[bold]ConfigMap: {name}[/]")
                .AddColumn("[bold]Key[/]")
                .AddColumn("[bold]Value[/]");

            foreach (var (key, value) in cm.Data)
            {
                var displayValue = value?.Length > 100 ? value[..100] + "..." : value ?? "";
                table.AddRow(key, displayValue.EscapeMarkup());
            }

            AnsiConsole.Write(table);
        }
    }

    private static string MaskValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Length <= 4) return new string('*', value.Length);
        return value[..2] + new string('*', Math.Min(value.Length - 4, 10)) + value[^2..];
    }

    private static string FormatSize(int bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
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

