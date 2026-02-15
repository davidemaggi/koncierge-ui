using KonciergeUI.Cli.Helpers;
using KonciergeUI.Cli.Infrastructure;
using KonciergeUI.Core.Abstractions;
using KonciergeUI.Data;
using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models.Kube;
using KonciergeUI.Models.Security;
using Spectre.Console;
using Spectre.Console.Cli;
using static KonciergeUI.Models.Forwarding.Enums;

namespace KonciergeUI.Cli.Commands;

public class TemplateCreateCommand : AsyncCommand
{
    private readonly IPreferencesStorage _storage;
    private readonly IKubeRepository _kubeRepository;
    private readonly IClusterDiscoveryService _clusterDiscovery;
    private readonly CliState _state;

    public TemplateCreateCommand(
        IPreferencesStorage storage,
        IKubeRepository kubeRepository,
        IClusterDiscoveryService clusterDiscovery,
        CliState state)
    {
        _storage = storage;
        _kubeRepository = kubeRepository;
        _clusterDiscovery = clusterDiscovery;
        _state = state;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        AnsiConsole.Write(new FigletText("New Template").Color(Color.Cyan1));

        // Basic info
        var name = AnsiConsole.Prompt(
            new TextPrompt<string>("Template [cyan]name[/]:")
                .Validate(n => !string.IsNullOrWhiteSpace(n) ? ValidationResult.Success() : ValidationResult.Error("Name is required"))
        );

        var description = AnsiConsole.Prompt(
            new TextPrompt<string>("Description (optional):")
                .AllowEmpty()
        );

        var icon = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select an [cyan]icon[/]:")
                .AddChoices("🚀", "🔧", "💻", "🌐", "📦", "⚙️", "🔌", "🎯", "🔥", "💾")
        );

        var tagsInput = AnsiConsole.Prompt(
            new TextPrompt<string>("Tags (comma separated, optional):")
                .AllowEmpty()
        );
        var tags = string.IsNullOrWhiteSpace(tagsInput)
            ? new List<string>()
            : tagsInput.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

        var favorite = AnsiConsole.Confirm("Mark as favorite?", false);

