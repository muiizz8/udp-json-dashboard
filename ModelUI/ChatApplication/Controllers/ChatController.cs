using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using ChatApplication.Common;
using ChatApplication.Network;

namespace ChatApplication.Controllers;

public sealed class ChatController
{
    private readonly ObservableCollection<ChatMessage> _messages;
    private readonly ObservableCollection<string> _debugLines;
    private readonly ObservableCollection<InstanceConfig> _instances;
    private readonly Action<string> _setConnectionStatus;

    private readonly IniConfigService _config;
    private UdpMessagingService? _udpService;
    private TcpMessagingService? _tcpService;
    private bool _isServerRunning;

    public InstanceConfig? SelectedInstance { get; set; }

    public ChatController(
        ObservableCollection<ChatMessage> messages,
        ObservableCollection<string> debugLines,
        ObservableCollection<InstanceConfig> instances,
        Action<string> setConnectionStatus)
    {
        _config = new IniConfigService();
        _messages = messages;
        _debugLines = debugLines;
        _instances = instances;
        _setConnectionStatus = setConnectionStatus;
        LoadConfig();
    }

    public void StopServerIfRunning()
    {
        if (_isServerRunning) StopServer();
    }

    public void StartServer(string protocol)
    {
        if (SelectedInstance == null) { AddDebug("No instance selected."); return; }

        if (!TryParsePort(SelectedInstance.LocalPort, out var localPort) ||
            !TryParsePort(SelectedInstance.RemotePort, out var remotePort))
        {
            AddDebug("Invalid port in config.");
            return;
        }

        if (protocol == "UDP")
        {
            _udpService = new UdpMessagingService(
                SelectedInstance.LocalIp, localPort,
                SelectedInstance.RemoteIp, remotePort);
            _udpService.MessageReceived += OnMessageReceived;
            _udpService.Debug += msg => Dispatcher.UIThread.Post(() => AddDebug(msg));
            _udpService.StartServer();
        }
        else
        {
            _tcpService = new TcpMessagingService(
                SelectedInstance.LocalIp, localPort,
                SelectedInstance.RemoteIp, remotePort);
            _tcpService.MessageReceived += OnMessageReceived;
            _tcpService.Debug += msg => Dispatcher.UIThread.Post(() => AddDebug(msg));
            _tcpService.StartServer();
        }

        _isServerRunning = true;
        _setConnectionStatus($"Listening on {SelectedInstance.LocalIp}:{SelectedInstance.LocalPort} ({protocol})");
        AddDebug($"{protocol} server started.");
    }

    public void StopServer()
    {
        _udpService?.StopServer();
        _tcpService?.StopServer();
        _isServerRunning = false;
        _setConnectionStatus("Not connected");
        AddDebug("Server stopped.");
    }

    public void ConnectClient(string protocol)
    {
        if (protocol == "TCP")
        {
            _tcpService?.ConnectClient();
            AddDebug("TCP client connecting...");
        }
        else
        {
            AddDebug("UDP is connectionless — ready to send.");
        }
    }

    public void DisconnectClient(string protocol)
    {
        if (protocol == "TCP")
        {
            _tcpService?.DisconnectClient();
            AddDebug("TCP client disconnected.");
        }
    }

    public void SendMessage(string protocol, string text, string remote)
    {
        if (protocol == "UDP" && _udpService != null)
            _udpService.Send(text);
        else if (protocol == "TCP" && _tcpService != null)
            _tcpService.Send(text);
        else
        {
            AddDebug("No active service. Press Start Server first.");
            return;
        }

        Dispatcher.UIThread.Post(() => _messages.Add(new ChatMessage
        {
            Text = text,
            TimeStamp = DateTime.Now,
            IsSent = true,
            Remote = remote
        }));
    }

    private void OnMessageReceived(string message, string remote)
    {
        Dispatcher.UIThread.Post(() => _messages.Add(new ChatMessage
        {
            Text = message,
            TimeStamp = DateTime.Now,
            IsSent = false,
            Remote = remote
        }));
    }

    private void LoadConfig()
    {
        _instances.Clear();
        foreach (var inst in _config.LoadInstances())
            _instances.Add(inst);
        SelectedInstance = _instances.Count > 0 ? _instances[0] : null;
    }

    private void AddDebug(string message)
        => _debugLines.Add($"[{DateTime.UtcNow:HH:mm:ss}] {message}");

    private static bool TryParsePort(string input, out int port)
        => int.TryParse(input, out port) && port > 0 && port <= 65535;
}
