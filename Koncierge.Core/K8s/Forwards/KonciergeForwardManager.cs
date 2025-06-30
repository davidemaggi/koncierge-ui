using k8s;
using Koncierge.Domain.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static Koncierge.Core.K8s.Forwards.PortForwardSession;

namespace Koncierge.Core.K8s.Forwards
{
    public class KonciergeForwardManager: IKonciergeForwardManager, IDisposable
    {
        private readonly IKubernetesClientManager _clientManager;
        private   KonciergeClient _konciergeClient;
        private readonly ConcurrentDictionary<Guid, PortForwardSession> _sessions = new();

        public KonciergeForwardManager(IKubernetesClientManager clientManager){ 
            _clientManager = clientManager;


        }

        public PortForwardSession StartPortForward(
           KonciergeClient client,
           string namespaceName,
           string targetName,
           int targetPort,
           int localPort,
           bool isService = false)

            
        {
            _konciergeClient = client;

            var session = new PortForwardSession
            {
                Id= PortForwardSession.GenerateId(_konciergeClient.KubeConfig.Id, _konciergeClient.Context, namespaceName, targetName, localPort, targetPort),
                KubeConfig = _konciergeClient.KubeConfig,
                Namespace = namespaceName,
                TargetName = targetName,
                TargetPort = targetPort,
                LocalPort = localPort,
                ContextName = _konciergeClient.Context
            };

            session.ForwardingTask = Task.Run(() =>
                ForwardPort(session, isService), session.CancellationTokenSource.Token);

            _sessions.TryAdd(session.Id, session);
            return session;
        }

        private async Task ForwardPort(PortForwardSession session, bool isService)
        {
            try
            {

                _konciergeClient = await _clientManager.GetClient(session.KubeConfig.Id, session.ContextName);


                string podName = isService
                    ? await ResolveServiceToPod(session.Namespace, session.TargetName)
                    : session.TargetName;

                using var webSocket = await _konciergeClient.Client.WebSocketNamespacedPodPortForwardAsync(
                    podName,
                    session.Namespace,
                    new[] { session.TargetPort },null,null,
                    session.CancellationTokenSource.Token
                );

                using var demuxer = new StreamDemuxer(webSocket);
                demuxer.Start();
                var stream = demuxer.GetStream(ChannelIndex.StdIn, ChannelIndex.StdOut);

                using var listener = new TcpListener(IPAddress.Loopback, session.LocalPort);
                listener.Start();

                while (!session.CancellationTokenSource.Token.IsCancellationRequested)
                {
                    using var tcpClient = await listener.AcceptTcpClientAsync();
                    await using var tcpStream = tcpClient.GetStream();

                    var copyTasks = new[]
                    {
                    tcpStream.CopyToAsync(stream, session.CancellationTokenSource.Token),
                    stream.CopyToAsync(tcpStream, session.CancellationTokenSource.Token)
                };

                    await Task.WhenAny(copyTasks);
                }
            }
            catch (Exception ex)
            {
                session.Status = PortForwardStatus.Error;
                LogToSession(session, $"ERROR: {ex.Message}");
            }
            finally
            {
                session.Status = PortForwardStatus.Stopped;
            }
        }

        private async Task<string> ResolveServiceToPod(string namespaceName, string serviceName)
        {
            var service = await _konciergeClient.Client.CoreV1.ReadNamespacedServiceAsync(serviceName, namespaceName);
            var selector = service.Spec.Selector;
            string labelSelector = string.Join(",", selector.Select(kv => $"{kv.Key}={kv.Value}"));

            var pods = await _konciergeClient.Client.CoreV1.ListNamespacedPodAsync(namespaceName, labelSelector: labelSelector);
            return pods.Items.First().Metadata.Name;
        }

        private void LogToSession(PortForwardSession session, string message)
        {
            var logBytes = Encoding.UTF8.GetBytes($"[{DateTime.UtcNow:O}] {message}\n");
            session.Logs.Write(logBytes);
        }

        public void StopPortForward(Guid sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                session.CancellationTokenSource.Cancel();
                session.Status = PortForwardStatus.Stopped;
            }
        }

        public PortForwardSession GetSession(Guid sessionId) =>
            _sessions.TryGetValue(sessionId, out var session) ? session : null;

        public void Dispose()
        {
            foreach (var session in _sessions.Values)
            {
                session.CancellationTokenSource.Cancel();
            }
            GC.SuppressFinalize(this);
        }


    }
}
