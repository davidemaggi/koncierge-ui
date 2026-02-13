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
        private readonly ConcurrentDictionary<Guid, IActiveForward> _activeForwards = new();
        private readonly ConcurrentDictionary<Guid, IKubernetes> _k8sClients = new(); // Track clients per template
        private readonly IKubeRepository _kube;

        public PortForwardingService(ILogger<PortForwardingService> logger, IKubeRepository kube)
        {
            _logger = logger;
            _kube = kube;
        }

        #region Template Management

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
            var activeForwards = new List<(Guid InstanceId, ActiveForward Forward)>();

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

                    // Resolve secrets using the extracted method
                    await ResolveSecretsForForwardAsync(instance, cluster, cancellationToken);

                    forwardInstances.Add(instance);

                    var activeForward = await StartForwardInternalAsync(instance, k8sClient, cancellationToken);
                    activeForwards.Add((instance.Id, activeForward));
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
                foreach (var (instanceId, activeForward) in activeForwards)
                {
                    try
                    {
                        if (_activeForwards.TryRemove(instanceId, out _))
                        {
                            await activeForward.StopAsync();
                        }
                        else
                        {
                            await activeForward.StopAsync();
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Error during cleanup of forward");
                    }
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
                        try
                        {
                            await activeForward.StopAsync();
                            forward.Status = ForwardStatus.Stopped;
                            forward.StoppedAt = DateTimeOffset.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error stopping forward {ForwardId}", forward.Id);
                            forward.Status = ForwardStatus.Failed;
                            forward.ErrorMessage = ex.Message;
                        }
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

        #endregion

        #region Individual Forward Management

        public async Task<bool> StartForwardAsync(Guid forwardInstanceId, CancellationToken cancellationToken = default)
        {
            var (template, forward) = FindForwardInstance(forwardInstanceId);

            if (template == null || forward == null)
            {
                _logger.LogWarning("Forward instance {ForwardId} not found", forwardInstanceId);
                return false;
            }

            if (forward.Status == ForwardStatus.Running)
            {
                _logger.LogInformation("Forward {ForwardName} ({ForwardId}) is already running",
                    forward.Name, forwardInstanceId);
                return true;
            }

            try
            {
                _logger.LogInformation("Starting forward {ForwardName} ({ForwardId})",
                    forward.Name, forwardInstanceId);

                forward.Status = ForwardStatus.Starting;
                forward.StartedAt = DateTimeOffset.UtcNow;
                forward.StoppedAt = null;
                forward.ErrorMessage = null;
                forward.ReconnectAttempts = 0;

                // Get the K8s client for this template
                if (!_k8sClients.TryGetValue(template.TemplateId, out var k8sClient))
                {
                    throw new InvalidOperationException(
                        $"K8s client not found for template {template.Definition.Name}");
                }

                // Resolve secrets if not already done or if they need refresh
                if (!forward.ResolvedSecrets.Any() && forward.Definition.LinkedSecrets.Any())
                {
                    await ResolveSecretsForForwardAsync(forward, template.ClusterInfo, cancellationToken);
                }

                // Start the actual port forward
                var activeForward = await StartForwardInternalAsync(forward, k8sClient, cancellationToken);
                _activeForwards[forwardInstanceId] = activeForward;

                forward.Status = ForwardStatus.Running;
                forward.BoundLocalPort = activeForward.BoundPort;

                _logger.LogInformation("Forward {ForwardName} ({ForwardId}) started successfully on port {Port}",
                    forward.Name, forwardInstanceId, forward.BoundLocalPort);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start forward {ForwardName} ({ForwardId})",
                    forward.Name, forwardInstanceId);

                forward.Status = ForwardStatus.Failed;
                forward.ErrorMessage = ex.Message;
                forward.StoppedAt = DateTimeOffset.UtcNow;

                return false;
            }
        }

        public async Task<bool> StopForwardAsync(Guid forwardInstanceId, CancellationToken cancellationToken = default)
        {
            var (template, forward) = FindForwardInstance(forwardInstanceId);

            if (template == null || forward == null)
            {
                _logger.LogWarning("Forward instance {ForwardId} not found", forwardInstanceId);
                return false;
            }

            if (forward.Status == ForwardStatus.Stopped)
            {
                _logger.LogInformation("Forward {ForwardName} ({ForwardId}) is already stopped",
                    forward.Name, forwardInstanceId);
                return true;
            }

            try
            {
                _logger.LogInformation("Stopping forward {ForwardName} ({ForwardId})",
                    forward.Name, forwardInstanceId);

                forward.Status = ForwardStatus.Stopping;

                // Stop the active forward
                if (_activeForwards.TryRemove(forwardInstanceId, out var activeForward))
                {
                    await activeForward.StopAsync();
                }

                forward.Status = ForwardStatus.Stopped;
                forward.StoppedAt = DateTimeOffset.UtcNow;
                forward.BoundLocalPort = null;

                _logger.LogInformation("Forward {ForwardName} ({ForwardId}) stopped successfully",
                    forward.Name, forwardInstanceId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop forward {ForwardName} ({ForwardId})",
                    forward.Name, forwardInstanceId);

                forward.Status = ForwardStatus.Failed;
                forward.ErrorMessage = ex.Message;

                return false;
            }
        }

        public async Task<bool> RestartForwardAsync(Guid forwardInstanceId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Restarting forward {ForwardId}", forwardInstanceId);

            var stopResult = await StopForwardAsync(forwardInstanceId, cancellationToken);
            if (!stopResult)
            {
                _logger.LogWarning("Failed to stop forward {ForwardId}, cannot restart", forwardInstanceId);
                return false;
            }

            // Brief delay to ensure cleanup
            await Task.Delay(500, cancellationToken);

            return await StartForwardAsync(forwardInstanceId, cancellationToken);
        }

        public Task<ForwardStatus?> GetForwardStatusAsync(Guid forwardInstanceId)
        {
            var (_, forward) = FindForwardInstance(forwardInstanceId);
            return Task.FromResult(forward?.Status);
        }

        public ForwardInstance? GetForwardInstance(Guid forwardInstanceId)
        {
            var (_, forward) = FindForwardInstance(forwardInstanceId);
            return forward;
        }

        #endregion

        #region Logs

        public Task<IReadOnlyCollection<string>> GetForwardLogsAsync(Guid forwardInstanceId, int maxLines = 100)
        {
            if (_activeForwards.TryGetValue(forwardInstanceId, out var activeForward))
            {
                return Task.FromResult(activeForward.GetLogs(maxLines));
            }

            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Find which template contains a specific forward instance.
        /// </summary>
        private (RunningTemplate? template, ForwardInstance? forward) FindForwardInstance(Guid forwardInstanceId)
        {
            foreach (var template in _runningTemplates.Values)
            {
                var forward = template.Forwards.FirstOrDefault(f => f.Id == forwardInstanceId);
                if (forward != null)
                {
                    return (template, forward);
                }
            }
            return (null, null);
        }

        /// <summary>
        /// Resolve secrets and configmaps for a forward instance.
        /// </summary>
        private async Task ResolveSecretsForForwardAsync(
            ForwardInstance forward,
            ClusterConnectionInfo cluster,
            CancellationToken cancellationToken)
        {
            var definition = forward.Definition;

            if (!definition.LinkedSecrets.Any())
            {
                return;
            }

            var nsSecrets = new List<SecretInfo>();
            var nsConfigs = new List<ConfigMapInfo>();

            try
            {
                if (definition.LinkedSecrets.Any(x => x.SourceType == Models.Security.SecretSourceType.Secret))
                {
                    nsSecrets = await _kube.ListSecretsAsync(cluster, definition.Namespace);
                }

                if (definition.LinkedSecrets.Any(x => x.SourceType == Models.Security.SecretSourceType.ConfigMap))
                {
                    nsConfigs = await _kube.ListConfigMapsAsync(cluster, definition.Namespace);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list secrets/configmaps for forward {ForwardName}", forward.Name);
                throw;
            }

            forward.ResolvedSecrets.Clear(); // Clear any existing

            foreach (var secret in definition.LinkedSecrets)
            {
                var tmp = new ResolvedSecret()
                {
                    Reference = secret,
                    Value = null
                };

                try
                {
                    if (secret.SourceType == Models.Security.SecretSourceType.ConfigMap)
                    {
                        var cm = nsConfigs.FirstOrDefault(cm => cm.Name == secret.ResourceName);
                        if (cm != null && cm.Data.TryGetValue(secret.Key, out var configValue))
                        {
                            tmp.Value = configValue;
                        }
                        else
                        {
                            _logger.LogWarning(
                                "ConfigMap {ResourceName} or key {Key} not found in namespace {Namespace}",
                                secret.ResourceName, secret.Key, secret.Namespace);
                        }
                    }
                    else if (secret.SourceType == Models.Security.SecretSourceType.Secret)
                    {
                        var s = nsSecrets.FirstOrDefault(s => s.Name == secret.ResourceName);
                        if (s != null && s.Data.TryGetValue(secret.Key, out var secretValue))
                        {
                            if (secretValue != null)
                            {
                                tmp.Value = secretValue;
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Secret {ResourceName} or key {Key} not found in namespace {Namespace}",
                                secret.ResourceName, secret.Key, secret.Namespace);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error retrieving Secret/ConfigMap {Namespace}.{ResourceName}",
                        secret.Namespace, secret.ResourceName);
                }

                forward.ResolvedSecrets.Add(tmp);
            }

            _logger.LogInformation("Resolved {Count} secrets for forward {ForwardName}",
                forward.ResolvedSecrets.Count, forward.Name);
        }

        /// <summary>
        /// Internal method to start the actual port forward.
        /// </summary>
        private async Task<ActiveForward> StartForwardInternalAsync(
            ForwardInstance instance,
            IKubernetes k8sClient,
            CancellationToken cancellationToken)
        {
            var definition = instance.Definition;

            // Resolve pod name and actual container port if targeting a service
            string podName;
            int containerPort;

            if (definition.ResourceType == ResourceType.Service)
            {
                (podName, containerPort) = await ResolvePodFromServiceAsync(
                    definition.ResourceName, 
                    definition.Namespace, 
                    definition.TargetPort,
                    k8sClient, 
                    cancellationToken);
            }
            else
            {
                podName = definition.ResourceName;
                containerPort = definition.TargetPort;
            }

            _logger.LogInformation("Starting forward {ForwardName} to pod {PodName} port {Port}",
                definition.Name, podName, containerPort);

            var activeForward = new ActiveForward(
                instance.Id,
                podName,
                definition.Namespace,
                containerPort,
                definition.LocalPort,
                k8sClient,
                _logger
            );

            await activeForward.StartAsync(cancellationToken);

            return activeForward;
        }

        /// <summary>
        /// Resolve a pod name and container port from a service.
        /// </summary>
        private async Task<(string PodName, int ContainerPort)> ResolvePodFromServiceAsync(
            string serviceName,
            string @namespace,
            int servicePort,
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

                // Find the matching port and get the targetPort (container port)
                var matchingPort = service.Spec.Ports?.FirstOrDefault(p => p.Port == servicePort);
                int containerPort;
                
                if (matchingPort?.TargetPort != null)
                {
                    // TargetPort can be a string (named port) or int
                    if (int.TryParse(matchingPort.TargetPort.Value, out var parsedPort))
                    {
                        containerPort = parsedPort;
                    }
                    else
                    {
                        // Named port - for now fall back to service port, could resolve from pod spec
                        _logger.LogWarning(
                            "Service {ServiceName} uses named targetPort '{NamedPort}', falling back to service port {ServicePort}",
                            serviceName, matchingPort.TargetPort.Value, servicePort);
                        containerPort = servicePort;
                    }
                }
                else
                {
                    // If targetPort is not specified, it defaults to the same as port
                    containerPort = servicePort;
                }

                _logger.LogInformation(
                    "Service {ServiceName} port {ServicePort} maps to container port {ContainerPort}",
                    serviceName, servicePort, containerPort);

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
                    
                return (runningPod.Metadata.Name, containerPort);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve pod from service {ServiceName}", serviceName);
                throw;
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _logger.LogInformation("Disposing PortForwardingService, stopping all forwards");

            // Stop all active forwards
            var stopTasks = _activeForwards.Values
                .Select(async activeForward =>
                {
                    try
                    {
                        await activeForward.StopAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error stopping forward during dispose");
                    }
                });

            Task.WhenAll(stopTasks).GetAwaiter().GetResult();

            // Dispose all K8s clients
            foreach (var k8sClient in _k8sClients.Values)
            {
                try
                {
                    k8sClient.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing K8s client");
                }
            }

            _activeForwards.Clear();
            _runningTemplates.Clear();
            _k8sClients.Clear();

            _logger.LogInformation("PortForwardingService disposed");
        }

        #endregion
    }

}
