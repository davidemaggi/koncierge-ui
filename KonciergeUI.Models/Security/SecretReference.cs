using System;
using System.Collections.Generic;
using System.Text;

namespace KonciergeUI.Models.Security
{
    /// <summary>
    /// Reference to a Kubernetes secret, configmap, or kubeconfig entry
    /// that should be linked to a port forward.
    /// </summary>
    public record SecretReference
    {
        /// <summary>
        /// Unique identifier for this reference.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Display name/label (e.g., "Database Password").
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Type of secret source.
        /// </summary>
        public required SecretSourceType SourceType { get; init; }

        /// <summary>
        /// Name of the Kubernetes secret/configmap.
        /// </summary>
        public required string ResourceName { get; init; }

        /// <summary>
        /// Namespace of the resource (null = use default).
        /// </summary>
        public string? Namespace { get; init; }

        /// <summary>
        /// Key within the secret/configmap data.
        /// </summary>
        public required string Key { get; init; }

        /// <summary>
        /// Optional icon identifier for UI display (e.g., "key", "database", "api").
        /// </summary>
        public string? Icon { get; init; }
    }
}
