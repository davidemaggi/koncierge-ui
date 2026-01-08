using KonciergeUI.Models.Kube;
using System;
using System.Collections.Generic;
using System.Text;
using static KonciergeUI.Models.Forwarding.Enums;

namespace KonciergeUI.Models.Forwarding
{
    /// <summary>
    /// Represents a running execution of a ForwardTemplate on a specific cluster.
    /// Tracks all active forward instances and their status.
    /// </summary>
    public record ForwardTemplateExecution
    {
        /// <summary>
        /// Unique identifier for this execution instance.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// The template being executed.
        /// </summary>
        public required ForwardTemplate Template { get; init; }

        /// <summary>
        /// The cluster this template is running against.
        /// </summary>
        public required ClusterConnectionInfo Cluster { get; init; }

        /// <summary>
        /// Overall execution status.
        /// </summary>
        public ExecutionStatus Status { get; init; } = ExecutionStatus.Starting;

        /// <summary>
        /// List of individual forward instances (one per PortForwardDefinition).
        /// </summary>
        public required List<ForwardInstance> Forwards { get; init; } = new();

        /// <summary>
        /// When this execution started.
        /// </summary>
        public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// When this execution stopped (null if still running).
        /// </summary>
        public DateTimeOffset? StoppedAt { get; init; }

        /// <summary>
        /// Optional error message if execution failed.
        /// </summary>
        public string? ErrorMessage { get; init; }
    }

    

   

   
}
