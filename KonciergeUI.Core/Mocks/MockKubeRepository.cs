using KonciergeUI.Core.Abstractions;
using KonciergeUI.Models.Kube;
using k8s.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using k8s;

namespace KonciergeUI.Core.Mocks
{
    public class MockKubeRepository : IKubeRepository
    {
        private static readonly Random _random = new();

        // In-memory mock data for secrets/configmaps
        private readonly Dictionary<(string ClusterId, string Namespace), List<V1Secret>> _secrets = new();
        private readonly Dictionary<(string ClusterId, string Namespace), List<V1ConfigMap>> _configMaps = new();

        public MockKubeRepository()
        {
            SeedSecretsAndConfigMaps();
        }

        private void SeedSecretsAndConfigMaps()
        {
            // You can tune this to any namespaces you like
            var namespaces = new[] { "default", "backend", "frontend" };

            foreach (var ns in namespaces)
            {
                var key = ("mock-cluster", ns);

                _secrets[key] = new List<V1Secret>
                {
                    new V1Secret()
                    {
                        Metadata = new V1ObjectMeta(){Name =  "stripe-key",  NamespaceProperty = ns},
                        Type = "Opaque"
                        
                    },
                    new V1Secret()
                    {
                        Metadata = new V1ObjectMeta(){Name =  "sendgrid-key",  NamespaceProperty = ns},
                        Type = "Opaque"
                        
                    },
                    new V1Secret()
                    {
                        Metadata = new V1ObjectMeta(){Name =  "jwt-secret",  NamespaceProperty = ns},
                        Type = "Opaque"
                        
                    }
                    
                };

                _configMaps[key] = new List<V1ConfigMap>
                {
                    new V1ConfigMap(){Metadata = new V1ObjectMeta(){Name ="api-url",  NamespaceProperty = ns}, Data = new Dictionary<string, string> { ["API_URL"] = "https://api.example.com" }},
                    new V1ConfigMap(){Metadata = new V1ObjectMeta(){Name ="environment",  NamespaceProperty = ns}, Data = new Dictionary<string, string> { ["ENV"] = "Development" }},
                    new V1ConfigMap(){Metadata = new V1ObjectMeta(){Name ="new-ui",  NamespaceProperty = ns}, Data = new Dictionary<string, string> { ["NEW_UI_ENABLED"] = "true" }},
                    
                };
            }
        }

        private static string GetClusterKey(ClusterConnectionInfo cluster)
            => string.IsNullOrWhiteSpace(cluster.Id) ? "mock-cluster" : cluster.Id;

        public IKubernetes GetClient(ClusterConnectionInfo cluster)
        {
            throw new NotImplementedException();
        }

        public Task<List<PodInfo>> ListPodsAsync(ClusterConnectionInfo cluster, string? namespaceFilter = null)
        {
            var pods = GenerateMockPods(cluster.Id);

            if (!string.IsNullOrEmpty(namespaceFilter))
            {
                pods = pods.Where(p => p.Namespace == namespaceFilter).ToList();
            }

            return Task.FromResult(pods);
        }

        public Task<List<ServiceInfo>> ListServicesAsync(ClusterConnectionInfo cluster, string? namespaceFilter = null)
        {
            var services = GenerateMockServices(cluster.Id);

            if (!string.IsNullOrEmpty(namespaceFilter))
            {
                services = services.Where(s => s.Namespace == namespaceFilter).ToList();
            }

            return Task.FromResult(services);
        }

        public Task<PodInfo?> GetPodAsync(ClusterConnectionInfo cluster, string podName, string @namespace)
        {
            var pods = GenerateMockPods(cluster.Id);
            var pod = pods.FirstOrDefault(p => p.Name == podName && p.Namespace == @namespace);
            return Task.FromResult(pod);
        }

        public Task<ServiceInfo?> GetServiceAsync(ClusterConnectionInfo cluster, string serviceName, string @namespace)
        {
            var services = GenerateMockServices(cluster.Id);
            var service = services.FirstOrDefault(s => s.Name == serviceName && s.Namespace == @namespace);
            return Task.FromResult(service);
        }

