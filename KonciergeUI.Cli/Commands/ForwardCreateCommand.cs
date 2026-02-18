using KonciergeUI.Cli.Helpers;
using KonciergeUI.Cli.Infrastructure;
using KonciergeUI.Core.Abstractions;
using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models.Kube;
using KonciergeUI.Models.Security;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using static KonciergeUI.Models.Forwarding.Enums;

namespace KonciergeUI.Cli.Commands;

public class ForwardCreateSettings : CommandSettings
{
    [CommandOption("-c|--cluster <CLUSTER>")]
    [Description("Cluster name (uses selected cluster if not specified)")]
    public string? Cluster { get; set; }

    [CommandOption("-n|--namespace <NAMESPACE>")]
    [Description("Namespace (interactive selection if not specified)")]
    public string? Namespace { get; set; }

    [CommandOption("-r|--resource <RESOURCE>")]
    [Description("Resource name (pod or service)")]
    public string? ResourceName { get; set; }

    [CommandOption("-t|--type <TYPE>")]
    [Description("Resource type: pod or service")]
    public string? ResourceType { get; set; }

    [CommandOption("-p|--port <PORT>")]
    [Description("Target port")]
    public int? TargetPort { get; set; }

    [CommandOption("-l|--local-port <LOCAL_PORT>")]
    [Description("Local port (0 for auto-assign)")]
    public int? LocalPort { get; set; }
}

public class ForwardCreateCommand : AsyncCommand<ForwardCreateSettings>
{
    private readonly IKubeRepository _kubeRepository;
    private readonly IClusterDiscoveryService _clusterDiscovery;
    private readonly IPortForwardingService _portForwardingService;
    private readonly CliState _state;

    public ForwardCreateCommand(
        IKubeRepository kubeRepository,
        IClusterDiscoveryService clusterDiscovery,
        IPortForwardingService portForwardingService,
        CliState state)
    {
        _kubeRepository = kubeRepository;
        _clusterDiscovery = clusterDiscovery;
        _portForwardingService = portForwardingService;
        _state = state;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ForwardCreateSettings settings)
    {
        var cluster = await GetClusterAsync(settings.Cluster);
        if (cluster == null) return 1;

        // Determine namespace (required for forward)
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
            // Interactive namespace selection (required)
            @namespace = await PromptHelpers.SelectNamespaceRequiredAsync(
                _kubeRepository,
                cluster,
                defaultNamespace: cluster.DefaultNamespace);
        }

        // Determine resource type
        ResourceType resourceType;
        if (!string.IsNullOrEmpty(settings.ResourceType))
        {
            resourceType = settings.ResourceType.ToLower() switch
            {
                "pod" => Enums.ResourceType.Pod,
                "service" => Enums.ResourceType.Service,
                "svc" => Enums.ResourceType.Service,
                _ => throw new ArgumentException($"Invalid resource type: {settings.ResourceType}")
            };
        }
        else
        {
            resourceType = AnsiConsole.Prompt(
                new SelectionPrompt<ResourceType>()
                    .Title("Select resource type:")
                    .AddChoices(Enum.GetValues<ResourceType>())
            );
        }

        // Select resource
        string resourceName;
        List<int> availablePorts;

