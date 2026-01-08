namespace KonciergeUI.Models.Kube;

public class PodInfo
{
    public string Name { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<string> Ports { get; init; } = new();
}