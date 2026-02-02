using KonciergeUI.Models.Kube;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KonciergeUI.Core.Abstractions
{
    public interface IClusterDiscoveryService
    {
        /// <summary>
        /// Discover all available clusters from default + custom kubeconfigs.
        /// </summary>
        Task<List<ClusterConnectionInfo>> DiscoverClustersAsync();

        /// <summary>
        /// Get a specific cluster by ID.
        /// </summary>
        Task<ClusterConnectionInfo?> GetClusterByIdAsync(string clusterId);

        /// <summary>
        /// Load clusters from a specific kubeconfig file.
        /// </summary>
        Task<List<ClusterConnectionInfo>> LoadKubeconfigAsync(string kubeconfigPath);

        /// <summary>
        /// Scan a directory for kubeconfig files and load all clusters.
        /// </summary>
        Task<List<ClusterConnectionInfo>> ScanDirectoryAsync(string directoryPath);

        /// <summary>
        /// Validate whether the provided kubeconfig content can be parsed.
        /// </summary>
        bool IsValidKubeconfig(Stream kubeconfigStream);
    }
}
