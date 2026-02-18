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

        // Linked secrets - always show the selection (user can skip)
        AnsiConsole.MarkupLine("\n[bold]Link Secrets/ConfigMaps[/]");
        var linkedSecrets = await SelectSecretsAsync(cluster, @namespace);

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
            var k8sSecrets = await AnsiConsole.Status()
                .StartAsync("Loading secrets...", async _ =>
                    await _kubeRepository.ListSecretsAsync(cluster, @namespace));

            var configMaps = await AnsiConsole.Status()
                .StartAsync("Loading configmaps...", async _ =>
                    await _kubeRepository.ListConfigMapsAsync(cluster, @namespace));

            // Filter to only those with data
            var secretsWithData = k8sSecrets.Where(s => s.Data.Any()).ToList();
            var configMapsWithData = configMaps.Where(c => c.Data.Any()).ToList();

            if (!secretsWithData.Any() && !configMapsWithData.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No secrets or configmaps with data found in this namespace.[/]");
                return secrets;
            }

            AnsiConsole.MarkupLine($"[dim]Found {secretsWithData.Count} secret(s) and {configMapsWithData.Count} configmap(s) with data.[/]");
            AnsiConsole.MarkupLine("[dim]Select secrets/configmaps to link to this forward, or skip to continue without.[/]\n");

            // Build selection items with index for reliable lookup
            var secretItems = secretsWithData.Select((s, i) => new { Index = i, Type = "Secret", Resource = s, Display = $"Secret: {s.Name} ({s.Data.Count} keys)" }).ToList();
            var configMapItems = configMapsWithData.Select((c, i) => new { Index = i, Type = "ConfigMap", Resource = c, Display = $"ConfigMap: {c.Name} ({c.Data.Count} keys)" }).ToList();

            while (true)
            {
                var doneLabel = secrets.Any() 
                    ? $"✓ Done - {secrets.Count} item(s) linked" 
                    : "Skip - no secrets/configmaps";
                
                var choices = new List<string> { doneLabel };
                choices.AddRange(secretItems.Select(s => s.Display));
                choices.AddRange(configMapItems.Select(c => c.Display));

                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select secret/configmap to link:")
                        .PageSize(15)
                        .AddChoices(choices)
                );

                if (selection.StartsWith("✓ Done") || selection.StartsWith("Skip"))
                    break;

                // Find the selected item
                var selectedSecret = secretItems.FirstOrDefault(s => s.Display == selection);
                var selectedConfigMap = configMapItems.FirstOrDefault(c => c.Display == selection);

                if (selectedSecret != null)
                {
                    var secret = selectedSecret.Resource;
                    var keyChoices = new List<string> { ">> All keys <<" };
                    keyChoices.AddRange(secret.Data.Keys);

                    var keySelection = AnsiConsole.Prompt(
                        new MultiSelectionPrompt<string>()
                            .Title($"Select key(s) from {secret.Name}:")
                            .PageSize(15)
                            .AddChoices(keyChoices)
                    );

                    var keysToAdd = keySelection.Contains(">> All keys <<")
                        ? secret.Data.Keys.ToList()
                        : keySelection.Where(k => k != ">> All keys <<").ToList();

                    foreach (var key in keysToAdd)
                    {
                        if (!secrets.Any(s => s.ResourceName == secret.Name && s.Key == key))
                        {
                            secrets.Add(new SecretReference
                            {
                                SourceType = SecretSourceType.Secret,
                                ResourceName = secret.Name,
                                Namespace = @namespace,
                                Key = key,
                                Name = $"{secret.Name}/{key}"
                            });
                            AnsiConsole.MarkupLine($"[green]✓[/] Added secret: {secret.Name.EscapeMarkup()}/{key.EscapeMarkup()}");
                        }
                    }
                }
                else if (selectedConfigMap != null)
                {
                    var configMap = selectedConfigMap.Resource;
                    var keyChoices = new List<string> { ">> All keys <<" };
                    keyChoices.AddRange(configMap.Data.Keys);

                    var keySelection = AnsiConsole.Prompt(
                        new MultiSelectionPrompt<string>()
                            .Title($"Select key(s) from {configMap.Name}:")
                            .PageSize(15)
                            .AddChoices(keyChoices)
                    );

                    var keysToAdd = keySelection.Contains(">> All keys <<")
                        ? configMap.Data.Keys.ToList()
                        : keySelection.Where(k => k != ">> All keys <<").ToList();

                    foreach (var key in keysToAdd)
                    {
                        if (!secrets.Any(s => s.ResourceName == configMap.Name && s.Key == key))
                        {
                            secrets.Add(new SecretReference
                            {
                                SourceType = SecretSourceType.ConfigMap,
                                ResourceName = configMap.Name,
                                Namespace = @namespace,
                                Key = key,
                                Name = $"{configMap.Name}/{key}"
                            });
                            AnsiConsole.MarkupLine($"[green]✓[/] Added configmap: {configMap.Name.EscapeMarkup()}/{key.EscapeMarkup()}");
                        }
                    }
                }

                if (secrets.Any())
                {
                    AnsiConsole.MarkupLine($"\n[dim]Currently linked: {secrets.Count} item(s)[/]");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[yellow]Could not load secrets: " + ex.Message.EscapeMarkup() + "[/]");
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

