using System;
using System.Collections.Generic;
using System.Text;
using static KonciergeUI.Models.Forwarding.Enums;

namespace KonciergeUI.Models.Forwarding
{
    /// <summary>
    /// A single active port forward within an execution.
    /// </summary>
    public record ForwardInstance
    {
        /// <summary>
        /// Unique identifier for this forward instance.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Display name (from PortForwardDefinition).
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// The original definition from the template.
        /// </summary>
        public required PortForwardDefinition Definition { get; init; }

        /// <summary>
        /// Current status of this forward.
        /// </summary>
        public ForwardStatus Status { get; init; } = ForwardStatus.Starting;

        /// <summary>
        /// Actual local port bound (might differ from Definition.LocalPort if auto-assigned).
        /// </summary>
        public int? BoundLocalPort { get; init; }

        /// <summary>
        /// Target host (typically localhost/127.0.0.1).
        /// </summary>
        public string LocalHost { get; init; } = "127.0.0.1";

        /// <summary>
        /// Full local address (e.g., "http://localhost:8080").
        /// </summary>
        public string? LocalAddress => Status == ForwardStatus.Running && BoundLocalPort.HasValue
            ? $"{GetProtocolScheme()}://{LocalHost}:{BoundLocalPort}"
            : null;

        /// <summary>
        /// Resolved secrets/configmaps with their actual values.
        /// </summary>
        public List<ResolvedSecret> ResolvedSecrets { get; init; } = new();

        /// <summary>
        /// When this forward started.
        /// </summary>
        public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// When this forward stopped (null if still running).
        /// </summary>
        public DateTimeOffset? StoppedAt { get; init; }

        /// <summary>
        /// Error message if this forward failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Number of reconnection attempts (for resilience tracking).
        /// </summary>
        public int ReconnectAttempts { get; init; }

        private string GetProtocolScheme() => Definition.Protocol switch
        {
            ForwardProtocol.Http => "http",
            ForwardProtocol.Https => "https",
            ForwardProtocol.Grpc => "grpc",
            _ => "tcp"
        };
    }
}
