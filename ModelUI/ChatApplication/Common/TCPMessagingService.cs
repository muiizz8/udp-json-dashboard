using System;
using System.Net;
using System.Text;
using NetCoreServer;

namespace ChatApplication.Network;

/// <summary>
/// Provides TCP client/server messaging for plain-text chat messages.
/// </summary>
public sealed class TcpMessagingService
{
    private readonly TcpJsonServer server;
    private TcpJsonClient? client;
    private readonly string remoteIp;
    private readonly int remotePort;

    public event Action<string>? Debug;
    public event Action<string, string>? MessageReceived;

    public TcpMessagingService(string localIp, int localPort, string remoteIp, int remotePort)
    {
        this.remoteIp = remoteIp;
        this.remotePort = remotePort;
        server = new TcpJsonServer(IPAddress.Parse(localIp), localPort, this);
    }

    /// <summary>Starts the TCP server to accept incoming connections.</summary>
    public void StartServer() => server.Start();

    /// <summary>Stops the TCP server.</summary>
    public void StopServer() => server.Stop();

    /// <summary>Connects the TCP client to the remote endpoint.</summary>
    public void ConnectClient()
    {
        client?.Disconnect();
        client = new TcpJsonClient(remoteIp, remotePort, this);
        client.ConnectAsync();
    }

    /// <summary>Disconnects the TCP client.</summary>
    public void DisconnectClient()
    {
        client?.Disconnect();
        client = null;
    }

    /// <summary>Sends a plain-text message over TCP (newline-delimited).</summary>
    public void Send(string message)
    {
        if (client == null || !client.IsConnected)
        {
            Debug?.Invoke("TCP: client is not connected. Call ConnectClient() first.");
            return;
        }
        client.SendAsync(message + "\n");
        Debug?.Invoke("Sent (TCP)");
    }

    private sealed class TcpJsonServer : TcpServer
    {
        private readonly TcpMessagingService parent;
        public TcpJsonServer(IPAddress address, int port, TcpMessagingService parent)
            : base(address, port) => this.parent = parent;

        protected override TcpSession CreateSession() => new TcpJsonSession(this, parent);
    }

    private sealed class TcpJsonSession : TcpSession
    {
        private readonly TcpMessagingService parent;
        private readonly StringBuilder buffer = new();

        public TcpJsonSession(TcpServer server, TcpMessagingService parent) : base(server)
            => this.parent = parent;

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            this.buffer.Append(Encoding.UTF8.GetString(buffer, (int)offset, (int)size));
            DrainBuffer();
        }

        private void DrainBuffer()
        {
            while (true)
            {
                var text = buffer.ToString();
                var idx = text.IndexOf('\n');
                if (idx < 0) break;

                var line = text[..idx].Trim();
                buffer.Remove(0, idx + 1);

                if (line.Length == 0) continue;

                var remote = Socket?.RemoteEndPoint?.ToString() ?? "Unknown";
                parent.MessageReceived?.Invoke(line, remote);
            }
        }
    }

    private sealed class TcpJsonClient : TcpClient
    {
        private readonly TcpMessagingService parent;
        private readonly StringBuilder buffer = new();

        public TcpJsonClient(string host, int port, TcpMessagingService parent) : base(host, port)
            => this.parent = parent;

        protected override void OnConnected() => parent.Debug?.Invoke("TCP client connected.");
        protected override void OnDisconnected() => parent.Debug?.Invoke("TCP client disconnected.");

        protected override void OnReceived(byte[] data, long offset, long size)
        {
            buffer.Append(Encoding.UTF8.GetString(data, (int)offset, (int)size));
            DrainBuffer();
        }

        private void DrainBuffer()
        {
            while (true)
            {
                var text = buffer.ToString();
                var idx = text.IndexOf('\n');
                if (idx < 0) break;

                var line = text[..idx].Trim();
                buffer.Remove(0, idx + 1);

                if (line.Length == 0) continue;

                var remote = Socket?.RemoteEndPoint?.ToString() ?? "Server";
                parent.MessageReceived?.Invoke(line, remote);
            }
        }
    }
}
