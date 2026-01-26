using KonciergeUI.Models.Kube;
using System;
using System.Collections.Generic;
using System.Text;
using k8s;
using k8s.Models;

namespace KonciergeUI.Core.Abstractions
{
    public interface IKubeRepository
    {
        
         /// <summary>
        /// List all pods in a cluster (optionally filter by namespace).
        /// </summary>
        IKubernetes GetClient(ClusterConnectionInfo cluster);
        
        
        /// <summary>
        /// List all pods in a cluster (optionally filter by namespace).
        /// </summary>
        Task<List<PodInfo>> ListPodsAsync(ClusterConnectionInfo cluster, string? namespaceFilter = null);

        /// <summary>
        /// List all services in a cluster (optionally filter by namespace).
        /// </summary>
        Task<List<ServiceInfo>> ListServicesAsync(ClusterConnectionInfo cluster, string? namespaceFilter = null);

        /// <summary>
        /// Get detailed info for a specific pod.
        /// </summary>
        Task<PodInfo?> GetPodAsync(ClusterConnectionInfo cluster, string podName, string @namespace);

        /// <summary>
        /// Get detailed info for a specific service.
        /// </summary>
        Task<ServiceInfo?> GetServiceAsync(ClusterConnectionInfo cluster, string serviceName, string @namespace);
        
        Task<List<SecretInfo>> ListSecretsAsync(ClusterConnectionInfo cluster, string @namespace);
        Task<List<ConfigMapInfo>> ListConfigMapsAsync(ClusterConnectionInfo cluster, string @namespace);

        Task<SecretInfo> GetSecretAsync(ClusterConnectionInfo cluster, string @namespace, string name);
        Task<ConfigMapInfo> GetConfigMapAsync(ClusterConnectionInfo cluster, string @namespace, string name);
    }
}
