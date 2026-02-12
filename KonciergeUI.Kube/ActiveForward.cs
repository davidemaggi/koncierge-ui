using k8s;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Diagnostics;

namespace KonciergeUI.Kube
{
    public class ActiveForward : IActiveForward
    {
        private readonly Guid _instanceId;
        private readonly string _podName;
        private readonly string _namespace;
        private readonly int _targetPort;
        private readonly int _requestedLocalPort;
        private readonly IKubernetes _k8sClient;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentQueue<string> _logs = new();
        private readonly SemaphoreSlim _logSemaphore = new(1, 1);
        private readonly object _startStopLock = new();
        private const int MaxLogEntries = 1000;

        private TcpListener? _listener;
        private Task? _acceptLoopTask;
        private bool _isStarted;
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
            cancellationToken.ThrowIfCancellationRequested();

            lock (_startStopLock)
            {
                if (_isStarted)
                {
                    return;
                }

                var localPort = _requestedLocalPort == 0 ? FindAvailablePort() : _requestedLocalPort;
                var localEndPoint = new IPEndPoint(IPAddress.Loopback, localPort);

                _listener = new TcpListener(localEndPoint);
                _listener.Start(100);

                BoundPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
                _isStarted = true;
            }

            await AddLogAsync($"✓ Port forward started: localhost:{BoundPort} -> {_podName}:{_targetPort}").ConfigureAwait(false);

            _logger.LogInformation("Forward {InstanceId} listening on localhost:{Port} -> {Pod}:{TargetPort}",
                _instanceId, BoundPort, _podName, _targetPort);

            _acceptLoopTask = Task.Run(() => AcceptConnectionsLoopAsync(_cts.Token), _cts.Token);
        }

        public async Task StopAsync()
        {
            await AddLogAsync("⚠ Port forward stopping...").ConfigureAwait(false);

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

            if (_acceptLoopTask is not null)
            {
                try
                {
                    await _acceptLoopTask.ConfigureAwait(false);
                }
                catch { }
            }

            lock (_startStopLock)
            {
                _isStarted = false;
                _listener = null;
            }

            await AddLogAsync("✓ Port forward stopped").ConfigureAwait(false);
            _logger.LogInformation("Forward {InstanceId} stopped", _instanceId);
        }

        private async Task AcceptConnectionsLoopAsync(CancellationToken cancellationToken)
        {
            await AddLogAsync("✓ Accept loop started, waiting for connections...").ConfigureAwait(false);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (_listener is null)
                        {
                            break;
                        }

                        var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);

                        var remoteEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
                        await AddLogAsync($"✓ New connection from {remoteEndpoint}").ConfigureAwait(false);

                        // Handle each connection on a separate task - each gets its own WebSocket
                        _ = Task.Run(() => HandleClientConnectionAsync(client, cancellationToken), cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (SocketException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogError(ex, "Error accepting connection for forward {InstanceId}", _instanceId);
                        await AddLogAsync($"✗ Error accepting connection: {ex.Message}").ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Accept loop failed for forward {InstanceId}", _instanceId);
                await AddLogAsync($"✗ Accept loop failed: {ex.Message}").ConfigureAwait(false);
            }
        }

