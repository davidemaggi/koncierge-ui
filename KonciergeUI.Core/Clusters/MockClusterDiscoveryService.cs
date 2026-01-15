using KonciergeUI.Models.Kube;
using System;
using System.Collections.Generic;
using System.Text;

namespace KonciergeUI.Core.Clusters
{
    public class MockClusterDiscoveryService : IClusterDiscoveryService
    {
        private static readonly List<ClusterConnectionInfo> _mockClusters = new();

        static MockClusterDiscoveryService()
        {
            // Initialize mock data once
            _mockClusters = GenerateMockClusters();
        }

        public Task<List<ClusterConnectionInfo>> DiscoverClustersAsync()
        {
            return Task.FromResult(_mockClusters);
        }

        public Task<ClusterConnectionInfo?> GetClusterByIdAsync(string clusterId)
        {
            var cluster = _mockClusters.FirstOrDefault(c => c.Id == clusterId);
            return Task.FromResult(cluster);
        }

        public Task<List<ClusterConnectionInfo>> LoadKubeconfigAsync(string kubeconfigPath)
        {
            var clusters = _mockClusters
                .Where(c => c.KubeconfigPath == kubeconfigPath)
                .ToList();

            return Task.FromResult(clusters);
        }

        public Task<List<ClusterConnectionInfo>> ScanDirectoryAsync(string directoryPath)
        {
            // Mock scanning a directory - return custom kubeconfig clusters
            var customClusters = _mockClusters
                .Where(c => !c.IsDefaultKubeconfig)
                .ToList();

            return Task.FromResult(customClusters);
        }

        private static List<ClusterConnectionInfo> GenerateMockClusters()
        {
            var clusters = new List<ClusterConnectionInfo>();

            // KUBECONFIG 1: Default (~/.kube/config) with 3 clusters
            clusters.Add(new ClusterConnectionInfo
            {
                Id = "default-dev",
                Name = "Development",
                KubeconfigPath = "~/.kube/config",
                ContextName = "dev-context",
                ClusterUrl = "https://dev-k8s.company.local:6443",
                DefaultNamespace = "default",
                UserName = "dev-admin",
                IsCurrentContext = true,
                IsDefaultKubeconfig = true,
                Description = "Development cluster",
                LastConnected = DateTimeOffset.Now.AddHours(-2)
            });

            clusters.Add(new ClusterConnectionInfo
            {
                Id = "default-staging",
                Name = "Staging",
                KubeconfigPath = "~/.kube/config",
                ContextName = "staging-context",
                ClusterUrl = "https://staging-k8s.company.local:6443",
                DefaultNamespace = "staging",
                UserName = "staging-admin",
                IsCurrentContext = false,
                IsDefaultKubeconfig = true,
                Description = "Staging environment",
                LastConnected = DateTimeOffset.Now.AddDays(-1)
            });

            clusters.Add(new ClusterConnectionInfo
            {
                Id = "default-prod",
                Name = "Production",
                KubeconfigPath = "~/.kube/config",
                ContextName = "prod-context",
                ClusterUrl = "https://prod-k8s.company.local:6443",
                DefaultNamespace = "prod",
                UserName = "prod-viewer",
                IsCurrentContext = false,
                IsDefaultKubeconfig = true,
                Description = "Production cluster (read-only)",
                LastConnected = DateTimeOffset.Now.AddDays(-3)
            });

            // KUBECONFIG 2: Custom (~/projects/backend-cluster.yaml) with 3 clusters
            clusters.Add(new ClusterConnectionInfo
            {
                Id = "backend-dev",
                Name = "Backend Dev",
                KubeconfigPath = "~/projects/backend-cluster.yaml",
                ContextName = "backend-dev",
                ClusterUrl = "https://192.168.1.100:6443",
                DefaultNamespace = "backend-dev",
                UserName = "developer",
                IsCurrentContext = false,
                IsDefaultKubeconfig = false,
                Description = "Backend microservices dev",
                LastConnected = DateTimeOffset.Now.AddMinutes(-30)
            });

            clusters.Add(new ClusterConnectionInfo
            {
                Id = "backend-test",
                Name = "Backend Test",
                KubeconfigPath = "~/projects/backend-cluster.yaml",
                ContextName = "backend-test",
                ClusterUrl = "https://192.168.1.101:6443",
                DefaultNamespace = "backend-test",
                UserName = "tester",
                IsCurrentContext = true,
                IsDefaultKubeconfig = false,
                Description = "Backend testing environment",
                LastConnected = DateTimeOffset.Now.AddHours(-1)
            });

            clusters.Add(new ClusterConnectionInfo
            {
                Id = "backend-integration",
                Name = "Backend Integration",
                KubeconfigPath = "~/projects/backend-cluster.yaml",
                ContextName = "backend-integration",
                ClusterUrl = "https://192.168.1.102:6443",
                DefaultNamespace = "integration",
                UserName = "ci-cd",
                IsCurrentContext = false,
                IsDefaultKubeconfig = false,
                Description = "CI/CD integration tests",
                LastConnected = null // Never connected
            });

            return clusters;
        }
    }
}
