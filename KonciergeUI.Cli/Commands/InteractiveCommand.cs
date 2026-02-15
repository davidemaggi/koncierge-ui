using KonciergeUI.Cli.Helpers;
using KonciergeUI.Cli.Infrastructure;
using KonciergeUI.Core.Abstractions;
using KonciergeUI.Data;
using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models.Kube;
using Spectre.Console;
using Spectre.Console.Cli;
using static KonciergeUI.Models.Forwarding.Enums;

namespace KonciergeUI.Cli.Commands;

public class InteractiveCommand : AsyncCommand
{
    private readonly IClusterDiscoveryService _clusterDiscovery;
    private readonly IKubeRepository _kubeRepository;
    private readonly IPortForwardingService _portForwardingService;
    private readonly IPreferencesStorage _storage;
    private readonly CliState _state;

    public InteractiveCommand(
        IClusterDiscoveryService clusterDiscovery,
        IKubeRepository kubeRepository,
        IPortForwardingService portForwardingService,
        IPreferencesStorage storage,
        CliState state)
    {
        _clusterDiscovery = clusterDiscovery;
        _kubeRepository = kubeRepository;
        _portForwardingService = portForwardingService;
        _storage = storage;
        _state = state;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(
            new FigletText("Koncierge")
                .Color(Color.Cyan1)
                .Centered());
        AnsiConsole.MarkupLine("[dim]Kubernetes Port Forward Manager[/]\n");

        // Auto-select cluster on startup
        await SelectClusterAsync();

        while (true)
        {
            var clusterInfo = _state.SelectedCluster != null
                ? $"[cyan]{_state.SelectedCluster.Name.EscapeMarkup()}[/]"
                : "[dim]None[/]";

            var runningCount = _portForwardingService.GetAllRunningTemplates().Count;
            var runningInfo = runningCount > 0
                ? $" [green]({runningCount} running)[/]"
                : "";

            AnsiConsole.Write(new Rule($"Cluster: {clusterInfo}{runningInfo}").RuleStyle("dim"));

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("\nWhat would you like to do?")
                    .PageSize(15)
                    .HighlightStyle(new Style(Color.Cyan1))
                    .AddChoiceGroup("[bold]Cluster[/]", 
                        "🔄 Switch Cluster",
                        "📋 List Pods",
                        "🌐 List Services",
                        "🔐 List Secrets/ConfigMaps")
                    .AddChoiceGroup("[bold]Port Forwarding[/]",
                        "▶️  Quick Forward",
                        "📊 View Active Forwards",
                        "⏹️  Stop Forward")
                    .AddChoiceGroup("[bold]Templates[/]",
                        "📁 List Templates",
                        "🚀 Run Template",
                        "⏹️  Stop Template",
                        "➕ Create Template")
                    .AddChoiceGroup("[bold]Other[/]",
                        "❌ Exit")
            );

            AnsiConsole.Clear();

            switch (choice)
            {
                case "🔄 Switch Cluster":
                    await SelectClusterAsync();
                    break;

                case "📋 List Pods":
                    await ListPodsAsync();
                    break;

                case "🌐 List Services":
                    await ListServicesAsync();
                    break;

                case "🔐 List Secrets/ConfigMaps":
                    await ListSecretsAsync();
                    break;

                case "▶️  Quick Forward":
                    await CreateQuickForwardAsync();
                    break;

                case "📊 View Active Forwards":
                    await ViewActiveForwardsAsync();
                    break;

                case "⏹️  Stop Forward":
                    await StopForwardAsync();
                    break;

                case "📁 List Templates":
                    await ListTemplatesAsync();
                    break;

                case "🚀 Run Template":
                    await RunTemplateAsync();
                    break;

                case "⏹️  Stop Template":
                    await StopTemplateAsync();
                    break;

                case "➕ Create Template":
                    await CreateTemplateAsync();
                    break;

                case "❌ Exit":
                    // Stop all forwards before exiting
                    var running = _portForwardingService.GetAllRunningTemplates();
                    if (running.Any())
                    {
                        if (AnsiConsole.Confirm($"Stop {running.Count} running forward(s) before exit?", true))
                        {
                            foreach (var template in running)
                            {
                                await _portForwardingService.StopTemplateAsync(template.TemplateId);
                            }
                        }
                    }
                    AnsiConsole.MarkupLine("[dim]Goodbye![/]");
                    return 0;
            }

            AnsiConsole.WriteLine();
            if (!AnsiConsole.Confirm("Continue?", true))
            {
                break;
            }
            AnsiConsole.Clear();
        }