        // Create template
        var template = new ForwardTemplate
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            Icon = icon,
            Tags = tags.Any() ? tags : null,
            Favorite = favorite,
            Forwards = new List<PortForwardDefinition>()
        };

        // Add forwards
        AnsiConsole.MarkupLine("\n[bold]Now let's add port forwards to your template.[/]");

        var cluster = await GetClusterAsync();
        if (cluster == null)
        {
            AnsiConsole.MarkupLine("[yellow]No cluster available. Template will be saved without forwards.[/]");
        }
        else
        {
            while (AnsiConsole.Confirm("\nAdd a port forward?", true))
            {
                var forward = await CreateForwardDefinitionAsync(cluster);
                if (forward != null)
                {
                    template.Forwards.Add(forward);
                    AnsiConsole.MarkupLine($"[green]✓[/] Added forward: {forward.Name}");
                }
            }
        }

        // Save template
        await _storage.AddForwardTemplateAsync(template);

        AnsiConsole.MarkupLine($"\n[green]✓[/] Template [cyan]{template.Name}[/] created with {template.Forwards.Count} forward(s)");
        AnsiConsole.MarkupLine($"[dim]ID: {template.Id}[/]");

        return 0;
    }

    private async Task<PortForwardDefinition?> CreateForwardDefinitionAsync(ClusterConnectionInfo cluster)
    {
        // Namespace selection
        var @namespace = await PromptHelpers.SelectNamespaceRequiredAsync(
            _kubeRepository,
            cluster,
            defaultNamespace: cluster.DefaultNamespace);

        // Resource type
        var resourceType = AnsiConsole.Prompt(
            new SelectionPrompt<ResourceType>()
                .Title("Resource type:")
                .AddChoices(Enum.GetValues<ResourceType>())
        );

        // Select resource
        string resourceName;
        List<int> availablePorts;

        try
        {
            if (resourceType == ResourceType.Pod)
            {
                var pods = await AnsiConsole.Status()
                    .StartAsync("Loading pods...", async ctx =>
                        await _kubeRepository.ListPodsAsync(cluster, @namespace));

                var podsWithPorts = pods.Where(p => p.Ports.Any()).ToList();

                if (!podsWithPorts.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No pods with ports found in this namespace.[/]");
                    return null;
                }

                var selectedPod = AnsiConsole.Prompt(
                    new SelectionPrompt<PodInfo>()
                        .Title("Select a [cyan]pod[/]:")
                        .PageSize(15)
                        .UseConverter(p => $"{p.Name.EscapeMarkup()} [[{p.Status}]] - Ports: {string.Join(", ", p.Ports.Select(pt => pt.Port))}")
                        .AddChoices(podsWithPorts)
                );

                resourceName = selectedPod.Name;
                availablePorts = selectedPod.Ports.Select(p => p.Port).ToList();
            }
            else
            {
                var services = await AnsiConsole.Status()
                    .StartAsync("Loading services...", async ctx =>
                        await _kubeRepository.ListServicesAsync(cluster, @namespace));

                var servicesWithPorts = services.Where(s => s.Ports.Any()).ToList();

                if (!servicesWithPorts.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No services with ports found in this namespace.[/]");
                    return null;
                }

                var selectedService = AnsiConsole.Prompt(
                    new SelectionPrompt<ServiceInfo>()
                        .Title("Select a [cyan]service[/]:")
                        .PageSize(15)
                        .UseConverter(s => $"{s.Name.EscapeMarkup()} [[{s.Type}]] - Ports: {string.Join(", ", s.Ports.Select(p => $"{p.Port}:{p.TargetPort}"))}")
                        .AddChoices(servicesWithPorts)
                );

                resourceName = selectedService.Name;
                availablePorts = selectedService.Ports.Select(p => p.Port).ToList();
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error loading resources: {ex.Message.EscapeMarkup()}[/]");
            return null;
        }

        // Port selection
        int targetPort;
        if (availablePorts.Count == 1)
        {
            targetPort = availablePorts[0];
        }
        else
        {
            targetPort = AnsiConsole.Prompt(
                new SelectionPrompt<int>()
                    .Title("Select target [cyan]port[/]:")
                    .AddChoices(availablePorts)
            );
        }

        var localPort = AnsiConsole.Prompt(
            new TextPrompt<int>("Local port (0 for auto):")
                .DefaultValue(targetPort)
        );

        // Protocol
        var protocol = AnsiConsole.Prompt(
            new SelectionPrompt<ForwardProtocol>()
                .Title("Protocol:")
                .AddChoices(Enum.GetValues<ForwardProtocol>())
        );

        // Display name
        var forwardName = AnsiConsole.Prompt(
            new TextPrompt<string>("Forward display name:")
                .DefaultValue($"{resourceName}:{targetPort}")
        );

        // Linked secrets
        var linkedSecrets = new List<SecretReference>();
        if (AnsiConsole.Confirm("Link secrets/configmaps?", false))
        {
            linkedSecrets = await SelectSecretsAsync(cluster, @namespace);
        }

        return new PortForwardDefinition
        {
            Id = Guid.CreateVersion7(),
            Name = forwardName,
            ResourceType = resourceType,
            ResourceName = resourceName,
            Namespace = @namespace,
            TargetPort = targetPort,
            LocalPort = localPort,
            Protocol = protocol,
            LinkedSecrets = linkedSecrets
        };
    }

    private async Task<List<SecretReference>> SelectSecretsAsync(ClusterConnectionInfo cluster, string @namespace)
    {
        var secrets = new List<SecretReference>();

        try
        {
            var k8sSecrets = await _kubeRepository.ListSecretsAsync(cluster, @namespace);
            var configMaps = await _kubeRepository.ListConfigMapsAsync(cluster, @namespace);

            while (true)
            {
                var choices = new List<string> { "[Done]" };
                choices.AddRange(k8sSecrets.Select(s => $"Secret: {s.Name}"));
                choices.AddRange(configMaps.Select(c => $"ConfigMap: {c.Name}"));

                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select secret/configmap:")
                        .PageSize(15)
                        .AddChoices(choices)
                );

                if (selection == "[Done]")
                    break;

                if (selection.StartsWith("Secret:"))
                {
                    var secretName = selection.Replace("Secret: ", "");
                    var secret = k8sSecrets.First(s => s.Name == secretName);

                    var key = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title($"Select key from [cyan]{secretName}[/]:")
                            .AddChoices(secret.Data.Keys)
                    );

                    secrets.Add(new SecretReference
                    {
                        SourceType = SecretSourceType.Secret,
                        ResourceName = secretName,
                        Namespace = @namespace,
                        Key = key,
                        Name = $"{secretName}/{key}"
                    });

                    AnsiConsole.MarkupLine($"[green]✓[/] Added: {secretName}/{key}");
                }
                else if (selection.StartsWith("ConfigMap:"))
                {
                    var configMapName = selection.Replace("ConfigMap: ", "");
                    var configMap = configMaps.First(c => c.Name == configMapName);

                    var key = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title($"Select key from [cyan]{configMapName}[/]:")
                            .AddChoices(configMap.Data.Keys)
                    );

                    secrets.Add(new SecretReference
                    {
                        SourceType = SecretSourceType.ConfigMap,
                        ResourceName = configMapName,
                        Namespace = @namespace,
                        Key = key,
                        Name = $"{configMapName}/{key}"
                    });

                    AnsiConsole.MarkupLine($"[green]✓[/] Added: {configMapName}/{key}");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Could not load secrets: {ex.Message.EscapeMarkup()}[/]");
        }

        return secrets;
    }

    private async Task<ClusterConnectionInfo?> GetClusterAsync()
    {
        if (_state.SelectedCluster != null)
            return _state.SelectedCluster;

        var clusters = await _clusterDiscovery.DiscoverClustersAsync();
        if (clusters.Count == 0)
            return null;

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [cyan]cluster[/] to configure forwards:")
                .AddChoices(clusters.Select(c => $"{c.Name} ({c.ContextName})"))
        );

        var selectedName = selection.Split(" (")[0];
        var selected = clusters.First(c => c.Name == selectedName);
        _state.SelectedCluster = selected;
        return selected;
    }
}

