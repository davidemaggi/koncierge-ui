using System;
using System.Collections.Generic;
using System.Text;

namespace KonciergeUI.Models.Security
{

    public enum SecretSourceType
    {
        /// <summary>
        /// Kubernetes Secret (base64-encoded).
        /// </summary>
        Secret,

        /// <summary>
        /// Kubernetes ConfigMap (plain text).
        /// </summary>
        ConfigMap,

        /// <summary>
        /// Entry from a kubeconfig file (e.g., token, cert).
        /// </summary>
        KubeconfigEntry
    }

    
}