        return 0;
    }

    private async Task SelectClusterAsync()
    {
        var clusters = await AnsiConsole.Status()
            .StartAsync("Discovering clusters...", async ctx =>
                await _clusterDiscovery.DiscoverClustersAsync());

        if (!clusters.Any())
        {
            AnsiConsole.MarkupLine("[red]No clusters found.[/]");
            return;
        }

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<ClusterConnectionInfo>()
                .Title("Select a [cyan]cluster[/]:")
                .PageSize(15)
                .UseConverter(c => c.IsCurrentContext
                    ? $"[green]● {c.Name.EscapeMarkup()}[/] ({c.ContextName.EscapeMarkup()}) [[current]]"
                    : $"  {c.Name.EscapeMarkup()} ({c.ContextName.EscapeMarkup()})")
                .AddChoices(clusters)
        );

        
        _state.SelectedCluster = selection;
        AnsiConsole.MarkupLine($"[green]✓[/] Selected: [cyan]{selection.Name.EscapeMarkup()}[/]");
    }

    private async Task ListPodsAsync()
    {
        if (_state.SelectedCluster == null)
        {
            AnsiConsole.MarkupLine("[yellow]Please select a cluster first.[/]");
            return;
        }

        var @namespace = await PromptHelpers.SelectNamespaceAsync(
            _kubeRepository,
            _state.SelectedCluster,
            includeAllOption: true,
            defaultNamespace: _state.SelectedCluster.DefaultNamespace);

        var pods = await AnsiConsole.Status()
            .StartAsync("Fetching pods...", async ctx =>
                await _kubeRepository.ListPodsAsync(_state.SelectedCluster, @namespace));

        if (!pods.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No pods found.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Namespace[/]")
            .AddColumn("[bold]Status[/]")
            .AddColumn("[bold]Ports[/]")
            .AddColumn("[bold]Restarts[/]");

        foreach (var pod in pods.OrderBy(p => p.Namespace).ThenBy(p => p.Name))
        {
            var statusColor = pod.Status switch
            {
                PodStatus.Running => "green",
                PodStatus.Pending => "yellow",
                PodStatus.Failed or PodStatus.CrashLoopBackOff => "red",
                _ => "dim"
            };

            var ports = pod.Ports.Any()
                ? string.Join(", ", pod.Ports.Select(p => p.Port.ToString()))
                : "-";

            table.AddRow(
                $"[cyan]{pod.Name.EscapeMarkup()}[/]",
                pod.Namespace,
                $"[{statusColor}]{pod.Status}[/]",
                ports,
                pod.RestartCount.ToString()
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[dim]Total: {pods.Count} pods[/]");
    }

    private async Task ListServicesAsync()
    {
        if (_state.SelectedCluster == null)
        {
            AnsiConsole.MarkupLine("[yellow]Please select a cluster first.[/]");
            return;
        }

        var @namespace = await PromptHelpers.SelectNamespaceAsync(
            _kubeRepository,
            _state.SelectedCluster,
            includeAllOption: true,
            defaultNamespace: _state.SelectedCluster.DefaultNamespace);

        var services = await AnsiConsole.Status()
            .StartAsync("Fetching services...", async ctx =>
                await _kubeRepository.ListServicesAsync(_state.SelectedCluster, @namespace));

        if (!services.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No services found.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Namespace[/]")
            .AddColumn("[bold]Type[/]")
            .AddColumn("[bold]Ports[/]")
            .AddColumn("[bold]Cluster IP[/]");

        foreach (var svc in services.OrderBy(s => s.Namespace).ThenBy(s => s.Name))
        {
            var ports = svc.Ports.Any()
                ? string.Join(", ", svc.Ports.Select(p => $"{p.Port}:{p.TargetPort}"))
                : "-";

            table.AddRow(
                $"[cyan]{svc.Name.EscapeMarkup()}[/]",
                svc.Namespace,
                svc.Type.ToString(),
                ports,
                svc.ClusterIp ?? "-"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[dim]Total: {services.Count} services[/]");
    }

    private async Task ListSecretsAsync()
    {
        if (_state.SelectedCluster == null)
        {
            AnsiConsole.MarkupLine("[yellow]Please select a cluster first.[/]");
            return;
        }

        var @namespace = await PromptHelpers.SelectNamespaceRequiredAsync(
            _kubeRepository,
            _state.SelectedCluster,
            defaultNamespace: _state.SelectedCluster.DefaultNamespace);

        var secrets = await AnsiConsole.Status()
            .StartAsync("Fetching secrets...", async ctx =>
                await _kubeRepository.ListSecretsAsync(_state.SelectedCluster, @namespace));

        var configMaps = await AnsiConsole.Status()
            .StartAsync("Fetching configmaps...", async ctx =>
                await _kubeRepository.ListConfigMapsAsync(_state.SelectedCluster, @namespace));

        if (secrets.Any())
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold red]Secrets[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Keys[/]");

            foreach (var s in secrets.OrderBy(s => s.Name))
            {
                var keys = s.Data.Any() ? string.Join(", ", s.Data.Keys.Take(5)) : "-";
                table.AddRow($"[cyan]{s.Name.EscapeMarkup()}[/]", keys);
            }
            AnsiConsole.Write(table);
        }

        if (configMaps.Any())
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold blue]ConfigMaps[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Keys[/]");

            foreach (var cm in configMaps.OrderBy(c => c.Name))
            {
                var keys = cm.Data.Any() ? string.Join(", ", cm.Data.Keys.Take(5)) : "-";
                table.AddRow($"[cyan]{cm.Name.EscapeMarkup()}[/]", keys);
            }
            AnsiConsole.Write(table);
        }
    }

    private async Task CreateQuickForwardAsync()
    {
        if (_state.SelectedCluster == null)
        {
            AnsiConsole.MarkupLine("[yellow]Please select a cluster first.[/]");
            return;
        }

        var resourceType = AnsiConsole.Prompt(
            new SelectionPrompt<ResourceType>()
                .Title("Resource type:")
                .AddChoices(Enum.GetValues<ResourceType>())
        );

        var @namespace = await PromptHelpers.SelectNamespaceRequiredAsync(
            _kubeRepository,
            _state.SelectedCluster,
            defaultNamespace: _state.SelectedCluster.DefaultNamespace);

        string resourceName;
        List<int> ports;

        if (resourceType == ResourceType.Pod)
        {
            var pods = await _kubeRepository.ListPodsAsync(_state.SelectedCluster, @namespace);
            var podsWithPorts = pods.Where(p => p.Ports.Any()).ToList();

            if (!podsWithPorts.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No pods with ports found.[/]");
                return;
            }

            var pod = AnsiConsole.Prompt(
                new SelectionPrompt<PodInfo>()
                    .Title("Select pod:")
                    .PageSize(15)
                    .UseConverter(p => $"{p.Name.EscapeMarkup()} [[{p.Status}]] - {string.Join(", ", p.Ports.Select(pt => pt.Port))}")
                    .AddChoices(podsWithPorts)
            );

            resourceName = pod.Name;
            ports = pod.Ports.Select(p => p.Port).ToList();
        }
        else
        {
            var services = await _kubeRepository.ListServicesAsync(_state.SelectedCluster, @namespace);
            var svcWithPorts = services.Where(s => s.Ports.Any()).ToList();

            if (!svcWithPorts.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No services with ports found.[/]");
                return;
            }

            var svc = AnsiConsole.Prompt(
                new SelectionPrompt<ServiceInfo>()
                    .Title("Select service:")
                    .PageSize(15)
                    .UseConverter(s => $"{s.Name.EscapeMarkup()} [[{s.Type}]] - {string.Join(", ", s.Ports.Select(p => $"{p.Port}:{p.TargetPort}"))}")
                    .AddChoices(svcWithPorts)
            );

            resourceName = svc.Name;
            ports = svc.Ports.Select(p => p.Port).ToList();
        }

        var targetPort = ports.Count == 1
            ? ports[0]
            : AnsiConsole.Prompt(
                new SelectionPrompt<int>()
                    .Title("Select port:")
                    .AddChoices(ports)
            );

        var localPort = AnsiConsole.Prompt(
            new TextPrompt<int>("Local port:")
                .DefaultValue(targetPort)
        );

        var forwardDef = new PortForwardDefinition
        {
            Id = Guid.CreateVersion7(),
            Name = $"{resourceName}:{targetPort}",
            ResourceType = resourceType,
            ResourceName = resourceName,
            Namespace = @namespace,
            TargetPort = targetPort,
            LocalPort = localPort
        };

        var template = new ForwardTemplate
        {
            Id = Guid.CreateVersion7(),
            Name = $"Quick: {resourceName}",
            Forwards = new List<PortForwardDefinition> { forwardDef }
        };

        try
        {
            var running = await AnsiConsole.Status()
                .StartAsync("Starting forward...", async ctx =>
                    await _portForwardingService.StartTemplateAsync(template, _state.SelectedCluster));

            var fwd = running.Forwards.First();
            AnsiConsole.MarkupLine($"[green]✓[/] Forward started: [cyan]{fwd.LocalAddress ?? $"localhost:{fwd.BoundLocalPort}"}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private Task ViewActiveForwardsAsync()
    {
        var templates = _portForwardingService.GetAllRunningTemplates();

        if (!templates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No active forwards.[/]");
            return Task.CompletedTask;
        }

        foreach (var t in templates)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title($"[bold]{t.Definition.Icon ?? "📦"} {t.Definition.Name.EscapeMarkup()}[/] on [blue]{t.ClusterInfo.Name.EscapeMarkup()}[/]")
                .AddColumn("[bold]Name[/]")
                .AddColumn("[bold]Resource[/]")
                .AddColumn("[bold]Status[/]")
                .AddColumn("[bold]Local Address[/]")
                .AddColumn("[bold]Secrets[/]");

            foreach (var f in t.Forwards)
            {
                var statusColor = f.Status switch
                {
                    ForwardStatus.Running => "green",
                    ForwardStatus.Failed => "red",
                    _ => "yellow"
                };

                table.AddRow(
                    f.Name,
                    $"{f.Definition.ResourceType}: {f.Definition.ResourceName}",
                    $"[{statusColor}]{f.Status}[/]",
                    f.LocalAddress ?? $"localhost:{f.BoundLocalPort}",
                    f.ResolvedSecrets.Count > 0 ? $"{f.ResolvedSecrets.Count} linked" : "-"
                );
            }

            AnsiConsole.Write(table);

            // Show resolved secrets
            var allSecrets = t.Forwards.SelectMany(f => f.ResolvedSecrets).ToList();
            if (allSecrets.Any())
            {
                AnsiConsole.MarkupLine("\n[bold]Resolved Secrets:[/]");
                foreach (var s in allSecrets)
                {
                    var value = s.IsSensitive ? "[dim]****[/]" : s.Value?.EscapeMarkup() ?? "-";
                    AnsiConsole.MarkupLine($"  • {s.Reference.Name ?? s.Reference.Key}: {value}");
                }
            }
            AnsiConsole.WriteLine();
        }

        return Task.CompletedTask;
    }

    private async Task StopForwardAsync()
    {
        var templates = _portForwardingService.GetAllRunningTemplates();

        if (!templates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No active forwards to stop.[/]");
            return;
        }

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<RunningTemplate>()
                .Title("Select forward to [red]stop[/]:")
                .UseConverter(t => $"{t.Definition.Icon ?? "📦"} {t.Definition.Name.EscapeMarkup()} on {t.ClusterInfo.Name.EscapeMarkup()}")
                .AddChoices(templates)
        );

        await _portForwardingService.StopTemplateAsync(selection.TemplateId);
        AnsiConsole.MarkupLine($"[green]✓[/] Stopped: {selection.Definition.Name.EscapeMarkup()}");
    }

    private async Task ListTemplatesAsync()
    {
        var templates = await _storage.GetForwardTemplatesAsync();

        if (!templates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No saved templates.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Forwards[/]")
            .AddColumn("[bold]Tags[/]")
            .AddColumn("[bold]★[/]");

        foreach (var t in templates.OrderByDescending(t => t.Favorite).ThenBy(t => t.Name))
        {
            var tags = t.Tags?.Any() == true ? string.Join(", ", t.Tags) : "-";
            table.AddRow(
                $"{t.Icon ?? "📦"} [cyan]{t.Name.EscapeMarkup()}[/]",
                t.Forwards.Count.ToString(),
                tags,
                t.Favorite ? "[yellow]★[/]" : ""
            );
        }

        AnsiConsole.Write(table);
    }

    private async Task RunTemplateAsync()
    {
        if (_state.SelectedCluster == null)
        {
            AnsiConsole.MarkupLine("[yellow]Please select a cluster first.[/]");
            return;
        }

        var templates = await _storage.GetForwardTemplatesAsync();

        if (!templates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No templates available.[/]");
            return;
        }

        var template = AnsiConsole.Prompt(
            new SelectionPrompt<ForwardTemplate>()
                .Title("Select template to [green]run[/]:")
                .PageSize(15)
                .UseConverter(t => $"{t.Icon ?? "📦"} {t.Name.EscapeMarkup()} ({t.Forwards.Count} forwards)")
                .AddChoices(templates.OrderByDescending(t => t.Favorite))
        );

        try
        {
            var running = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Starting {template.Name.EscapeMarkup()}...", async ctx =>
                    await _portForwardingService.StartTemplateAsync(template, _state.SelectedCluster));

            AnsiConsole.MarkupLine($"\n[green]✓[/] Template [cyan]{template.Name.EscapeMarkup()}[/] started!\n");

            var table = new Table().Border(TableBorder.Simple)
                .AddColumn("Forward")
                .AddColumn("Status")
                .AddColumn("Local Address");

            foreach (var f in running.Forwards)
            {
                var statusColor = f.Status == ForwardStatus.Running ? "green" : "red";
                table.AddRow(f.Name, $"[{statusColor}]{f.Status}[/]", f.LocalAddress ?? "-");
            }
            AnsiConsole.Write(table);

            // Show resolved secrets
            var secrets = running.Forwards.SelectMany(f => f.ResolvedSecrets).ToList();
            if (secrets.Any())
            {
                AnsiConsole.MarkupLine("\n[bold]Resolved Secrets:[/]");
                foreach (var s in secrets)
                {
                    AnsiConsole.MarkupLine($"  • {s.Reference.Name ?? s.Reference.Key}: [dim]****[/]");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private async Task StopTemplateAsync()
    {
        var running = _portForwardingService.GetAllRunningTemplates();

        if (!running.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No templates running.[/]");
            return;
        }

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<RunningTemplate>()
                .Title("Select template to [red]stop[/]:")
                .UseConverter(t => $"{t.Definition.Icon ?? "📦"} {t.Definition.Name.EscapeMarkup()} on {t.ClusterInfo.Name.EscapeMarkup()}")
                .AddChoices(running)
        );

        await _portForwardingService.StopTemplateAsync(selection.TemplateId);
        AnsiConsole.MarkupLine($"[green]✓[/] Stopped: {selection.Definition.Name.EscapeMarkup()}");
    }

    private async Task CreateTemplateAsync()
    {
        AnsiConsole.Write(new Rule("[bold]Create New Template[/]"));

        var name = AnsiConsole.Prompt(
            new TextPrompt<string>("Template name:")
        );

        var description = AnsiConsole.Prompt(
            new TextPrompt<string>("Description (optional):")
                .AllowEmpty()
        );

        var icon = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Icon:")
                .AddChoices("🚀", "🔧", "💻", "🌐", "📦", "⚙️")
        );

        var template = new ForwardTemplate
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Description = string.IsNullOrEmpty(description) ? null : description,
            Icon = icon,
            Forwards = new List<PortForwardDefinition>()
        };

        // Add forwards if cluster is available
        if (_state.SelectedCluster != null)
        {
            while (AnsiConsole.Confirm("Add a forward?", true))
            {
                var fwd = await CreateForwardDefinitionAsync();
                if (fwd != null)
                {
                    template.Forwards.Add(fwd);
                    AnsiConsole.MarkupLine($"[green]✓[/] Added: {fwd.Name.EscapeMarkup()}");
                }
            }
        }

        await _storage.AddForwardTemplateAsync(template);
        AnsiConsole.MarkupLine($"\n[green]✓[/] Template [cyan]{template.Name.EscapeMarkup()}[/] created!");
    }

    private async Task<PortForwardDefinition?> CreateForwardDefinitionAsync()
    {
        if (_state.SelectedCluster == null) return null;

        var @namespace = await PromptHelpers.SelectNamespaceRequiredAsync(
            _kubeRepository,
            _state.SelectedCluster,
            defaultNamespace: _state.SelectedCluster.DefaultNamespace);

        var resourceType = AnsiConsole.Prompt(
            new SelectionPrompt<ResourceType>()
                .Title("Resource type:")
                .AddChoices(Enum.GetValues<ResourceType>())
        );

        string resourceName;
        List<int> ports;

        if (resourceType == ResourceType.Pod)
        {
            var pods = await _kubeRepository.ListPodsAsync(_state.SelectedCluster, @namespace);
            var podsWithPorts = pods.Where(p => p.Ports.Any()).ToList();

            if (!podsWithPorts.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No pods with ports.[/]");
                return null;
            }

            var pod = AnsiConsole.Prompt(
                new SelectionPrompt<PodInfo>()
                    .Title("Select pod:")
                    .UseConverter(p => $"{p.Name.EscapeMarkup()} - {string.Join(", ", p.Ports.Select(pt => pt.Port))}")
                    .AddChoices(podsWithPorts)
            );

            resourceName = pod.Name;
            ports = pod.Ports.Select(p => p.Port).ToList();
        }
        else
        {
            var services = await _kubeRepository.ListServicesAsync(_state.SelectedCluster, @namespace);
            var svcWithPorts = services.Where(s => s.Ports.Any()).ToList();

            if (!svcWithPorts.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No services with ports.[/]");
                return null;
            }

            var svc = AnsiConsole.Prompt(
                new SelectionPrompt<ServiceInfo>()
                    .Title("Select service:")
                    .UseConverter(s => $"{s.Name.EscapeMarkup()} - {string.Join(", ", s.Ports.Select(p => p.Port))}")
                    .AddChoices(svcWithPorts)
            );

            resourceName = svc.Name;
            ports = svc.Ports.Select(p => p.Port).ToList();
        }

        var targetPort = ports.Count == 1 ? ports[0] : AnsiConsole.Prompt(
            new SelectionPrompt<int>().Title("Target port:").AddChoices(ports));

        var localPort = AnsiConsole.Prompt(
            new TextPrompt<int>("Local port:").DefaultValue(targetPort));

        var fwdName = AnsiConsole.Prompt(
            new TextPrompt<string>("Forward name:")
                .DefaultValue($"{resourceName}:{targetPort}")
        );

        return new PortForwardDefinition
        {
            Id = Guid.CreateVersion7(),
            Name = fwdName,
            ResourceType = resourceType,
            ResourceName = resourceName,
            Namespace = @namespace,
            TargetPort = targetPort,
            LocalPort = localPort
        };
    }
}

