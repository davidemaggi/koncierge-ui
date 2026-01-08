using KonciergeUI.Models.Security;
using System;
using System.Collections.Generic;
using System.Text;
using static KonciergeUI.Models.Forwarding.Enums;

namespace KonciergeUI.Models.Forwarding
{
    /// <summary>
    /// A single port forward definition within a template.
    /// </summary>
    public record PortForwardDefinition
    {
        /// <summary>
        /// Unique identifier for this forward definition.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Display name (e.g., "API Server").
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Kubernetes resource type (Pod or Service).
        /// </summary>
        public required ResourceType ResourceType { get; init; }

        /// <summary>
        /// Name of the pod or service.
        /// </summary>
        public required string ResourceName { get; init; }

        /// <summary>
        /// Namespace of the resource (can be null to use cluster default).
        /// </summary>
        public string? Namespace { get; init; }

        /// <summary>
        /// Target port on the pod/service (e.g., 8080).
        /// </summary>
        public required int TargetPort { get; init; }

        /// <summary>
        /// Local port to bind (e.g., 8080). Can be 0 for auto-assign.
        /// </summary>
        public int LocalPort { get; init; }

        /// <summary>
        /// Protocol hint (Http, Tcp, Grpc, etc.).
        /// </summary>
        public ForwardProtocol Protocol { get; init; } = ForwardProtocol.Tcp;

        /// <summary>
        /// List of secrets/configmaps/kubeconfig entries linked to this forward.
        /// </summary>
        public List<SecretReference> LinkedSecrets { get; init; } = new();

        /// <summary>
        /// Optional selector labels (if ResourceType is Service and you want pod selection).
        /// </summary>
        public Dictionary<string, string>? LabelSelector { get; init; }
    }
}
