
using System.Net.WebSockets;
using KonciergeUI.Models.Forwarding;
using k8s;
using k8s.Models;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;

namespace KonciergeUI.Kube;

public sealed class PortForwardRuntime : IAsyncDisposable
{
    private readonly IKubernetes _client;
    private readonly PortForwardDefinition _definition;
    private readonly string _namespace;
    private CancellationTokenSource _cts = new();
    private readonly object _sync = new();

    private Task? _runTask;
    private Task? _pumpTask;
    private int _restartAttempts;
    private const int MaxRestartAttempts = 10;

    private WebSocket? _webSocket;
    private StreamDemuxer? _demuxer;
    private Stream? _podReadStream;
    private Stream? _podWriteStream;
    private TcpListener? _tcpListener;

    private readonly ConcurrentQueue<ForwardLogEntry> _logs = new();
    private const int MaxLogEntries = 500;

    public ForwardInstance Instance { get; private set; }

    public PortForwardRuntime(IKubernetes client, PortForwardDefinition definition, string defaultNamespace)
    {
        _client = client;
        _definition = definition;
        _namespace = string.IsNullOrWhiteSpace(definition.Namespace)
            ? defaultNamespace
            : definition.Namespace!;

        Instance = new ForwardInstance
        {
            Id = definition.Id,
            Name = definition.Name,
            Definition = definition,
            Status = Enums.ForwardStatus.Stopped,
            BoundLocalPort = definition.LocalPort,
            LocalHost = "127.0.0.1",
            StartedAt = DateTimeOffset.UtcNow,
            ResolvedSecrets = new(),
            ReconnectAttempts = 0
        };
    }

    public IReadOnlyList<ForwardLogEntry> GetLogs() => _logs.ToArray();

    private void Log(string message)
    {
        Console.WriteLine($"[{DateTimeOffset.UtcNow:HH:mm:ss}] [{Instance.Name}] {message}");
        _logs.Enqueue(new ForwardLogEntry { Timestamp = DateTimeOffset.UtcNow, Message = message });
        while (_logs.Count > MaxLogEntries)
            _logs.TryDequeue(out _);
    }

    public void Start()
    {
        lock (_sync)
        {
            if (Instance.Status is Enums.ForwardStatus.Running or Enums.ForwardStatus.Starting)
                return;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _restartAttempts = 0;
            UpdateStatus(Enums.ForwardStatus.Starting);

            _runTask = Task.Run(() => RunLoopAsync(_cts.Token));
            Log($"ðŸ”¥ Forward started: {_definition.Name}");
        }
    }

    public async Task StopAsync()
    {
        lock (_sync)
        {
            if (Instance.Status is Enums.ForwardStatus.Stopped or Enums.ForwardStatus.Failed)
                return;

            UpdateStatus(Enums.ForwardStatus.Stopping);
            _cts.Cancel();
        }

        if (_runTask != null)
            await _runTask.WaitAsync(TimeSpan.FromSeconds(5));

        await CleanupAsync();
        UpdateStatus(Enums.ForwardStatus.Stopped);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        Log("ðŸš€ RunLoopAsync STARTED");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await StartForwardOnceAsync(ct);
                UpdateStatus(Enums.ForwardStatus.Running, Instance.BoundLocalPort);
                Log($"âœ… Forward running: {Instance.LocalAddress}");

                await Task.WhenAny(
                    WaitUntilBrokenOrCancelledAsync(ct),
                    _pumpTask ?? Task.CompletedTask
                );
            }
            catch (OperationCanceledException)
            {
                Log("Loop cancelled - normal");
                break;
            }
            catch (Exception ex)
            {
                Log($"Loop error: {ex.Message}");
                UpdateStatus(Enums.ForwardStatus.Failed, error: ex.Message);
            }

            await CleanupAsync();

            if (ct.IsCancellationRequested)
                break;

            _restartAttempts++;
            if (_restartAttempts > MaxRestartAttempts)
            {
                UpdateStatus(Enums.ForwardStatus.Failed, error: "Max restarts reached");
                Log("Max restart attempts reached");
                break;
            }

