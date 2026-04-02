using System;

namespace ChatCore.Interfaces;

public interface IMessagingTransport : IDisposable
{
    string Protocol { get; }
    bool IsListening { get; }
    bool IsConnected { get; }

    event Action<string>? DebugMessage;
    event Action<string, string>? MessageReceived;  // (message, remoteEndpoint)

    void StartServer(string localIp, int localPort);
    void StopServer();
    void Connect(string remoteIp, int remotePort);
    void Disconnect();
    void Send(string message);
}
