using System;
using System.Collections.Generic;
using System.Text;

namespace KonciergeUI.Models.Forwarding
{
    /// <summary>
    /// A reusable template defining a set of port forwards + linked secrets/configmaps.
    /// User creates/edits templates, then applies them to a cluster.
    /// </summary>
    public record ForwardTemplate
    {
        /// <summary>
        /// Unique identifier for this template.
        /// </summary>
        public required Guid Id { get; init; } = Guid.CreateVersion7();

        /// <summary>
        /// Display name (e.g., "Backend Dev Environment").
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Optional description/notes.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// List of port forward definitions in this template.
        /// </summary>
        public required List<PortForwardDefinition> Forwards { get; init; } = new();

        /// <summary>
        /// When this template was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// When this template was last modified.
        /// </summary>
        public DateTimeOffset ModifiedAt { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Optional icon/emoji identifier (e.g., "🚀", "backend", etc.).
        /// </summary>
        public string? Icon { get; init; }

        /// <summary>
        /// Tags for organizing templates (e.g., "dev", "staging", "backend").
        /// </summary>
        public List<string>? Tags { get; init; }
    }

    

}
