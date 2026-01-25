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
        public required Guid Id { get; init; } = Guid.CreateVersion7();

        /// <summary>
        /// Display name (e.g., "API Server").
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Kubernetes resource type (Pod or Service).
        /// </summary>
        public required ResourceType ResourceType { get; set; }

        /// <summary>
        /// Name of the pod or service.
        /// </summary>
        public required string ResourceName { get; set; }

        /// <summary>
        /// Namespace of the resource (can be null to use cluster default).
        /// </summary>
        public required string Namespace { get; set; }

        /// <summary>
        /// Target port on the pod/service (e.g., 8080).
        /// </summary>
        public required int TargetPort { get; set; }

        /// <summary>
        /// Local port to bind (e.g., 8080). Can be 0 for auto-assign.
        /// </summary>
        public int LocalPort { get; set; }

        /// <summary>
        /// Protocol hint (Http, Tcp, Grpc, etc.).
        /// </summary>
        public ForwardProtocol Protocol { get; set; } = ForwardProtocol.Tcp;

        /// <summary>
        /// List of secrets/configmaps/kubeconfig entries linked to this forward.
        /// </summary>
        public List<SecretReference> LinkedSecrets { get; set; } = new();

        /// <summary>
        /// Optional selector labels (if ResourceType is Service and you want pod selection).
        /// </summary>
        public Dictionary<string, string>? LabelSelector { get; set; }
    }
}
