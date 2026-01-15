namespace KonciergeUI.Models.Kube;

public record PodInfo
{
    public required string Name { get; init; }
    public required string Namespace { get; init; }
    public required PodStatus Status { get; init; }
    public List<ContainerPort> Ports { get; init; } = new();
    public string? Phase { get; init; } // Running, Pending, Failed, etc.
    public DateTimeOffset? StartTime { get; init; }
    public Dictionary<string, string> Labels { get; init; } = new();
    public string? NodeName { get; init; }
    public int RestartCount { get; init; }
}

public record ContainerPort
{
    public required string Name { get; init; }
    public required int Port { get; init; }
    public string Protocol { get; init; } = "TCP";
    public string? ContainerName { get; init; }
}

public enum PodStatus
{
    Running,
    Pending,
    Succeeded,
    Failed,
    Unknown,
    CrashLoopBackOff,
    ImagePullBackOff
}