        if (resourceType == Enums.ResourceType.Pod)
        {
            var pods = await _kubeRepository.ListPodsAsync(cluster, @namespace);
            var podsWithPorts = pods.Where(p => p.Ports.Any()).ToList();

            if (!podsWithPorts.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No pods with exposed ports found.[/]");
                return 1;
            }

            if (!string.IsNullOrEmpty(settings.ResourceName))
            {
                var pod = podsWithPorts.FirstOrDefault(p => p.Name.Equals(settings.ResourceName, StringComparison.OrdinalIgnoreCase));
                if (pod == null)
                {
                    AnsiConsole.MarkupLine($"[red]Pod '{settings.ResourceName.EscapeMarkup()}' not found or has no ports.[/]");
                    return 1;
                }
                resourceName = pod.Name;
                availablePorts = pod.Ports.Select(p => p.Port).ToList();
            }
            else
            {
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
        }
        else
        {
            var services = await _kubeRepository.ListServicesAsync(cluster, @namespace);
            var servicesWithPorts = services.Where(s => s.Ports.Any()).ToList();

            if (!servicesWithPorts.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No services with ports found.[/]");
                return 1;
            }

            if (!string.IsNullOrEmpty(settings.ResourceName))
            {
                var svc = servicesWithPorts.FirstOrDefault(s => s.Name.Equals(settings.ResourceName, StringComparison.OrdinalIgnoreCase));
                if (svc == null)
                {
                    AnsiConsole.MarkupLine($"[red]Service '{settings.ResourceName.EscapeMarkup()}' not found or has no ports.[/]");
                    return 1;
                }
                resourceName = svc.Name;
                availablePorts = svc.Ports.Select(p => p.Port).ToList();
            }
            else
            {
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

        // Select port
        int targetPort;
        if (settings.TargetPort.HasValue)
        {
            targetPort = settings.TargetPort.Value;
        }
        else if (availablePorts.Count == 1)
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

        // Local port
        var localPort = settings.LocalPort ?? AnsiConsole.Prompt(
            new TextPrompt<int>("Enter local port (0 for auto):")
                .DefaultValue(targetPort)
        );

        // Linked secrets - always show the selection (user can skip)
        AnsiConsole.MarkupLine("\n[bold]Link Secrets/ConfigMaps[/]");
        var linkedSecrets = await SelectSecretsAsync(cluster, @namespace);

        // Create forward definition
        var forwardDef = new PortForwardDefinition
        {
            Id = Guid.CreateVersion7(),
            Name = $"{resourceName}:{targetPort}",
            ResourceType = resourceType,
            ResourceName = resourceName,
            Namespace = @namespace,
            TargetPort = targetPort,
            LocalPort = localPort,
            Protocol = ForwardProtocol.Tcp,
            LinkedSecrets = linkedSecrets
        };

        // Create template and start
        var template = new ForwardTemplate
        {
            Id = Guid.CreateVersion7(),
            Name = $"Quick Forward: {resourceName}",
            Forwards = new List<PortForwardDefinition> { forwardDef }
        };

        try
        {
            var running = await AnsiConsole.Status()
                .StartAsync("Starting port forward...", async ctx =>
                {
                    return await _portForwardingService.StartTemplateAsync(template, cluster);
                });

            var forward = running.Forwards.First();
            AnsiConsole.MarkupLine($"\n[green]✓[/] Port forward started!");
            AnsiConsole.MarkupLine($"  [cyan]Local address:[/] {forward.LocalAddress ?? $"localhost:{forward.BoundLocalPort}"}");

            if (forward.ResolvedSecrets.Any())
            {
                AnsiConsole.MarkupLine("\n[bold]Linked secrets/configmaps:[/]");
                foreach (var secret in forward.ResolvedSecrets)
                {
                    var maskedValue = secret.IsSensitive ? "****" : secret.Value;
                    AnsiConsole.MarkupLine($"  • {secret.Reference.Name ?? secret.Reference.Key}: [dim]{maskedValue}[/]");
                }
            }

            AnsiConsole.MarkupLine($"\n[dim]Use 'koncierge forward stop {template.Id}' to stop[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to start forward: {ex.Message.EscapeMarkup()}[/]");
            return 1;
        }

        return 0;
    }

    private async Task<List<SecretReference>> SelectSecretsAsync(ClusterConnectionInfo cluster, string @namespace)
    {
        var secrets = new List<SecretReference>();

        try
        {
            var k8sSecrets = await AnsiConsole.Status()
                .StartAsync("Loading secrets...", async ctx =>
                    await _kubeRepository.ListSecretsAsync(cluster, @namespace));

            var configMaps = await AnsiConsole.Status()
                .StartAsync("Loading configmaps...", async ctx =>
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
            AnsiConsole.MarkupLine("[yellow]Warning: Could not load secrets/configmaps: " + ex.Message.EscapeMarkup() + "[/]");
        }

        return secrets;
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

