using System;
using System.Collections.Generic;
using System.Text;

namespace KonciergeUI.Models.Kube
{
    public record ServiceInfo
    {
        public required string Name { get; init; }
        public required string Namespace { get; init; }
        public required ServiceType Type { get; init; }
        public string? ClusterIp { get; init; }
        public List<ServicePort> Ports { get; init; } = new();
        public Dictionary<string, string> Selector { get; init; } = new();
        public DateTimeOffset? CreatedAt { get; init; }
        public List<string>? ExternalIPs { get; init; }
        public string? LoadBalancerIp { get; init; }
    }

    public record ServicePort
    {
        public required string Name { get; init; }
        public required int Port { get; init; }
        public required int TargetPort { get; init; }
        public string Protocol { get; init; } = "TCP";
        public int? NodePort { get; init; }
    }

    public enum ServiceType
    {
        ClusterIP,
        NodePort,
        LoadBalancer,
        ExternalName
    }
}
