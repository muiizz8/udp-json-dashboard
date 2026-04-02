using System;
using System.Net;
using System.Text;
using NetCoreServer;
using ChatCore.Interfaces;

namespace ChatApplication.Implementations.Transports;

/// <summary>
/// TCP implementation of IMessagingTransport using NetCoreServer.
/// </summary>
public sealed class TcpTransport : IMessagingTransport
{
    private TcpJsonServer? _server;
    private TcpJsonClient? _client;
    private string _remoteIp = "";
    private int _remotePort;
    private bool _isListening;

    public string Protocol => "TCP";
    public bool IsListening => _isListening;
    public bool IsConnected => _client?.IsConnected ?? false;

    public event Action<string>? DebugMessage;
    public event Action<string, string>? MessageReceived;

    public void StartServer(string localIp, int localPort)
    {
        _server?.Stop();
        _server = new TcpJsonServer(IPAddress.Parse(localIp), localPort, this);
        _server.Start();
        _isListening = true;
        DebugMessage?.Invoke($"TCP server started on {localIp}:{localPort}");
    }

    public void StopServer()
    {
        _server?.Stop();
        _isListening = false;
        DebugMessage?.Invoke("TCP server stopped.");
    }

    public void Connect(string remoteIp, int remotePort)
    {
        _remoteIp = remoteIp;
        _remotePort = remotePort;
        _client?.Disconnect();
        _client = new TcpJsonClient(_remoteIp, _remotePort, this);
        _client.ConnectAsync();
        DebugMessage?.Invoke("TCP client connecting...");
    }

    public void Disconnect()
    {
        _client?.Disconnect();
        _client = null;
        DebugMessage?.Invoke("TCP client disconnected.");
    }

    public void Send(string message)
    {
        if (_client == null || !_client.IsConnected)
        {
            DebugMessage?.Invoke("TCP: client is not connected. Call Connect() first.");
            return;
        }
        _client.SendAsync(message + "\n");
        DebugMessage?.Invoke("Sent (TCP)");
    }

    public void Dispose()
    {
        StopServer();
        _client?.Disconnect();
    }

    // ── Inner server ──────────────────────────────────────────────────────

    private sealed class TcpJsonServer : TcpServer
    {
        private readonly TcpTransport _parent;
        public TcpJsonServer(IPAddress address, int port, TcpTransport parent)
            : base(address, port) => _parent = parent;

        protected override TcpSession CreateSession() => new TcpJsonSession(this, _parent);
    }

    private sealed class TcpJsonSession : TcpSession
    {
        private readonly TcpTransport _parent;
        private readonly StringBuilder _buffer = new();

        public TcpJsonSession(TcpServer server, TcpTransport parent) : base(server)
            => _parent = parent;

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            _buffer.Append(Encoding.UTF8.GetString(buffer, (int)offset, (int)size));
            DrainBuffer();
        }

        private void DrainBuffer()
        {
            while (true)
            {
                var text = _buffer.ToString();
                var idx = text.IndexOf('\n');
                if (idx < 0) break;

                var line = text[..idx].Trim();
                _buffer.Remove(0, idx + 1);

                if (line.Length == 0) continue;

                var remote = Socket?.RemoteEndPoint?.ToString() ?? "Unknown";
                _parent.MessageReceived?.Invoke(line, remote);
            }
        }
    }

    // ── Inner client ──────────────────────────────────────────────────────

    private sealed class TcpJsonClient : TcpClient
    {
        private readonly TcpTransport _parent;
        private readonly StringBuilder _buffer = new();

        public TcpJsonClient(string host, int port, TcpTransport parent) : base(host, port)
            => _parent = parent;

        protected override void OnConnected() => _parent.DebugMessage?.Invoke("TCP client connected.");
        protected override void OnDisconnected() => _parent.DebugMessage?.Invoke("TCP client disconnected.");

        protected override void OnReceived(byte[] data, long offset, long size)
        {
            _buffer.Append(Encoding.UTF8.GetString(data, (int)offset, (int)size));
            DrainBuffer();
        }

        private void DrainBuffer()
        {
            while (true)
            {
                var text = _buffer.ToString();
                var idx = text.IndexOf('\n');
                if (idx < 0) break;

                var line = text[..idx].Trim();
                _buffer.Remove(0, idx + 1);

                if (line.Length == 0) continue;

                var remote = Socket?.RemoteEndPoint?.ToString() ?? "Server";
                _parent.MessageReceived?.Invoke(line, remote);
            }
        }
    }
}