        public Task<List<SecretInfo>> ListSecretsAsync(ClusterConnectionInfo cluster, string @namespace)
        {
            var key = (GetClusterKey(cluster), @namespace);
            _secrets.TryGetValue(key, out var list);
            list ??= new List<V1Secret>();
            // Filter service-account tokens etc. if you ever add them.
            list = list
                .Where(s => !string.Equals(s.Type, "kubernetes.io/service-account-token", StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Metadata?.Name)
                .ToList();

            return Task.FromResult(list.Select(s => new SecretInfo
            {
                Name = s.Metadata.Name,
                NameSpace = s.Metadata.Namespace(),
                Data = s.Data?
    .ToDictionary(
        kvp => kvp.Key,
        kvp => Encoding.UTF8.GetString(kvp.Value)
    ) ?? new Dictionary<string, string>()
            }
            ).ToList());
        }

        public Task<List<ConfigMapInfo>> ListConfigMapsAsync(ClusterConnectionInfo cluster, string @namespace)
        {
            var key = (GetClusterKey(cluster), @namespace);
            _configMaps.TryGetValue(key, out var list);
            list ??= new List<V1ConfigMap>();
            list = list
                .OrderBy(c => c.Metadata?.Name)
                .ToList();


            return Task.FromResult(list.Select(c => new ConfigMapInfo
            {
                Name = c.Metadata.Name,
                NameSpace = c.Metadata.Namespace(),
                Data = c.Data.ToDictionary()
            }
         ).ToList());

        }

        // ----------------- existing mock generators -----------------

        private List<PodInfo> GenerateMockPods(string clusterId)
        {
            var pods = new List<PodInfo>();
            var namespaces = new[] { "default", "backend", "frontend", "database", "monitoring", "cache", "ingress" };
            var appTypes = new[] { "api", "worker", "web", "db", "redis", "nginx", "prometheus", "grafana", "rabbitmq", "kafka" };
            var statuses = new[] { PodStatus.Running, PodStatus.Running, PodStatus.Running, PodStatus.Pending, PodStatus.CrashLoopBackOff };

            for (int i = 0; i < 25; i++)
            {
                var ns = namespaces[_random.Next(namespaces.Length)];
                var appType = appTypes[_random.Next(appTypes.Length)];
                var status = statuses[_random.Next(statuses.Length)];
                var suffix = GenerateRandomSuffix();

                var ports = GenerateRandomPorts(appType);

                pods.Add(new PodInfo
                {
                    Name = $"{appType}-{suffix}",
                    Namespace = ns,
                    Status = status,
                    Phase = status.ToString(),
                    Ports = ports,
                    StartTime = DateTimeOffset.Now.AddHours(-_random.Next(1, 168)),
                    Labels = new Dictionary<string, string>
                    {
                        ["app"] = appType,
                        ["version"] = $"v{_random.Next(1, 5)}",
                        ["tier"] = ns,
                        ["cluster"] = clusterId
                    },
                    NodeName = $"node-{_random.Next(1, 6)}",
                    RestartCount = status == PodStatus.CrashLoopBackOff ? _random.Next(5, 20) : _random.Next(0, 3)
                });
            }

            return pods.OrderBy(p => p.Namespace).ThenBy(p => p.Name).ToList();
        }

        private List<ServiceInfo> GenerateMockServices(string clusterId)
        {
            var services = new List<ServiceInfo>();
            var namespaces = new[] { "default", "backend", "frontend", "database", "monitoring", "cache", "ingress" };
            var serviceNames = new[]
            {
                "api-gateway", "auth-service", "user-service", "payment-service",
                "notification-service", "frontend-web", "admin-dashboard",
                "postgres", "mysql", "mongodb", "redis-master", "redis-replica",
                "rabbitmq", "kafka-broker", "elasticsearch", "prometheus",
                "grafana", "nginx-ingress", "traefik", "cert-manager"
            };
            var types = new[] { ServiceType.ClusterIP, ServiceType.NodePort, ServiceType.LoadBalancer };

            for (int i = 0; i < 22; i++)
            {
                var name = serviceNames[i % serviceNames.Length];
                var ns = namespaces[_random.Next(namespaces.Length)];
                var type = i < 15 ? ServiceType.ClusterIP : types[_random.Next(types.Length)];

                var ports = GenerateServicePorts(name, type);

                services.Add(new ServiceInfo
                {
                    Name = name + (i >= serviceNames.Length ? $"-{i}" : ""),
                    Namespace = ns,
                    Type = type,
                    ClusterIp = GenerateClusterIp(),
                    Ports = ports,
                    Selector = new Dictionary<string, string>
                    {
                        ["app"] = name.Split('-')[0],
                        ["tier"] = ns
                    },
                    CreatedAt = DateTimeOffset.Now.AddDays(-_random.Next(1, 90)),
                    ExternalIPs = type == ServiceType.LoadBalancer ? new List<string> { GenerateExternalIp() } : null,
                    LoadBalancerIp = type == ServiceType.LoadBalancer ? GenerateExternalIp() : null
                });
            }

            return services.OrderBy(s => s.Namespace).ThenBy(s => s.Name).ToList();
        }

        private List<ContainerPort> GenerateRandomPorts(string appType)
        {
            var ports = new List<ContainerPort>();

            var commonPorts = appType switch
            {
                "api" => new[] { (8080, "http"), (8443, "https") },
                "web" => new[] { (80, "http"), (443, "https") },
                "db" => new[] { (5432, "postgres"), (3306, "mysql") },
                "redis" => new[] { (6379, "redis") },
                "nginx" => new[] { (80, "http"), (443, "https") },
                "prometheus" => new[] { (9090, "prometheus") },
                "grafana" => new[] { (3000, "grafana") },
                "rabbitmq" => new[] { (5672, "amqp"), (15672, "management") },
                "kafka" => new[] { (9092, "kafka") },
                _ => new[] { (8080, "http") }
            };

            foreach (var (port, name) in commonPorts)
            {
                ports.Add(new ContainerPort
                {
                    Name = name,
                    Port = port,
                    Protocol = "TCP",
                    ContainerName = appType
                });
            }

            return ports;
        }

        private List<ServicePort> GenerateServicePorts(string serviceName, ServiceType type)
        {
            var ports = new List<ServicePort>();

            if (serviceName.Contains("postgres") || serviceName.Contains("mysql"))
            {
                ports.Add(new ServicePort
                {
                    Name = "db",
                    Port = serviceName.Contains("postgres") ? 5432 : 3306,
                    TargetPort = serviceName.Contains("postgres") ? 5432 : 3306,
                    Protocol = "TCP",
                    NodePort = type == ServiceType.NodePort ? 30000 + _random.Next(1000, 3000) : null
                });
            }
            else if (serviceName.Contains("redis"))
            {
                ports.Add(new ServicePort
                {
                    Name = "redis",
                    Port = 6379,
                    TargetPort = 6379,
                    Protocol = "TCP",
                    NodePort = type == ServiceType.NodePort ? 30000 + _random.Next(1000, 3000) : null
                });
            }
            else if (serviceName.Contains("rabbitmq"))
            {
                ports.Add(new ServicePort { Name = "amqp", Port = 5672, TargetPort = 5672, Protocol = "TCP" });
                ports.Add(new ServicePort { Name = "management", Port = 15672, TargetPort = 15672, Protocol = "TCP" });
            }
            else if (serviceName.Contains("kafka"))
            {
                ports.Add(new ServicePort { Name = "kafka", Port = 9092, TargetPort = 9092, Protocol = "TCP" });
            }
            else if (serviceName.Contains("grafana"))
            {
                ports.Add(new ServicePort { Name = "http", Port = 3000, TargetPort = 3000, Protocol = "TCP" });
            }
            else if (serviceName.Contains("prometheus"))
            {
                ports.Add(new ServicePort { Name = "http", Port = 9090, TargetPort = 9090, Protocol = "TCP" });
            }
            else
            {
                ports.Add(new ServicePort
                {
                    Name = "http",
                    Port = 80,
                    TargetPort = 8080,
                    Protocol = "TCP",
                    NodePort = type == ServiceType.NodePort ? 30000 + _random.Next(1000, 3000) : null
                });

                if (_random.Next(0, 2) == 0)
                {
                    ports.Add(new ServicePort
                    {
                        Name = "https",
                        Port = 443,
                        TargetPort = 8443,
                        Protocol = "TCP",
                        NodePort = type == ServiceType.NodePort ? 30000 + _random.Next(1000, 3000) : null
                    });
                }
            }

            return ports;
        }

        private string GenerateRandomSuffix()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Range(0, 10)
                .Select(_ => chars[_random.Next(chars.Length)])
                .ToArray());
        }

        private string GenerateClusterIp()
        {
            return $"10.96.{_random.Next(0, 255)}.{_random.Next(1, 255)}";
        }

        private string GenerateExternalIp()
        {
            return $"203.{_random.Next(0, 255)}.{_random.Next(0, 255)}.{_random.Next(1, 255)}";
        }
    }
}
