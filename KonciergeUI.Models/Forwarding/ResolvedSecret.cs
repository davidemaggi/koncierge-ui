using KonciergeUI.Models.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace KonciergeUI.Models.Forwarding
{
    /// <summary>
    /// A resolved secret/configmap value ready for display/copy.
    /// </summary>
    public record ResolvedSecret
    {
        /// <summary>
        /// Original reference from PortForwardDefinition.
        /// </summary>
        public required SecretReference Reference { get; init; }

        /// <summary>
        /// The actual decoded value (base64-decoded for secrets).
        /// </summary>
        public required string Value { get; init; }

        /// <summary>
        /// Whether this value is sensitive (should be masked in UI by default).
        /// </summary>
        public bool IsSensitive { get; init; } = true;
    }
}
