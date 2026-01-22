using k8s;
using KonciergeUI.Core.Abstractions;
using KonciergeUI.Models.Kube;
using System;
using System.Collections.Generic;
using System.Text;
using KonciergeUI.Data;

namespace KonciergeUI.Core.Clusters
{

    public class ClusterDiscoveryService : IClusterDiscoveryService
    {
        private readonly IPreferencesStorage _preferencesStorage;

        public ClusterDiscoveryService(IPreferencesStorage preferencesStorage)
        {
            _preferencesStorage = preferencesStorage;
        }

        public async Task<List<ClusterConnectionInfo>> DiscoverClustersAsync()
        {
            var clusters = new List<ClusterConnectionInfo>();

            // 1. Load default kubeconfig (~/.kube/config)
            clusters.AddRange(await LoadDefaultKubeconfigAsync());

            // 2. Load all kubeconfigs from default folder (~/.kube/)
            clusters.AddRange(await LoadKubeconfigsFromDefaultFolderAsync());

            // 3. Load custom kubeconfig paths from secure storage
            var customPaths = await _preferencesStorage.GetCustomKubeconfigPathsAsync();
            foreach (var path in customPaths)
            {
                clusters.AddRange(await LoadKubeconfigFromPathAsync(path, isDefault: false));
            }

            // Remove duplicates (same context + kubeconfig path)
            return clusters
                .GroupBy(c => $"{c.KubeconfigPath}_{c.ContextName}")
                .Select(g => g.First())
                .OrderByDescending(c => c.IsDefaultKubeconfig)
                .ThenBy(c => c.KubeconfigPath)
                .ThenBy(c => c.Name)
                .ToList();
        }

        public async Task<ClusterConnectionInfo?> GetClusterByIdAsync(string clusterId)
        {
            var allClusters = await DiscoverClustersAsync();
            return allClusters.FirstOrDefault(c => c.Id == clusterId);
        }

        public async Task<List<ClusterConnectionInfo>> LoadKubeconfigAsync(string kubeconfigPath)
        {
            return await LoadKubeconfigFromPathAsync(kubeconfigPath, isDefault: false);
        }

        public async Task<List<ClusterConnectionInfo>> ScanDirectoryAsync(string directoryPath)
        {
            var clusters = new List<ClusterConnectionInfo>();

            if (!Directory.Exists(directoryPath))
                return clusters;

            var files = Directory.GetFiles(directoryPath, "*", SearchOption.TopDirectoryOnly)
                .Where(f => !f.EndsWith(".lock") && !f.EndsWith(".bak"));

            foreach (var file in files)
            {
                try
                {
                    var fileClusters = await LoadKubeconfigFromPathAsync(file, isDefault: false);
                    clusters.AddRange(fileClusters);
                }
                catch
                {
                    // Skip invalid files
                }
            }

            return clusters;
        }

        private async Task<List<ClusterConnectionInfo>> LoadDefaultKubeconfigAsync()
        {
            var defaultPath = GetDefaultKubeconfigPath();

            if (!File.Exists(defaultPath))
                return new List<ClusterConnectionInfo>();

            return await LoadKubeconfigFromPathAsync(defaultPath, isDefault: true);
        }

        private async Task<List<ClusterConnectionInfo>> LoadKubeconfigsFromDefaultFolderAsync()
        {
            var defaultFolder = GetDefaultKubeconfigFolder();
            var clusters = new List<ClusterConnectionInfo>();

            if (!Directory.Exists(defaultFolder))
                return clusters;

            var files = Directory.GetFiles(defaultFolder)
                .Where(f =>
                    Path.GetFileName(f) != "config" && // Skip default config (already loaded)
                    !f.EndsWith(".lock") &&
                    !f.EndsWith(".bak") &&
                    !f.EndsWith(".tmp"))
                .ToList();

            foreach (var file in files)
            {
                try
                {
                    var fileClusters = await LoadKubeconfigFromPathAsync(file, isDefault: true);
                    clusters.AddRange(fileClusters);
                }
                catch
                {
                    // Skip invalid kubeconfig files
                }
            }

            return clusters;
        }

        private async Task<List<ClusterConnectionInfo>> LoadKubeconfigFromPathAsync(string path, bool isDefault)
        {
            var clusters = new List<ClusterConnectionInfo>();

            try
            {
                if (!File.Exists(path))
                    return clusters;

                // Load kubeconfig using official k8s client
                var kubeconfig =  KubernetesClientConfiguration.LoadKubeConfig(path);

                if (kubeconfig?.Contexts == null || !kubeconfig.Contexts.Any())
                    return clusters;

                var currentContext = kubeconfig.CurrentContext;

                foreach (var context in kubeconfig.Contexts)
                {
                    var cluster = kubeconfig.Clusters?.FirstOrDefault(c => c.Name == context.ContextDetails?.Cluster);
                    var user = kubeconfig.Users?.FirstOrDefault(u => u.Name == context.ContextDetails?.User);

                    if (cluster == null)
                        continue;

                    var clusterInfo = new ClusterConnectionInfo
                    {
                        Id = GenerateClusterId(path, context.Name),
                        Name = context.Name,
                        KubeconfigPath = path,
                        ContextName = context.Name,
                        ClusterUrl = cluster.ClusterEndpoint?.Server,
                        DefaultNamespace = context.ContextDetails?.Namespace ?? "default",
                        UserName = user?.Name,
                        IsCurrentContext = context.Name == currentContext,
                        IsDefaultKubeconfig = isDefault,
                        Description = null,
                        LastConnected = null
                    };

                    clusters.Add(clusterInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load kubeconfig from {path}: {ex.Message}");
            }

            return clusters;
        }

        private string GetHomeDirectory()
        {
            string homePath = (Environment.OSVersion.Platform == PlatformID.Unix ||
                    Environment.OSVersion.Platform == PlatformID.MacOSX)
     ? Environment.GetEnvironmentVariable("HOME")
     : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            return homePath;
        }


        private string GetDefaultKubeconfigPath()
        {
            return Path.Combine(GetHomeDirectory(), ".kube", "config");
        }

        private string GetDefaultKubeconfigFolder()
        {
            return Path.Combine(GetHomeDirectory(), ".kube");
        }


        private string GenerateClusterId(string kubeconfigPath, string contextName)
        {
            var combined = $"{kubeconfigPath}:{contextName}";
            var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(combined));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
