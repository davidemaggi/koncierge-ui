using System;
using System.Collections.Generic;
using System.Text;

namespace KonciergeUI.Models.Kube
{
    /// <summary>
    /// Represents a Kubernetes cluster connection extracted from a kubeconfig file.
    /// Maps to a kubeconfig context + cluster + user.
    /// </summary>
    public record ClusterConnectionInfo
    {
        /// <summary>
        /// Unique identifier for this connection (generated or from context name).
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Display name (typically the context name from kubeconfig).
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Full path to the kubeconfig file.
        /// </summary>
        public required string KubeconfigPath { get; init; }

        /// <summary>
        /// Context name within the kubeconfig (e.g., "my-dev-cluster").
        /// </summary>
        public required string ContextName { get; init; }

        /// <summary>
        /// Cluster server URL (e.g., "https://192.168.1.100:6443").
        /// </summary>
        public string? ClusterUrl { get; init; }

        /// <summary>
        /// Default namespace for this context (can be null if not set in kubeconfig).
        /// </summary>
        public string? DefaultNamespace { get; init; }

        /// <summary>
        /// User/auth entry name from kubeconfig.
        /// </summary>
        public string? UserName { get; init; }

        /// <summary>
        /// Whether this is the current-context in the kubeconfig.
        /// </summary>
        public bool IsCurrentContext { get; init; }

        /// <summary>
        /// Whether this connection is from the default kubeconfig location (~/.kube/config).
        /// </summary>
        public bool IsDefaultKubeconfig { get; init; }

        /// <summary>
        /// Optional friendly description or notes.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Last successful connection timestamp (null if never connected).
        /// </summary>
        public DateTimeOffset? LastConnected { get; init; }
    }
}