            UpdateStatus(Enums.ForwardStatus.Starting);
            Log($"Restart #{_restartAttempts}/{MaxRestartAttempts}");
            await Task.Delay(2000, ct);
        }
    }

    private Stream? GetForwardStreamOrWait(StreamDemuxer demux, byte local, byte? remote, int timeoutMs = 5000)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            var stream = demux.GetStream(local, remote);
            if (stream != null) 
            {
                Log($"âœ… Got stream L{local} R{(remote == null ? "null" : remote.ToString())} CanR/W={stream.CanRead}/{stream.CanWrite}");
                return stream;
            }
            Thread.Sleep(50);
        }
        //Log($"ðŸ’¥ Timeout L{local} R{(remote == null ? "null" : remote.ToString())} - streams: [{string.Join(", ", demux.GetAllStreams().Select(s => $"(L{s.LocalChannel},R{s.RemoteChannel})"))}]");
        return null;
    }

    private async Task StartForwardOnceAsync(CancellationToken ct)
    {
        if (Instance.BoundLocalPort == null || Instance.BoundLocalPort == 0)
            Instance.BoundLocalPort = GetFreeTcpPort();

        var podName = _definition.ResourceType == Enums.ResourceType.Pod
            ? _definition.ResourceName
            : await ResolvePodFromServiceAsync(ct);

        Log($"Forwarding {podName}:{_definition.TargetPort} â†’ localhost:{Instance.BoundLocalPort}");

        // K8s WebSocket
        _webSocket = await _client.WebSocketNamespacedPodPortForwardAsync(
            name: podName,
            @namespace: _namespace,
            ports: new[] { _definition.TargetPort },
            "v4.channel.k8s.io",
            cancellationToken: ct);

        _demuxer = new StreamDemuxer(_webSocket, StreamType.PortForward);
        _demuxer.Start();
        await Task.Delay(300, ct);

        // FIXED: Separate read/write streams for port-forward
        _podReadStream = GetForwardStreamOrWait(_demuxer, 0, null, 5000);
        _podWriteStream = GetForwardStreamOrWait(_demuxer, 0, null, 5000);  // Same stream works both ways

        if (_podReadStream == null || _podWriteStream == null)
            throw new InvalidOperationException("Pod not listening - no streams (0,null)");

        Log($"âœ… Streams ready R:{_podReadStream.CanRead} W:{_podWriteStream.CanWrite}");

        _tcpListener = new TcpListener(IPAddress.Loopback, Instance.BoundLocalPort.Value);
        _tcpListener.Start();
        Log("âœ… TcpListener bound");

        _pumpTask = PumpConnectionsAsync(_tcpListener, ct);
    }

    private async Task PumpConnectionsAsync(TcpListener listener, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var tcpClient = await listener.AcceptTcpClientAsync(ct);
                Log($"*** ACCEPTED ***");
                _ = Task.Run(() => PumpConnectionAsync(tcpClient, ct));
            }
        }
        catch (OperationCanceledException) { Log("Pump cancelled"); }
        catch (Exception ex) { Log($"Pump ERROR: {ex.Message}"); }
        finally { try { listener.Stop(); } catch { } }
    }

    private async Task PumpConnectionAsync(TcpClient tcpClient, CancellationToken ct)
    {
        try
        {
            await using var tcpStream = tcpClient.GetStream();
            var buffer = new byte[8192];
            Log("ðŸ”— Conn ready");

            // FIXED: tcpRead â†’ podWrite, podRead â†’ tcpWrite (directional)
            var tcpToPod = CopyStreamAsync(tcpStream, _podWriteStream!, buffer, ct, "TCPâ†’POD");
            var podToTcp = CopyStreamAsync(_podReadStream!, tcpStream, buffer, ct, "PODâ†’TCP");

            await Task.WhenAll(tcpToPod, podToTcp);
        }
        catch (Exception ex) { Log($"Conn ERROR: {ex.Message}"); }
    }

    private static async Task CopyStreamAsync(Stream source, Stream dest, byte[] buffer, 
        CancellationToken ct, string direction)
    {
        int total = 0;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var bytesRead = await source.ReadAsync(buffer, ct);
                Console.WriteLine($"{direction}: Read {bytesRead}B (tot {total += bytesRead})");

                if (bytesRead == 0) 
                {
                    Console.WriteLine($"{direction}: EOF");
                    break;
                }

                if (dest.CanWrite)
                {
                    await dest.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    await dest.FlushAsync(ct);
                    Console.WriteLine($"{direction}: Wrote {bytesRead}B");
                }
                else
                {
                    Console.WriteLine($"{direction}: SKIP write - !CanWrite");
                }
            }
        }
        catch (Exception ex) { Console.WriteLine($"{direction}: ERROR {ex.Message}"); }
    }

    private async Task WaitUntilBrokenOrCancelledAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_webSocket?.State != WebSocketState.Open)
            {
                Log($"WS broke: {_webSocket?.State}");
                return;
            }
            await Task.Delay(1000, ct);
        }
    }

    private async Task CleanupAsync()
    {
        if (_pumpTask != null && !_pumpTask.IsCompleted)
            await _pumpTask.WaitAsync(TimeSpan.FromSeconds(2));

        try { _tcpListener?.Stop(); } catch { }
        _podReadStream?.Dispose();
        _podWriteStream?.Dispose();
        _demuxer?.Dispose();
        _webSocket?.Dispose();

        _tcpListener = null;
        _podReadStream = null;
        _podWriteStream = null;
        _demuxer = null;
        _webSocket = null;
        _pumpTask = null;
    }

    private async Task<string> ResolvePodFromServiceAsync(CancellationToken ct)
    {
        var eps = await _client.CoreV1.ListNamespacedEndpointsAsync(_namespace, cancellationToken: ct);
        var ep = eps.Items.FirstOrDefault(e => e.Metadata?.Name == _definition.ResourceName)
                 ?? throw new InvalidOperationException($"No endpoints for {_definition.ResourceName}");

        var address = ep.Subsets?.FirstOrDefault()?.Addresses?.FirstOrDefault()
                      ?? throw new InvalidOperationException("No endpoint addresses");

        return address.TargetRef?.Name ?? throw new InvalidOperationException("No pod name");
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try { return ((IPEndPoint)listener.LocalEndpoint!).Port; }
        finally { listener.Stop(); }
    }

    private void UpdateStatus(Enums.ForwardStatus status, int? localPort = null, string? error = null)
    {
        lock (_sync)
        {
            Instance = Instance with
            {
                Status = status,
                BoundLocalPort = localPort ?? Instance.BoundLocalPort,
                ErrorMessage = error,
                StoppedAt = status == Enums.ForwardStatus.Stopped ? DateTimeOffset.UtcNow : Instance.StoppedAt,
                ReconnectAttempts = status == Enums.ForwardStatus.Starting ? _restartAttempts : Instance.ReconnectAttempts
            };
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts.Dispose();
    }
}