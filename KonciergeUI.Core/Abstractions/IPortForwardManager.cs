using k8s;
using KonciergeUI.Models.Forwarding;

namespace KonciergeUI.Core.Abstractions;

public interface IPortForwardManager
{
    Task<RunningTemplate> StartTemplateAsync(IKubernetes client, ForwardTemplate template, CancellationToken ct = default);
    Task StopTemplateAsync(Guid templateId);
    Task StartForwardAsync(Guid templateId, Guid forwardId);
    Task StopForwardAsync(Guid templateId, Guid forwardId);
    RunningTemplate? GetTemplate(Guid templateId); // renamed for clarity
}

