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
        AsciiLogo.Write();
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
                        "➕ Create Template",
                        "✏️  Edit Template",
                        "🗑️  Delete Template")
                    .AddChoiceGroup("[bold]Other[/]",
                        "ℹ️  Info",
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

                case "✏️  Edit Template":
                    await EditTemplateAsync();
                    break;

                case "🗑️  Delete Template":
                    await DeleteTemplateAsync();
                    break;

                case "ℹ️  Info":
                    new InfoCommand().Execute(context);
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

        // Linked secrets/configmaps
        AnsiConsole.MarkupLine("\n[bold]Link Secrets/ConfigMaps[/]");
        var linkedSecrets = await SelectSecretsAsync(@namespace);

        var forwardDef = new PortForwardDefinition
        {
            Id = Guid.CreateVersion7(),
            Name = $"{resourceName}:{targetPort}",
            ResourceType = resourceType,
            ResourceName = resourceName,
            Namespace = @namespace,
            TargetPort = targetPort,
            LocalPort = localPort,
            LinkedSecrets = linkedSecrets
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
                .AddColumn("[bold]Linked Secrets/ConfigMaps[/]");

            foreach (var f in t.Forwards)
            {
                var statusColor = f.Status switch
                {
                    ForwardStatus.Running => "green",
                    ForwardStatus.Failed => "red",
                    _ => "yellow"
                };

                // Build secrets display for this forward
                var secretsDisplay = "-";
                if (f.ResolvedSecrets.Any())
                {
                    var secretLines = f.ResolvedSecrets.Select(s => 
                        $"{(s.Reference.Name ?? s.Reference.Key).EscapeMarkup()}: {s.Value?.EscapeMarkup() ?? "-"}");
                    secretsDisplay = string.Join("\n", secretLines);
                }

                table.AddRow(
                    f.Name.EscapeMarkup(),
                    $"{f.Definition.ResourceType}: {f.Definition.ResourceName.EscapeMarkup()}",
                    $"[{statusColor}]{f.Status}[/]",
                    f.LocalAddress ?? $"localhost:{f.BoundLocalPort}",
                    secretsDisplay
                );
            }

            AnsiConsole.Write(table);
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

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Forward[/]")
                .AddColumn("[bold]Status[/]")
                .AddColumn("[bold]Local Address[/]")
                .AddColumn("[bold]Linked Secrets/ConfigMaps[/]");

            foreach (var f in running.Forwards)
            {
                var statusColor = f.Status == ForwardStatus.Running ? "green" : "red";
                
                // Build secrets display for this forward
                var secretsDisplay = "-";
                if (f.ResolvedSecrets.Any())
                {
                    var secretLines = f.ResolvedSecrets.Select(s => 
                        $"{(s.Reference.Name ?? s.Reference.Key).EscapeMarkup()}: {s.Value?.EscapeMarkup() ?? "-"}");
                    secretsDisplay = string.Join("\n", secretLines);
                }
                
                table.AddRow(
                    f.Name.EscapeMarkup(), 
                    $"[{statusColor}]{f.Status}[/]", 
                    f.LocalAddress ?? "-",
                    secretsDisplay
                );
            }
            AnsiConsole.Write(table);
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

        // Linked secrets/configmaps
        AnsiConsole.MarkupLine("\n[bold]Link Secrets/ConfigMaps[/]");
        var linkedSecrets = await SelectSecretsAsync(@namespace);

        return new PortForwardDefinition
        {
            Id = Guid.CreateVersion7(),
            Name = fwdName,
            ResourceType = resourceType,
            ResourceName = resourceName,
            Namespace = @namespace,
            TargetPort = targetPort,
            LocalPort = localPort,
            LinkedSecrets = linkedSecrets
        };
    }

    private async Task EditTemplateAsync()
    {
        var templates = await _storage.GetForwardTemplatesAsync();

        if (!templates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No templates to edit.[/]");
            return;
        }

        var template = AnsiConsole.Prompt(
            new SelectionPrompt<ForwardTemplate>()
                .Title("Select template to [cyan]edit[/]:")
                .PageSize(15)
                .UseConverter(t => $"{t.Icon ?? "📦"} {t.Name.EscapeMarkup()} ({t.Forwards.Count} forwards)")
                .AddChoices(templates.OrderByDescending(t => t.Favorite))
        );

        var modified = false;

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold]Editing: {template.Icon ?? "📦"} {template.Name.EscapeMarkup()}[/]"));
            
            // Show current template info
            AnsiConsole.MarkupLine($"[dim]Description:[/] {template.Description ?? "-"}");
            AnsiConsole.MarkupLine($"[dim]Tags:[/] {(template.Tags?.Any() == true ? string.Join(", ", template.Tags) : "-")}");
            AnsiConsole.MarkupLine($"[dim]Favorite:[/] {(template.Favorite ? "Yes" : "No")}");
            AnsiConsole.WriteLine();

            // Show forwards
            if (template.Forwards.Any())
            {
                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .Title("[bold]Forwards[/]")
                    .AddColumn("#")
                    .AddColumn("[bold]Name[/]")
                    .AddColumn("[bold]Resource[/]")
                    .AddColumn("[bold]Ports[/]")
                    .AddColumn("[bold]Linked Secrets[/]");

                for (var i = 0; i < template.Forwards.Count; i++)
                {
                    var f = template.Forwards[i];
                    var secretsCount = f.LinkedSecrets?.Count ?? 0;
                    table.AddRow(
                        (i + 1).ToString(),
                        f.Name.EscapeMarkup(),
                        $"{f.ResourceType}: {f.ResourceName.EscapeMarkup()}",
                        $"{f.LocalPort} → {f.TargetPort}",
                        secretsCount > 0 ? $"{secretsCount} linked" : "-"
                    );
                }
                AnsiConsole.Write(table);
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No forwards in this template.[/]");
            }

            AnsiConsole.WriteLine();

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices(
                        "📝 Edit template name/description",
                        "🏷️  Edit tags",
                        "⭐ Toggle favorite",
                        "🎨 Change icon",
                        "➕ Add forward",
                        "✏️  Edit forward",
                        "🗑️  Remove forward",
                        "💾 Save and exit",
                        "❌ Cancel (discard changes)")
            );

            switch (action)
            {
                case "📝 Edit template name/description":
                    template.Name = AnsiConsole.Prompt(
                        new TextPrompt<string>("Template name:")
                            .DefaultValue(template.Name));
                    template.Description = AnsiConsole.Prompt(
                        new TextPrompt<string>("Description (optional):")
                            .DefaultValue(template.Description ?? "")
                            .AllowEmpty());
                    if (string.IsNullOrWhiteSpace(template.Description))
                        template.Description = null;
                    modified = true;
                    break;

                case "🏷️  Edit tags":
                    var currentTags = template.Tags != null ? string.Join(", ", template.Tags) : "";
                    var tagsInput = AnsiConsole.Prompt(
                        new TextPrompt<string>("Tags (comma separated):")
                            .DefaultValue(currentTags)
                            .AllowEmpty());
                    template.Tags = string.IsNullOrWhiteSpace(tagsInput)
                        ? null
                        : tagsInput.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                    modified = true;
                    break;

                case "⭐ Toggle favorite":
                    template.Favorite = !template.Favorite;
                    AnsiConsole.MarkupLine($"[green]✓[/] Favorite: {(template.Favorite ? "Yes" : "No")}");
                    modified = true;
                    break;

                case "🎨 Change icon":
                    template.Icon = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Select icon:")
                            .AddChoices("🚀", "🔧", "💻", "🌐", "📦", "⚙️", "🔌", "🎯", "🔥", "💾", "🗄️", "📊"));
                    modified = true;
                    break;

                case "➕ Add forward":
                    if (_state.SelectedCluster == null)
                    {
                        AnsiConsole.MarkupLine("[yellow]Please select a cluster first to add forwards.[/]");
                    }
                    else
                    {
                        var newFwd = await CreateForwardDefinitionAsync();
                        if (newFwd != null)
                        {
                            template.Forwards.Add(newFwd);
                            AnsiConsole.MarkupLine($"[green]✓[/] Added forward: {newFwd.Name.EscapeMarkup()}");
                            modified = true;
                        }
                    }
                    break;

                case "✏️  Edit forward":
                    if (!template.Forwards.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No forwards to edit.[/]");
                    }
                    else
                    {
                        var fwdToEdit = AnsiConsole.Prompt(
                            new SelectionPrompt<PortForwardDefinition>()
                                .Title("Select forward to edit:")
                                .UseConverter(f => $"{f.Name.EscapeMarkup()} ({f.LocalPort} → {f.TargetPort})")
                                .AddChoices(template.Forwards));
                        
                        if (await EditForwardAsync(fwdToEdit))
                            modified = true;
                    }
                    break;

                case "🗑️  Remove forward":
                    if (!template.Forwards.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No forwards to remove.[/]");
                    }
                    else
                    {
                        var fwdToRemove = AnsiConsole.Prompt(
                            new SelectionPrompt<PortForwardDefinition>()
                                .Title("Select forward to [red]remove[/]:")
                                .UseConverter(f => $"{f.Name.EscapeMarkup()} ({f.LocalPort} → {f.TargetPort})")
                                .AddChoices(template.Forwards));
                        
                        if (AnsiConsole.Confirm($"Remove forward '{fwdToRemove.Name}'?", false))
                        {
                            template.Forwards.Remove(fwdToRemove);
                            AnsiConsole.MarkupLine($"[green]✓[/] Removed: {fwdToRemove.Name.EscapeMarkup()}");
                            modified = true;
                        }
                    }
                    break;

                case "💾 Save and exit":
                    if (modified)
                    {
                        await _storage.UpdateForwardTemplateAsync(template);
                        AnsiConsole.MarkupLine($"[green]✓[/] Template [cyan]{template.Name.EscapeMarkup()}[/] saved!");
                    }
                    return;

                case "❌ Cancel (discard changes)":
                    if (modified && !AnsiConsole.Confirm("Discard all changes?", false))
                        continue;
                    AnsiConsole.MarkupLine("[dim]Changes discarded.[/]");
                    return;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey(true);
        }
    }

    private async Task<bool> EditForwardAsync(PortForwardDefinition forward)
    {
        var modified = false;

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[bold]Editing Forward: {forward.Name.EscapeMarkup()}[/]"));

            // Show current forward info
            var infoTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Property")
                .AddColumn("Value");

            infoTable.AddRow("Name", forward.Name.EscapeMarkup());
            infoTable.AddRow("Resource", $"{forward.ResourceType}: {forward.ResourceName.EscapeMarkup()}");
            infoTable.AddRow("Namespace", forward.Namespace);
            infoTable.AddRow("Local Port", forward.LocalPort.ToString());
            infoTable.AddRow("Target Port", forward.TargetPort.ToString());
            infoTable.AddRow("Protocol", forward.Protocol.ToString());

            AnsiConsole.Write(infoTable);

            // Show linked secrets
            if (forward.LinkedSecrets?.Any() == true)
            {
                AnsiConsole.MarkupLine("\n[bold]Linked Secrets/ConfigMaps:[/]");
                foreach (var s in forward.LinkedSecrets)
                {
                    var type = s.SourceType == Models.Security.SecretSourceType.Secret ? "Secret" : "ConfigMap";
                    AnsiConsole.MarkupLine($"  • [{(s.SourceType == Models.Security.SecretSourceType.Secret ? "red" : "blue")}]{type}[/]: {s.ResourceName.EscapeMarkup()}/{s.Key.EscapeMarkup()}");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("\n[dim]No linked secrets/configmaps.[/]");
            }

            AnsiConsole.WriteLine();

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to edit?")
                    .AddChoices(
                        "📝 Change name",
                        "🔢 Change local port",
                        "🔗 Add secrets/configmaps",
                        "🗑️  Remove secret/configmap",
                        "🔄 Replace all secrets/configmaps",
                        "✅ Done editing",
                        "❌ Cancel")
            );

            switch (action)
            {
                case "📝 Change name":
                    forward.Name = AnsiConsole.Prompt(
                        new TextPrompt<string>("Forward name:")
                            .DefaultValue(forward.Name));
                    modified = true;
                    break;

                case "🔢 Change local port":
                    forward.LocalPort = AnsiConsole.Prompt(
                        new TextPrompt<int>("Local port (0 for auto):")
                            .DefaultValue(forward.LocalPort));
                    modified = true;
                    break;

                case "🔗 Add secrets/configmaps":
                    if (_state.SelectedCluster == null)
                    {
                        AnsiConsole.MarkupLine("[yellow]Please select a cluster first.[/]");
                    }
                    else
                    {
                        var newSecrets = await SelectSecretsAsync(forward.Namespace);
                        if (newSecrets.Any())
                        {
                            forward.LinkedSecrets ??= new List<Models.Security.SecretReference>();
                            foreach (var s in newSecrets)
                            {
                                if (!forward.LinkedSecrets.Any(x => x.ResourceName == s.ResourceName && x.Key == s.Key))
                                {
                                    forward.LinkedSecrets.Add(s);
                                    AnsiConsole.MarkupLine($"[green]✓[/] Added: {s.ResourceName.EscapeMarkup()}/{s.Key.EscapeMarkup()}");
                                }
                            }
                            modified = true;
                        }
                    }
                    break;

                case "🗑️  Remove secret/configmap":
                    if (forward.LinkedSecrets?.Any() != true)
                    {
                        AnsiConsole.MarkupLine("[yellow]No secrets to remove.[/]");
                    }
                    else
                    {
                        var secretToRemove = AnsiConsole.Prompt(
                            new SelectionPrompt<Models.Security.SecretReference>()
                                .Title("Select secret/configmap to [red]remove[/]:")
                                .UseConverter(s => $"{s.ResourceName}/{s.Key}")
                                .AddChoices(forward.LinkedSecrets));
                        
                        forward.LinkedSecrets.Remove(secretToRemove);
                        AnsiConsole.MarkupLine($"[green]✓[/] Removed: {secretToRemove.ResourceName.EscapeMarkup()}/{secretToRemove.Key.EscapeMarkup()}");
                        modified = true;
                    }
                    break;

                case "🔄 Replace all secrets/configmaps":
                    if (_state.SelectedCluster == null)
                    {
                        AnsiConsole.MarkupLine("[yellow]Please select a cluster first.[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[dim]Current secrets will be replaced with new selection.[/]");
                        var replacementSecrets = await SelectSecretsAsync(forward.Namespace);
                        forward.LinkedSecrets = replacementSecrets;
                        AnsiConsole.MarkupLine($"[green]✓[/] Replaced with {replacementSecrets.Count} secret(s)/configmap(s)");
                        modified = true;
                    }
                    break;

                case "✅ Done editing":
                    return modified;

                case "❌ Cancel":
                    return false;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey(true);
        }
    }

    private async Task DeleteTemplateAsync()
    {
        var templates = await _storage.GetForwardTemplatesAsync();

        if (!templates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No templates to delete.[/]");
            return;
        }

        var template = AnsiConsole.Prompt(
            new SelectionPrompt<ForwardTemplate>()
                .Title("Select template to [red]delete[/]:")
                .PageSize(15)
                .UseConverter(t => $"{t.Icon ?? "📦"} {t.Name.EscapeMarkup()} ({t.Forwards.Count} forwards)")
                .AddChoices(templates.OrderByDescending(t => t.Favorite))
        );

        if (AnsiConsole.Confirm($"[red]Delete[/] template '{template.Name}'? This cannot be undone.", false))
        {
            await _storage.RemoveForwardTemplateAsync(template.Id);
            AnsiConsole.MarkupLine($"[green]✓[/] Deleted: {template.Name.EscapeMarkup()}");
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]Deletion cancelled.[/]");
        }
    }

    private async Task<List<Models.Security.SecretReference>> SelectSecretsAsync(string @namespace)
    {
        var secrets = new List<Models.Security.SecretReference>();

        if (_state.SelectedCluster == null)
            return secrets;

        try
        {
            var k8sSecrets = await AnsiConsole.Status()
                .StartAsync("Loading secrets...", async _ =>
                    await _kubeRepository.ListSecretsAsync(_state.SelectedCluster, @namespace));

            var configMaps = await AnsiConsole.Status()
                .StartAsync("Loading configmaps...", async _ =>
                    await _kubeRepository.ListConfigMapsAsync(_state.SelectedCluster, @namespace));

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
                            secrets.Add(new Models.Security.SecretReference
                            {
                                SourceType = Models.Security.SecretSourceType.Secret,
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
                            secrets.Add(new Models.Security.SecretReference
                            {
                                SourceType = Models.Security.SecretSourceType.ConfigMap,
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
}



