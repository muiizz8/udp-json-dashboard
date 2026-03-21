using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NetCoreServer;
using Newtonsoft.Json;
using RxGUI.Common;

namespace RxGUI.Network;

/// <summary>
/// Provides TCP client/server messaging for JSON telemetry.
/// </summary>
public sealed class UdpMessagingService
{
    private readonly UdpJsonServer server;
    private readonly UdpJsonClient client;

    public event Action<string>? Debug;
    public event Action<string, string>? MessageReceived;

    /// <summary>
    /// Initializes the UDP server and client endpoints.
    /// </summary>
    public UdpMessagingService(string localIp, int localPort, string remoteIp, int remotePort)
    {
        server = new UdpJsonServer(IPAddress.Parse(localIp), localPort, this);
        client = new UdpJsonClient(remoteIp, remotePort, this);
    }
    /// <summary>Starts the TCP server.</summary>
    public void StartServer() => server.Start();
    /// <summary>Stops the TCP server.</summary>
    public void StopServer() => server.Stop();

    /// <summary>Connects the TCP client asynchronously.</summary>

    /// <summary>Sends telemetry data as JSON.</summary>
    public void Send(string json)
    {
        client.SendAsync(json);
        Debug?.Invoke($"Sent telemetry (UDP)");
    }

    private sealed class UdpJsonServer : UdpServer
    {
        private readonly UdpMessagingService parent;
        public UdpJsonServer(IPAddress address, int port, UdpMessagingService parent)
            : base(address, port) => this.parent = parent;

        protected override void OnStarted()
        {
            ReceiveAsync();
        }

        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {
            parent.Debug?.Invoke($"UDP RX {size} bytes from {endpoint}");
            var text = Encoding.UTF8.GetString(buffer, (int)offset, (int)size)
                .Trim()
                .TrimEnd('\0');
            try
            {
                if (text.Length == 0)
                    return;

                parent.MessageReceived?.Invoke(text, endpoint.ToString() ?? "Unknown");
            }
            catch (Exception ex)
            {
                parent.Debug?.Invoke($"Error processing message: {ex.Message}");
                parent.MessageReceived?.Invoke(text, endpoint.ToString() ?? "Unknown");
            }
            finally
            {
                ReceiveAsync();
            }
        }
    }

    private sealed class UdpJsonClient : IDisposable
    {
        private readonly UdpMessagingService parent;
        private readonly System.Net.Sockets.UdpClient client;
        private readonly IPEndPoint remoteEndPoint;

        public UdpJsonClient(string host, int port, UdpMessagingService parent)
        {
            this.parent = parent;
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
            client = new System.Net.Sockets.UdpClient();
        }

        public Task<int> SendAsync(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            return client.SendAsync(bytes, bytes.Length, remoteEndPoint);
        }

        public void Dispose()
        {
            try
            {
                client.Dispose();
            }
            catch (Exception ex)
            {
                parent.Debug?.Invoke($"UDP client dispose failed: {ex.Message}");
            }
        }
    }

}
