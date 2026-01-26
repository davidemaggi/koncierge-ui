using k8s;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace KonciergeUI.Kube
{
    public class ActiveForward
    {
        private readonly Guid _instanceId;
        private readonly string _podName;
        private readonly string _namespace;
        private readonly int _targetPort;
        private readonly int _requestedLocalPort;
        private readonly IKubernetes _k8sClient;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentBag<string> _logs = new();
        private readonly SemaphoreSlim _logSemaphore = new(1, 1);
        private const int MaxLogEntries = 1000;

        private TcpListener? _listener;
        public int BoundPort { get; private set; }

        public ActiveForward(
            Guid instanceId,
            string podName,
            string @namespace,
            int targetPort,
            int requestedLocalPort,
            IKubernetes k8sClient,
            ILogger logger)
        {
            _instanceId = instanceId;
            _podName = podName;
            _namespace = @namespace;
            _targetPort = targetPort;
            _requestedLocalPort = requestedLocalPort;
            _k8sClient = k8sClient;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var localPort = _requestedLocalPort == 0 ? FindAvailablePort() : _requestedLocalPort;

            var ipAddress = IPAddress.Loopback;
            var localEndPoint = new IPEndPoint(ipAddress, localPort);

            _listener = new TcpListener(localEndPoint);
            _listener.Start(100);

            BoundPort = ((IPEndPoint)_listener.LocalEndpoint).Port;

            await AddLogAsync($"✓ Port forward started: localhost:{BoundPort} -> {_podName}:{_targetPort}");

            _logger.LogInformation("Forward {InstanceId} listening on localhost:{Port} -> {Pod}:{TargetPort}",
                _instanceId, BoundPort, _podName, _targetPort);

            // Accept connections in background
            _ = Task.Run(() => AcceptConnectionsLoop(), _cts.Token);
        }

        public async Task StopAsync()
        {
            await AddLogAsync("⚠ Port forward stopping...");

            try
            {
                _cts.Cancel();
            }
            catch { }

            try
            {
                _listener?.Stop();
            }
            catch { }

            await AddLogAsync("✓ Port forward stopped");
            _logger.LogInformation("Forward {InstanceId} stopped", _instanceId);
        }

        private void AcceptConnectionsLoop()
        {
            AddLogAsync("✓ Accept loop started, waiting for connections...").Wait();

            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // Blocking accept
                        var client = _listener!.AcceptTcpClient();

                        var remoteEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
                        AddLogAsync($"✓ New connection from {remoteEndpoint}").Wait();

                        // Handle each connection on a separate thread - each gets its own WebSocket!
                        _ = Task.Run(() => HandleClientConnection(client), _cts.Token);
                    }
                    catch (SocketException) when (_cts.Token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
                    {
                        _logger.LogError(ex, "Error accepting connection for forward {InstanceId}", _instanceId);
                        AddLogAsync($"✗ Error accepting connection: {ex.Message}").Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Accept loop failed for forward {InstanceId}", _instanceId);
                AddLogAsync($"✗ Accept loop failed: {ex.Message}").Wait();
            }
        }

        private void HandleClientConnection(TcpClient client)
        {
            var connectionId = Guid.NewGuid().ToString("N")[..8];
            AddLogAsync($"[{connectionId}] Connection handler started").Wait();

            WebSocket? webSocket = null;
            StreamDemuxer? demux = null;

            try
            {
                client.NoDelay = true;
                client.ReceiveBufferSize = 65536;
                client.SendBufferSize = 65536;

                AddLogAsync($"[{connectionId}] Creating WebSocket...").Wait();

                // Create a NEW WebSocket for THIS connection
                webSocket = _k8sClient.WebSocketNamespacedPodPortForwardAsync(
                    _podName,
                    _namespace,
                    new[] { _targetPort },
                    "v4.channel.k8s.io",
                    cancellationToken: _cts.Token
                ).GetAwaiter().GetResult();

                AddLogAsync($"[{connectionId}] ✓ WebSocket connected, state: {webSocket.State}").Wait();

                demux = new StreamDemuxer(webSocket, StreamType.PortForward);
                demux.Start();

                var podStream = demux.GetStream((byte?)0, (byte?)0);

                if (podStream == null)
                {
                    AddLogAsync($"[{connectionId}] ✗ Failed to get pod stream").Wait();
                    return;
                }

                AddLogAsync($"[{connectionId}] ✓ Got streams, starting forwarding...").Wait();

                var clientStream = client.GetStream();

                // Both copy operations run on separate threads with BLOCKING I/O
                var clientToPodTask = Task.Run(() =>
                {
                    var buffer = new byte[4096];
                    long totalBytes = 0;
                    try
                    {
                        while (!_cts.Token.IsCancellationRequested)
                        {
                            // Blocking read
                            var bytesRead = clientStream.Read(buffer, 0, buffer.Length);
                            if (bytesRead == 0)
                            {
                                AddLogAsync($"[{connectionId}] C→P: Client closed ({totalBytes} bytes)").Wait();
                                break;
                            }

                            // Blocking write
                            podStream.Write(buffer, 0, bytesRead);
                            podStream.Flush();

                            totalBytes += bytesRead;

                            if (totalBytes == bytesRead)
                            {
                                AddLogAsync($"[{connectionId}] C→P: Started, first {bytesRead} bytes").Wait();
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        AddLogAsync($"[{connectionId}] C→P: Error after {totalBytes} bytes: {ex.Message}").Wait();
                    }
                }, _cts.Token);

                var podToClientTask = Task.Run(() =>
                {
                    var buffer = new byte[4096];
                    long totalBytes = 0;
                    try
                    {
                        while (!_cts.Token.IsCancellationRequested)
                        {
                            // Blocking read
                            var bytesRead = podStream.Read(buffer, 0, buffer.Length);
                            if (bytesRead == 0)
                            {
                                AddLogAsync($"[{connectionId}] P→C: Pod closed ({totalBytes} bytes)").Wait();
                                break;
                            }

                            // Blocking write
                            clientStream.Write(buffer, 0, bytesRead);
                            clientStream.Flush();

                            totalBytes += bytesRead;

                            if (totalBytes == bytesRead)
                            {
                                AddLogAsync($"[{connectionId}] P→C: Started, first {bytesRead} bytes").Wait();
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        AddLogAsync($"[{connectionId}] P→C: Error after {totalBytes} bytes: {ex.Message}").Wait();
                    }
                }, _cts.Token);

                // Wait for either direction to complete
                Task.WaitAny(clientToPodTask, podToClientTask);

                AddLogAsync($"[{connectionId}] ✓ Connection closed").Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling connection {ConnectionId}", connectionId);
                AddLogAsync($"[{connectionId}] ✗ Error: {ex.GetType().Name}: {ex.Message}").Wait();
            }
            finally
            {
                try { client?.Close(); } catch { }
                try { demux?.Dispose(); } catch { }
                try { webSocket?.Dispose(); } catch { }
            }
        }

        private int FindAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private async Task AddLogAsync(string message)
        {
            await _logSemaphore.WaitAsync();
            try
            {
                var logEntry = $"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] {message}";
                _logs.Add(logEntry);

                while (_logs.Count > MaxLogEntries)
                {
                    _logs.TryTake(out _);
                }
            }
            finally
            {
                _logSemaphore.Release();
            }
        }

        public IReadOnlyCollection<string> GetLogs(int maxLines)
        {
            return _logs.TakeLast(maxLines).ToList().AsReadOnly();
        }
    }

}
