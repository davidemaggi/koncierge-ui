using k8s;
using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models.Kube;
using static KonciergeUI.Models.Forwarding.Enums;

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

    /// <summary>
    /// Start a single forward within a running template.
    /// </summary>
    Task<bool> StartForwardAsync(Guid forwardInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop a single forward within a running template.
    /// </summary>
    Task<bool> StopForwardAsync(Guid forwardInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restart a single forward (stop + start).
    /// </summary>
    Task<bool> RestartForwardAsync(Guid forwardInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the status of a specific forward instance.
    /// </summary>
    Task<ForwardStatus?> GetForwardStatusAsync(Guid forwardInstanceId);

    /// <summary>
    /// Get a specific forward instance by ID.
    /// </summary>
    ForwardInstance? GetForwardInstance(Guid forwardInstanceId);
}
