using KonciergeUI.Models.Kube;

namespace KonciergeUI.Cli.Infrastructure;

/// <summary>
/// Holds the current CLI session state
/// </summary>
public class CliState
{
    /// <summary>
    /// Currently selected cluster
    /// </summary>
    public ClusterConnectionInfo? SelectedCluster { get; set; }

    /// <summary>
    /// Currently selected namespace filter
    /// </summary>
    public string? NamespaceFilter { get; set; }
}

