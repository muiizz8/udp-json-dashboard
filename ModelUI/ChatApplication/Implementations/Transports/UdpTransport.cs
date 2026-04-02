using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NetCoreServer;
using ChatCore.Interfaces;

namespace ChatApplication.Implementations.Transports;

/// <summary>
/// UDP implementation of IMessagingTransport using NetCoreServer.
/// </summary>
public sealed class UdpTransport : IMessagingTransport
{
    private UdpJsonServer? _server;
    private UdpJsonClient? _client;
    private bool _isListening;

    public string Protocol => "UDP";
    public bool IsListening => _isListening;
    public bool IsConnected => _client != null; // UDP is connectionless; client exists once Connect() is called

    public event Action<string>? DebugMessage;
    public event Action<string, string>? MessageReceived;

    public void StartServer(string localIp, int localPort)
    {
        _server?.Stop();
        _server = new UdpJsonServer(IPAddress.Parse(localIp), localPort, this);
        _server.Start();
        _isListening = true;
        DebugMessage?.Invoke($"UDP server started on {localIp}:{localPort}");
    }

    public void StopServer()
    {
        _server?.Stop();
        _isListening = false;
        DebugMessage?.Invoke("UDP server stopped.");
    }

    public void Connect(string remoteIp, int remotePort)
    {
        _client?.Dispose();
        _client = new UdpJsonClient(remoteIp, remotePort, this);
        DebugMessage?.Invoke($"UDP client configured for {remoteIp}:{remotePort}");
    }

    public void Disconnect()
    {
        _client?.Dispose();
        _client = null;
        DebugMessage?.Invoke("UDP client disconnected.");
    }

    public void Send(string message)
    {
        if (_client == null)
        {
            DebugMessage?.Invoke("UDP: no remote endpoint set. Call Connect() first.");
            return;
        }
        _client.SendAsync(message);
        DebugMessage?.Invoke("Sent telemetry (UDP)");
    }

    public void Dispose()
    {
        StopServer();
        _client?.Dispose();
    }

    // ── Inner server ──────────────────────────────────────────────────────

    private sealed class UdpJsonServer : UdpServer
    {
        private readonly UdpTransport _parent;
        public UdpJsonServer(IPAddress address, int port, UdpTransport parent)
            : base(address, port) => _parent = parent;

        protected override void OnStarted() => ReceiveAsync();

        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {
            _parent.DebugMessage?.Invoke($"UDP RX {size} bytes from {endpoint}");
            var text = Encoding.UTF8.GetString(buffer, (int)offset, (int)size)
                .Trim()
                .TrimEnd('\0');
            try
            {
                if (text.Length == 0) return;
                _parent.MessageReceived?.Invoke(text, endpoint.ToString() ?? "Unknown");
            }
            catch (Exception ex)
            {
                _parent.DebugMessage?.Invoke($"Error processing message: {ex.Message}");
                _parent.MessageReceived?.Invoke(text, endpoint.ToString() ?? "Unknown");
            }
            finally
            {
                ReceiveAsync();
            }
        }
    }

    // ── Inner client ──────────────────────────────────────────────────────

    private sealed class UdpJsonClient : IDisposable
    {
        private readonly UdpTransport _parent;
        private readonly System.Net.Sockets.UdpClient _client;
        private readonly IPEndPoint _remoteEndPoint;

        public UdpJsonClient(string host, int port, UdpTransport parent)
        {
            _parent = parent;
            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
            _client = new System.Net.Sockets.UdpClient();
        }

        public Task<int> SendAsync(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            return _client.SendAsync(bytes, bytes.Length, _remoteEndPoint);
        }

        public void Dispose()
        {
            try { _client.Dispose(); }
            catch (Exception ex) { _parent.DebugMessage?.Invoke($"UDP client dispose failed: {ex.Message}"); }
        }
    }
}
