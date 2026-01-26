using k8s;
using KonciergeUI.Core.Abstractions;
using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models.Kube;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using static KonciergeUI.Models.Forwarding.Enums;

namespace KonciergeUI.Kube
{
    public class PortForwardingService : IPortForwardingService, IDisposable
    {
        private readonly ILogger<PortForwardingService> _logger;
        private readonly ConcurrentDictionary<Guid, RunningTemplate> _runningTemplates = new();
        private readonly ConcurrentDictionary<Guid, ActiveForward> _activeForwards = new();
        private readonly ConcurrentDictionary<Guid, IKubernetes> _k8sClients = new(); // Track clients per template

        public PortForwardingService(ILogger<PortForwardingService> logger)
        {
            _logger = logger;
        }

        public async Task<RunningTemplate> StartTemplateAsync(
            ForwardTemplate template,
            ClusterConnectionInfo cluster,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Starting template {TemplateName} ({TemplateId}) on cluster {ClusterName} ({ClusterContext})",
                template.Name, template.Id, cluster.Name, cluster.ContextName);

            // Create K8s client for this specific cluster
            IKubernetes k8sClient;
            try
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(
                    kubeconfigPath: cluster.KubeconfigPath,
                    currentContext: cluster.ContextName
                );
                k8sClient = new Kubernetes(config);

                // Test connection
                await k8sClient.CoreV1.ListNamespaceAsync(cancellationToken: cancellationToken);

                _logger.LogInformation("Connected to cluster {ClusterName}", cluster.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to cluster {ClusterName}", cluster.Name);
                throw new InvalidOperationException($"Cannot connect to cluster {cluster.Name}: {ex.Message}", ex);
            }

            var forwardInstances = new List<ForwardInstance>();
            var activeForwards = new List<ActiveForward>();

            try
            {
                foreach (var definition in template.Forwards)
                {
                    var instance = new ForwardInstance
                    {
                        Id = Guid.CreateVersion7(),
                        Name = definition.Name,
                        Definition = definition,
                        Status = ForwardStatus.Starting
                    };

                    forwardInstances.Add(instance);

                    var activeForward = await StartForwardAsync(instance, k8sClient, cancellationToken);
                    activeForwards.Add(activeForward);
                    _activeForwards[instance.Id] = activeForward;

                    instance.Status = ForwardStatus.Running;
                    instance.BoundLocalPort = activeForward.BoundPort;
                }

                var runningTemplate = new RunningTemplate
                {
                    TemplateId = template.Id,
                    Definition = template,
                    Forwards = forwardInstances.AsReadOnly(),
                    StartedAt = DateTimeOffset.UtcNow,
                    ClusterInfo = cluster // Store cluster info with the running template
                };

                _runningTemplates[template.Id] = runningTemplate;
                _k8sClients[template.Id] = k8sClient; // Keep client alive for this template

                _logger.LogInformation(
                    "Template {TemplateName} started successfully on {ClusterName} with {Count} forwards",
                    template.Name, cluster.Name, forwardInstances.Count);

                return runningTemplate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start template {TemplateName} on {ClusterName}",
                    template.Name, cluster.Name);

                // Cleanup any started forwards
                foreach (var activeForward in activeForwards)
                {
                    await activeForward.StopAsync();
                }

                // Dispose the K8s client
                k8sClient.Dispose();

                throw;
            }
        }

        public async Task StopTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
        {
            if (!_runningTemplates.TryRemove(templateId, out var template))
            {
                _logger.LogWarning("Template {TemplateId} not found", templateId);
                return;
            }

            _logger.LogInformation("Stopping template {TemplateName}", template.Definition.Name);

            var stopTasks = template.Forwards
                .Select(async forward =>
                {
                    if (_activeForwards.TryRemove(forward.Id, out var activeForward))
                    {
                        await activeForward.StopAsync();
                        forward.Status = ForwardStatus.Stopped;
                        forward.StoppedAt = DateTimeOffset.UtcNow;
                    }
                });

            await Task.WhenAll(stopTasks);

            // Dispose the K8s client for this template
            if (_k8sClients.TryRemove(templateId, out var k8sClient))
            {
                k8sClient.Dispose();
                _logger.LogDebug("Disposed K8s client for template {TemplateId}", templateId);
            }

            _logger.LogInformation("Template {TemplateName} stopped", template.Definition.Name);
        }

        public Task<RunningTemplate?> GetRunningTemplateAsync(Guid templateId)
        {
            _runningTemplates.TryGetValue(templateId, out var template);
            return Task.FromResult(template);
        }

        public IReadOnlyCollection<RunningTemplate> GetAllRunningTemplates()
        {
            return _runningTemplates.Values.ToList().AsReadOnly();
        }

        public Task<IReadOnlyCollection<string>> GetForwardLogsAsync(Guid forwardInstanceId, int maxLines = 100)
        {
            if (_activeForwards.TryGetValue(forwardInstanceId, out var activeForward))
            {
                return Task.FromResult(activeForward.GetLogs(maxLines));
            }

            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        private async Task<ActiveForward> StartForwardAsync(
            ForwardInstance instance,
            IKubernetes k8sClient,
            CancellationToken cancellationToken)
        {
            var definition = instance.Definition;

            // Resolve pod name if targeting a service
            string podName = definition.ResourceType == ResourceType.Service
                ? await ResolvePodFromServiceAsync(definition.ResourceName, definition.Namespace, k8sClient, cancellationToken)
                : definition.ResourceName;

            _logger.LogInformation("Starting forward {ForwardName} to pod {PodName} port {Port}",
                definition.Name, podName, definition.TargetPort);

            var activeForward = new ActiveForward(
                instance.Id,
                podName,
                definition.Namespace,
                definition.TargetPort,
                definition.LocalPort,
                k8sClient,
                _logger
            );

            await activeForward.StartAsync(cancellationToken);

            return activeForward;
        }

        private async Task<string> ResolvePodFromServiceAsync(
            string serviceName,
            string @namespace,
            IKubernetes k8sClient,
            CancellationToken cancellationToken)
        {
            try
            {
                var service = await k8sClient.CoreV1.ReadNamespacedServiceAsync(
                    serviceName,
                    @namespace,
                    cancellationToken: cancellationToken);

                if (service.Spec?.Selector == null || !service.Spec.Selector.Any())
                {
                    throw new InvalidOperationException($"Service {serviceName} has no selector");
                }

                var labelSelector = string.Join(",", service.Spec.Selector.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                var pods = await k8sClient.CoreV1.ListNamespacedPodAsync(
                    @namespace,
                    labelSelector: labelSelector,
                    cancellationToken: cancellationToken
                );

                var runningPod = pods.Items.FirstOrDefault(p => p.Status?.Phase == "Running");
                if (runningPod == null)
                {
                    throw new InvalidOperationException($"No running pods found for service {serviceName}");
                }

                _logger.LogInformation("Resolved service {ServiceName} to pod {PodName}",
                    serviceName, runningPod.Metadata.Name);
                return runningPod.Metadata.Name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve pod from service {ServiceName}", serviceName);
                throw;
            }
        }

        public void Dispose()
        {
            foreach (var activeForward in _activeForwards.Values)
            {
                activeForward.StopAsync().GetAwaiter().GetResult();
            }

            foreach (var k8sClient in _k8sClients.Values)
            {
                k8sClient.Dispose();
            }

            _activeForwards.Clear();
            _runningTemplates.Clear();
            _k8sClients.Clear();
        }
    }

}
