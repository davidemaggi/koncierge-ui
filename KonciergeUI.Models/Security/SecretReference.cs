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
        public Guid Id { get; set; } = Guid.CreateVersion7();

        /// <summary>
        /// Display name/label (e.g., "Database Password").
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Type of secret source.
        /// </summary>
        public required SecretSourceType SourceType { get; set; }

        /// <summary>
        /// Name of the Kubernetes secret/configmap.
        /// </summary>
        public required string ResourceName { get; set; }

        /// <summary>
        /// Namespace of the resource (null = use default).
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Key within the secret/configmap data.
        /// </summary>
        public required string Key { get; set; }

        /// <summary>
        /// Optional icon identifier for UI display (e.g., "key", "database", "api").
        /// </summary>
        public string? Icon { get; set; }
    }
}
