﻿using k8s;
using k8s.Models;
using KonciergeUI.Core.Abstractions;
using KonciergeUI.Models.Kube;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;



namespace KonciergeUI.Kube.Repositories;

public class KubeRepository : IKubeRepository
{
    public IKubernetes GetClient(ClusterConnectionInfo cluster)=>CreateClient(cluster);

    public async Task<List<string>> ListNamespacesAsync(ClusterConnectionInfo cluster)
    {
        var namespaces = new List<string>();

        try
        {
            var client = CreateClient(cluster);
            var nsList = await client.CoreV1.ListNamespaceAsync();

            foreach (var ns in nsList.Items)
            {
                if (!string.IsNullOrEmpty(ns.Metadata?.Name))
                {
                    namespaces.Add(ns.Metadata.Name);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to list namespaces: {ex.Message}");
            throw;
        }

        return namespaces.OrderBy(n => n).ToList();
    }

    public async Task<List<PodInfo>> ListPodsAsync(ClusterConnectionInfo cluster, string? namespaceFilter = null)
    {
        var pods = new List<PodInfo>();

        try
        {
            var client = CreateClient(cluster);

            V1PodList podList;

            if (!string.IsNullOrEmpty(namespaceFilter))
            {
                podList = await client.CoreV1.ListNamespacedPodAsync(namespaceFilter);
            }
            else
            {
                podList = await client.CoreV1.ListPodForAllNamespacesAsync();
            }

            foreach (var pod in podList.Items)
            {
                pods.Add(MapPodInfo(pod));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to list pods: {ex.Message}");
            throw;
        }

        return pods;
    }

    public async Task<List<ServiceInfo>> ListServicesAsync(ClusterConnectionInfo cluster, string? namespaceFilter = null)
    {
        var services = new List<ServiceInfo>();

        try
        {
            var client = CreateClient(cluster);

            V1ServiceList serviceList;

            if (!string.IsNullOrEmpty(namespaceFilter))
            {
                serviceList = await client.CoreV1.ListNamespacedServiceAsync(namespaceFilter);
            }
            else
            {
                serviceList = await client.CoreV1.ListServiceForAllNamespacesAsync();
            }

            foreach (var service in serviceList.Items)
            {
                services.Add(MapServiceInfo(service));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to list services: {ex.Message}");
            throw;
        }

        return services;
    }

    public async Task<PodInfo?> GetPodAsync(ClusterConnectionInfo cluster, string podName, string @namespace)
    {
        try
        {
            var client = CreateClient(cluster);
            var pod = await client.CoreV1.ReadNamespacedPodAsync(podName, @namespace);
            return MapPodInfo(pod);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get pod {podName}: {ex.Message}");
            return null;
        }
    }

    public async Task<ServiceInfo?> GetServiceAsync(ClusterConnectionInfo cluster, string serviceName, string @namespace)
    {
        try
        {
            var client = CreateClient(cluster);
            var service = await client.CoreV1.ReadNamespacedServiceAsync(serviceName, @namespace);
            return MapServiceInfo(service);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get service {serviceName}: {ex.Message}");
            return null;
        }
    }

    private IKubernetes CreateClient(ClusterConnectionInfo cluster)
    {
        var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(
            kubeconfigPath: cluster.KubeconfigPath,
            currentContext: cluster.ContextName
        );

        return new Kubernetes(config);
    }

    private PodInfo MapPodInfo(V1Pod pod)
    {
        var status = MapPodStatus(pod.Status?.Phase, pod.Status?.ContainerStatuses);
        var ports = MapContainerPorts(pod.Spec?.Containers);
        var startTime = pod.Status?.StartTime ?? pod.Metadata?.CreationTimestamp;
        var restartCount = pod.Status?.ContainerStatuses
            ?.Sum(c => c.RestartCount) ?? 0;

        return new PodInfo
        {
            Name = pod.Metadata?.Name ?? "unknown",
            Namespace = pod.Metadata?.NamespaceProperty ?? "default",
            Status = status,
            Phase = pod.Status?.Phase,
            Ports = ports,
            StartTime = startTime,
            Labels = pod.Metadata?.Labels?.ToDictionary(kv => kv.Key, kv => kv.Value)
                ?? new Dictionary<string, string>(),
            NodeName = pod.Spec?.NodeName,
            RestartCount = (int)restartCount
        };
    }

    private ServiceInfo MapServiceInfo(V1Service service)
    {
        var serviceType = MapServiceType(service.Spec?.Type);
        var ports = MapServicePorts(service.Spec?.Ports);
        var selector = service.Spec?.Selector?.ToDictionary(kv => kv.Key, kv => kv.Value)
            ?? new Dictionary<string, string>();

        var externalIPs = service.Status?.LoadBalancer?.Ingress
            ?.Select(i => i.Ip ?? i.Hostname)
            .Where(ip => !string.IsNullOrEmpty(ip))
            .ToList();

        var loadBalancerIp = externalIPs?.FirstOrDefault();

        return new ServiceInfo
        {
            Name = service.Metadata?.Name ?? "unknown",
            Namespace = service.Metadata?.NamespaceProperty ?? "default",
            Type = serviceType,
            ClusterIp = service.Spec?.ClusterIP,
            Ports = ports,
            Selector = selector,
            CreatedAt = service.Metadata?.CreationTimestamp,
            ExternalIPs = externalIPs?.Count > 0 ? externalIPs : null,
            LoadBalancerIp = loadBalancerIp
        };
    }

    private PodStatus MapPodStatus(string? phase, IList<V1ContainerStatus>? containerStatuses)
    {
        // Check container states first
        if (containerStatuses != null)
        {
            foreach (var container in containerStatuses)
            {
                if (container.State?.Waiting != null)
                {
                    var reason = container.State.Waiting.Reason;
                    if (reason == "CrashLoopBackOff")
                        return PodStatus.CrashLoopBackOff;
                    if (reason == "ImagePullBackOff" || reason == "ErrImagePull")
                        return PodStatus.ImagePullBackOff;
                }

                if (container.State?.Terminated != null)
                {
                    return PodStatus.Failed;
                }
            }
        }

        // Map phase to status
        return phase switch
        {
            "Running" => PodStatus.Running,
            "Pending" => PodStatus.Pending,
            "Succeeded" => PodStatus.Succeeded,
            "Failed" => PodStatus.Failed,
            _ => PodStatus.Unknown
        };
    }

    private ServiceType MapServiceType(string? type)
    {
        return type switch
        {
            "ClusterIP" => ServiceType.ClusterIP,
            "NodePort" => ServiceType.NodePort,
            "LoadBalancer" => ServiceType.LoadBalancer,
            "ExternalName" => ServiceType.ExternalName,
            _ => ServiceType.ClusterIP
        };
    }

    private List<ContainerPort> MapContainerPorts(IList<V1Container>? containers)
    {
        var ports = new List<ContainerPort>();

        if (containers == null)
            return ports;

        foreach (var container in containers)
        {
            if (container.Ports == null)
                continue;

            foreach (var port in container.Ports)
            {
                ports.Add(new ContainerPort
                {
                    Name = port.Name ?? $"port-{port.ContainerPort}",
                    Port = port.ContainerPort,
                    Protocol = port.Protocol ?? "TCP",
                    ContainerName = container.Name
                });
            }
        }

        return ports;
    }

    private List<ServicePort> MapServicePorts(IList<V1ServicePort>? ports)
    {
        var servicePorts = new List<ServicePort>();

        if (ports == null)
            return servicePorts;

        foreach (var port in ports)
        {
            // Parse TargetPort - it can be either an int or a string (named port)
            int targetPort = port.Port; // Default to service port

            if (port.TargetPort != null)
            {
                if (int.TryParse(port.TargetPort.Value, out var parsed))
                {
                    targetPort = parsed;
                }
                else
                {
                    // It's a named port, use the service port as fallback
                    targetPort = port.Port;
                }
            }

            servicePorts.Add(new ServicePort
            {
                Name = port.Name ?? $"port-{port.Port}",
                Port = port.Port,
                TargetPort = targetPort,
                Protocol = port.Protocol ?? "TCP",
                NodePort = port.NodePort
            });
        }

        return servicePorts;
    }
    
    public async Task<List<SecretInfo>> ListSecretsAsync(ClusterConnectionInfo cluster, string @namespace)
    {
        try
        {
            var client = CreateClient(cluster);
            var list = await client.CoreV1.ListNamespacedSecretAsync(@namespace);
            // Filter out service-account-token etc if you want:
            var rawSecrets= list.Items
                .Where(s => s.Type != "kubernetes.io/service-account-token")
                .OrderBy(s => s.Metadata?.Name)
                .ToList();

            return rawSecrets.Select(s=> new SecretInfo { 
                Name=s.Metadata.Name,
                NameSpace=s.Metadata.Namespace(),
                Data = s.Data?
    .ToDictionary(
        kvp => kvp.Key,
        kvp => Encoding.UTF8.GetString(kvp.Value)
    ) ?? new Dictionary<string, string>()
            }
            ).ToList();

           


        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to list secrets in {@namespace}: {ex.Message}");
            return new List<SecretInfo>();
        }
    }

    public async Task<List<ConfigMapInfo>> ListConfigMapsAsync(ClusterConnectionInfo cluster, string @namespace)
    {
        try
        {
            var client = CreateClient(cluster);
            var list = await client.CoreV1.ListNamespacedConfigMapAsync(@namespace);
            var rawConfigs = list.Items
               .OrderBy(c => c.Metadata?.Name)
               .ToList();

            return rawConfigs.Select(c => new ConfigMapInfo
            {
                Name = c.Metadata.Name,
                NameSpace = c.Metadata.Namespace(),
                Data=c.Data.ToDictionary()
            }
           ).ToList();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to list configmaps in {@namespace}: {ex.Message}");
            return new List<ConfigMapInfo>();
        }
    }

    public async Task<SecretInfo?> GetSecretAsync(ClusterConnectionInfo cluster, string @namespace, string name)
    {
        var list = await ListSecretsAsync(cluster, @namespace);

        var ret = list.FirstOrDefault(s => s.Name == name);


        return ret;
    }

    public async Task<ConfigMapInfo?> GetConfigMapAsync(ClusterConnectionInfo cluster, string @namespace, string name)
    {
        var list = await ListConfigMapsAsync(cluster, @namespace);

        var ret = list.FirstOrDefault(s => s.Name == name);


        return ret;
    }


}