        private async Task HandleClientConnectionAsync(TcpClient client, CancellationToken cancellationToken)
        {
            var connectionId = Guid.NewGuid().ToString("N")[..8];
            var connectionStopwatch = Stopwatch.StartNew();
            long bytesFromClient = 0;
            long bytesToClient = 0;
            await AddLogAsync($"[{connectionId}] Connection handler started").ConfigureAwait(false);

            WebSocket? webSocket = null;

            try
            {
                client.NoDelay = true;
                client.ReceiveBufferSize = 65536;
                client.SendBufferSize = 65536;

                await AddLogAsync($"[{connectionId}] Creating WebSocket...").ConfigureAwait(false);

                var handshakeStopwatch = Stopwatch.StartNew();
                webSocket = await _k8sClient.WebSocketNamespacedPodPortForwardAsync(
                    _podName,
                    _namespace,
                    new[] { _targetPort },
                    "v4.channel.k8s.io",
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);
                handshakeStopwatch.Stop();

                await AddLogAsync($"[{connectionId}] ✓ WebSocket connected in {handshakeStopwatch.ElapsedMilliseconds}ms, state: {webSocket.State}")
                    .ConfigureAwait(false);

                var clientStream = client.GetStream();

                using var forwardCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var token = forwardCts.Token;

                await AddLogAsync($"[{connectionId}] ✓ Starting forwarding...").ConfigureAwait(false);

                // Track if we've received the initial port confirmation on each channel
                var dataChannelReady = false;
                var errorChannelReady = false;

                // Start client->pod copy (reads from TCP, sends to WebSocket channel 0)
                var clientToPod = Task.Run(async () =>
                {
                    var buffer = new byte[65536];
                    var sendBuffer = new byte[65537]; // +1 for channel byte
                    try
                    {
                        while (!token.IsCancellationRequested && webSocket.State == WebSocketState.Open)
                        {
                            var read = await clientStream.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false);
                            if (read == 0)
                            {
                                await AddLogAsync($"[{connectionId}] C→P: Client closed").ConfigureAwait(false);
                                break;
                            }

                            bytesFromClient += read;

                            if (bytesFromClient == read)
                            {
                                var firstLine = TryGetFirstLine(buffer, read);
                                if (!string.IsNullOrEmpty(firstLine))
                                {
                                    await AddLogAsync($"[{connectionId}] C→P: {firstLine}").ConfigureAwait(false);
                                }
                            }

                            // Prepend channel byte (0 for data)
                            sendBuffer[0] = 0;
                            Buffer.BlockCopy(buffer, 0, sendBuffer, 1, read);
                            await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer, 0, read + 1), 
                                WebSocketMessageType.Binary, true, token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        await AddLogAsync($"[{connectionId}] C→P: Canceled").ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await AddLogAsync($"[{connectionId}] C→P error: {ex.GetType().Name}: {ex.Message}").ConfigureAwait(false);
                    }
                }, token);

                // Start pod->client copy (reads from WebSocket, writes to TCP)
                var podToClient = Task.Run(async () =>
                {
                    var buffer = new byte[65537];
                    try
                    {
                        while (!token.IsCancellationRequested && webSocket.State == WebSocketState.Open)
                        {
                            var result = await webSocket.ReceiveAsync(buffer, token).ConfigureAwait(false);
                            
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await AddLogAsync($"[{connectionId}] P→C: WebSocket closed").ConfigureAwait(false);
                                break;
                            }

                            if (result.Count < 1) continue;

                            var channel = buffer[0];
                            var dataStart = 1;
                            var dataLength = result.Count - 1;

                            // First message on each channel is the port confirmation (2 bytes)
                            if (channel == 0 && !dataChannelReady)
                            {
                                dataChannelReady = true;
                                // Skip the 2-byte port confirmation
                                if (dataLength <= 2)
                                {
                                    continue;
                                }
                                // If there's more data after the port, forward it
                                dataStart = 3; // skip channel byte + 2 port bytes
                                dataLength = result.Count - 3;
                            }
                            else if (channel == 1 && !errorChannelReady)
                            {
                                errorChannelReady = true;
                                // Skip the 2-byte port confirmation on error channel
                                if (dataLength <= 2)
                                {
                                    continue;
                                }
                                // If there's actual error data after the port, process it
                                dataStart = 3;
                                dataLength = result.Count - 3;
                                if (dataLength > 0)
                                {
                                    var errorMsg = Encoding.UTF8.GetString(buffer, dataStart, dataLength).Trim();
                                    if (!string.IsNullOrEmpty(errorMsg))
                                    {
                                        await AddLogAsync($"[{connectionId}] K8s error: {errorMsg}").ConfigureAwait(false);
                                    }
                                }
                                continue;
                            }

                            if (channel == 0 && dataLength > 0)
                            {
                                // Data channel
                                bytesToClient += dataLength;

                                if (bytesToClient == dataLength)
                                {
                                    var firstLine = TryGetFirstLine(buffer.AsSpan(dataStart, dataLength).ToArray(), dataLength);
                                    if (!string.IsNullOrEmpty(firstLine))
                                    {
                                        await AddLogAsync($"[{connectionId}] P→C: {firstLine}").ConfigureAwait(false);
                                    }
                                }

                                await clientStream.WriteAsync(buffer.AsMemory(dataStart, dataLength), token).ConfigureAwait(false);
                                await clientStream.FlushAsync(token).ConfigureAwait(false);
                            }
                            else if (channel == 1 && dataLength > 0)
                            {
                                // Error channel
                                var errorMsg = Encoding.UTF8.GetString(buffer, dataStart, dataLength).Trim();
                                if (!string.IsNullOrEmpty(errorMsg))
                                {
                                    await AddLogAsync($"[{connectionId}] K8s error: {errorMsg}").ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        await AddLogAsync($"[{connectionId}] P→C: Canceled").ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        await AddLogAsync($"[{connectionId}] P→C error: {ex.GetType().Name}: {ex.Message}").ConfigureAwait(false);
                    }
                }, token);

                // Wait for either direction to complete
                await Task.WhenAny(clientToPod, podToClient).ConfigureAwait(false);

                // Cancel the other direction and cleanup
                forwardCts.Cancel();

                // Give a moment for graceful shutdown
                try
                {
                    await Task.WhenAll(clientToPod, podToClient).WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }
                catch { }

                await AddLogAsync($"[{connectionId}] ✓ Connection closed (C→P: {bytesFromClient}, P→C: {bytesToClient}, {connectionStopwatch.Elapsed.TotalSeconds:F1}s)")
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await AddLogAsync($"[{connectionId}] ⚠ Connection canceled ({connectionStopwatch.Elapsed.TotalSeconds:F1}s)")
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling connection {ConnectionId}", connectionId);
                await AddLogAsync($"[{connectionId}] ✗ Error: {ex.GetType().Name}: {ex.Message}").ConfigureAwait(false);
            }
            finally
            {
                try { client.Close(); } catch { }
                if (webSocket != null)
                {
                    try
                    {
                        if (webSocket.State == WebSocketState.Open)
                        {
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                        }
                        webSocket.Dispose();
                    }
                    catch { }
                }
            }
        }

        private static string? TryGetFirstLine(byte[] buffer, int bytesRead)
        {
            if (bytesRead <= 0)
            {
                return null;
            }

            var lineEnd = Array.IndexOf(buffer, (byte)'\n', 0, bytesRead);
            if (lineEnd < 0)
            {
                return null;
            }

            var length = lineEnd;
            if (length > 0 && buffer[length - 1] == '\r')
            {
                length--;
            }

            if (length <= 0)
            {
                return null;
            }

            return Encoding.ASCII.GetString(buffer, 0, length);
        }

        private int FindAvailablePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            return port;
        }

        private async Task AddLogAsync(string message)
        {
            await _logSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var logEntry = $"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}] {message}";
                _logs.Enqueue(logEntry);

                while (_logs.Count > MaxLogEntries && _logs.TryDequeue(out _))
                {
                }
            }
            finally
            {
                _logSemaphore.Release();
            }
        }


        public IReadOnlyCollection<string> GetLogs(int maxLines)
        {
            var snapshot = _logs.ToArray();
            return snapshot.TakeLast(maxLines).ToList().AsReadOnly();
        }
    }

}
