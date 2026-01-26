using k8s;
using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models.Kube;

namespace KonciergeUI.Core.Abstractions;

public interface IPortForwardingService
{
    Task<RunningTemplate> StartTemplateAsync(
        ForwardTemplate template,
        ClusterConnectionInfo cluster,
        CancellationToken cancellationToken = default);

    Task StopTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<RunningTemplate?> GetRunningTemplateAsync(Guid templateId);
    IReadOnlyCollection<RunningTemplate> GetAllRunningTemplates();
    Task<IReadOnlyCollection<string>> GetForwardLogsAsync(Guid forwardInstanceId, int maxLines = 100);
}


