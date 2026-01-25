namespace KonciergeUI.Models.Forwarding;

public record ForwardLogEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string Message { get; init; } = string.Empty;
}