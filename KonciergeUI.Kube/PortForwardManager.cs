using System.Collections.Concurrent;
using k8s;
using KonciergeUI.Core.Abstractions;
using KonciergeUI.Models.Forwarding;

namespace KonciergeUI.Kube;

public sealed class PortForwardManager : IPortForwardManager
{
    // templateId → runtime state
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, PortForwardRuntime>> _templateRuntimes = new();

    public async Task<RunningTemplate> StartTemplateAsync(
        IKubernetes client,
        ForwardTemplate template,
        CancellationToken ct = default)
    {
        // Stop existing template first
        await StopTemplateAsync(template.Id).ConfigureAwait(false);

        var runtimes = new ConcurrentDictionary<Guid, PortForwardRuntime>();

        // Start all forwards
        var startTasks = template.Forwards.Select(async def =>
        {
            var runtime = new PortForwardRuntime(client, def, def.Namespace);
            runtime.Start();
            runtimes[def.Id] = runtime;
        });

        await Task.WhenAll(startTasks).ConfigureAwait(false);

        var instances = runtimes.Values.Select(r => r.Instance).ToList();

        var running = new RunningTemplate
        {
            TemplateId = template.Id,
            Definition = template,
            Forwards = instances.AsReadOnly(),
            StartedAt = DateTimeOffset.UtcNow
        };

        _templateRuntimes[template.Id] = runtimes;
        return running;
    }

    public async Task StopTemplateAsync(Guid templateId)
    {
        if (!_templateRuntimes.TryRemove(templateId, out var runtimes))
            return;

        var tasks = runtimes.Values.Select(r => r.DisposeAsync().AsTask()).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task StartForwardAsync(Guid templateId, Guid forwardId)
    {
        if (_templateRuntimes.TryGetValue(templateId, out var runtimes) &&
            runtimes.TryGetValue(forwardId, out var runtime))
        {
            await Task.Run(() => runtime.Start()).ConfigureAwait(false);
        }
    }

    public async Task StopForwardAsync(Guid templateId, Guid forwardId)
    {
        if (_templateRuntimes.TryGetValue(templateId, out var runtimes) &&
            runtimes.TryGetValue(forwardId, out var runtime))
        {
            await runtime.StopAsync().ConfigureAwait(false);
        }
    }
    
    public RunningTemplate? GetTemplate(Guid templateId)
    {
        if (!_templateRuntimes.TryGetValue(templateId, out var runtimes) || runtimes.IsEmpty)
            return null;

        return new RunningTemplate
        {
            TemplateId = templateId,
            Forwards = runtimes.Values
                .Select(r => r.Instance)
                .OrderBy(i => i.Name)
                .ToList()
                .AsReadOnly(),
            StartedAt = runtimes.Values.First().Instance.StartedAt
        };
    }

}