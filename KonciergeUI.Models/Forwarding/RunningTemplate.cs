using KonciergeUI.Models.Kube;

namespace KonciergeUI.Models.Forwarding;

public sealed record RunningTemplate
{
    public Guid TemplateId { get; init; }
    public ForwardTemplate Definition { get; init; } = default!;
    public IReadOnlyCollection<ForwardInstance> Forwards { get; init; } = Array.Empty<ForwardInstance>();
    public DateTimeOffset StartedAt { get; init; }
    public ClusterConnectionInfo ClusterInfo { get; init; } = default!; // NEW: Track which cluster
}
