// using System;
// using System.Net;
// using System.Text;
// using NetCoreServer;
// using Newtonsoft.Json;
// using RxGUI.Common;

// namespace RxGUI.Network;

// /// <summary>
// /// Provides TCP client/server messaging for JSON telemetry.
// /// </summary>
// public sealed class TcpMessagingService
// {
//     private readonly TcpJsonServer server;
//     private readonly TcpJsonClient client;

//     public event Action<string>? Debug;
//     public event Action<TelemetryData, string>? MessageReceived;

//     /// <summary>
//     /// Initializes the TCP server and client endpoints.
//     /// </summary>
//     public TcpMessagingService(string localIp, int localPort, string remoteIp, int remotePort)
//     {
//         server = new TcpJsonServer(IPAddress.Parse(localIp), localPort, this);
//         client = new TcpJsonClient(remoteIp, remotePort, this);
//     }
//     /// <summary>Starts the TCP server.</summary>
//     public void StartServer() => server.Start();
//     /// <summary>Stops the TCP server.</summary>
//     public void StopServer() => server.Stop();

//     /// <summary>Connects the TCP client asynchronously.</summary>
//     public void ConnectClient() => client.ConnectAsync();
//     /// <summary>Disconnects the TCP client.</summary>
//     public void DisconnectClient() => client.Disconnect();

//     /// <summary>Sends telemetry data as JSON.</summary>
//     public void Send(TelemetryData data)
//     {
//         string json = JsonConvert.SerializeObject(data, Formatting.Indented);
//         client.SendAsync(json + "\n");
//         Debug?.Invoke($"Sent telemetry.");
//     }

//     private sealed class TcpJsonServer : TcpServer
//     {
//         private readonly TcpMessagingService parent;
//         public TcpJsonServer(IPAddress address, int port, TcpMessagingService parent)
//             : base(address, port) => this.parent = parent;

//         protected override TcpSession CreateSession() => new TcpJsonSession(this, parent);
//     }

//     private sealed class TcpJsonSession : TcpSession
//     {
//         private readonly TcpMessagingService parent;
//         private readonly StringBuilder buffer = new();

//         public TcpJsonSession(TcpServer server, TcpMessagingService parent) : base(server)
//             => this.parent = parent;

//         protected override void OnReceived(byte[] buffer, long offset, long size)
//         {
//             this.buffer.Append(Encoding.UTF8.GetString(buffer, (int)offset, (int)size));
//             DrainBuffer(remoteOverride: null);
//         }

//         private void DrainBuffer(string? remoteOverride)
//         {
//             while (true)
//             {
//                 var text = buffer.ToString();
//                 var newlineIndex = text.IndexOf('\n');
//                 if (newlineIndex < 0)
//                     break;

//                 var line = text[..newlineIndex].Trim();
//                 buffer.Remove(0, newlineIndex + 1);

//                 if (line.Length == 0)
//                     continue;

//                 try
//                 {
//                     var data = JsonConvert.DeserializeObject<TelemetryData>(line);
//                     if (data != null)
//                     {
//                         var remote = remoteOverride ?? Socket?.RemoteEndPoint?.ToString() ?? "Unknown";
//                         parent.MessageReceived?.Invoke(data, remote);
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     parent.Debug?.Invoke($"Invalid JSON: {ex.Message}");
//                 }
//             }
//         }
//     }

//     private sealed class TcpJsonClient : TcpClient
//     {
//         private readonly TcpMessagingService parent;
//         private readonly StringBuilder buffer = new();
//         public TcpJsonClient(string host, int port, TcpMessagingService parent) : base(host, port)
//             => this.parent = parent;

//         protected override void OnConnected() => parent.Debug?.Invoke("Client connected.");
//         protected override void OnDisconnected() => parent.Debug?.Invoke("Client disconnected.");

//         protected override void OnReceived(byte[] data, long offset, long size)
//         {
//             buffer.Append(Encoding.UTF8.GetString(data, (int)offset, (int)size));
//             DrainBuffer();
//         }

//         private void DrainBuffer()
//         {
//             while (true)
//             {
//                 var text = buffer.ToString();
//                 var newlineIndex = text.IndexOf('\n');
//                 if (newlineIndex < 0)
//                     break;

//                 var line = text[..newlineIndex].Trim();
//                 buffer.Remove(0, newlineIndex + 1);

//                 if (line.Length == 0)
//                     continue;

//                 try
//                 {
//                     var data = JsonConvert.DeserializeObject<TelemetryData>(line);
//                     if (data != null)
//                     {
//                         var remote = Socket?.RemoteEndPoint?.ToString() ?? "Server";
//                         parent.MessageReceived?.Invoke(data, remote);
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     parent.Debug?.Invoke($"Invalid JSON: {ex.Message}");
//                 }
//             }
//         }
//     }
    
// }
