using System;
using System.Collections.ObjectModel;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using ChatApplication.Common;
using ChatApplication.Implementations.Config;
using ChatApplication.Implementations.Transports;
using ChatCore.Models;
using Newtonsoft.Json;

namespace ChatApplication.Controllers;

public sealed class MainController
{
    private readonly IniConfigProvider _config;
    private readonly ObservableCollection<LogEntry> _logs;
    private readonly ObservableCollection<string> _debugLines;
    private readonly ObservableCollection<InstanceConfig> _instances;

    private UdpTransport _udp = null!;
    private bool _isServerRunning;

    public InstanceConfig? SelectedInstance { get; private set; }

    /// <summary>Raised when the controller changes which instance should be selected (e.g. after add/remove).</summary>
    public event Action<InstanceConfig?>? SelectedInstanceChanged;

    public MainController(
        ObservableCollection<LogEntry> logs,
        ObservableCollection<string> debugLines,
        ObservableCollection<InstanceConfig> instances)
    {
        _config = new IniConfigProvider();
        _logs = logs;
        _debugLines = debugLines;
        _instances = instances;
        LoadConfig();
        CreateUdpService();
    }

    public void StartServer()
    {
        RecreateUdpService();
        if (!EnsureUdpReady("start server")) return;

        if (SelectedInstance == null) { AddDebug("No instance selected."); return; }
        if (!TryParsePort(SelectedInstance.LocalPort, out var localPort))
        {
            AddDebug("Invalid local port."); return;
        }
        _udp.StartServer(SelectedInstance.LocalIp, localPort);
        _isServerRunning = true;
        NotifyInfo("Server started.", "Server");
    }

    public void StopServer()
    {
        if (!EnsureUdpReady("stop server")) return;
        _udp.StopServer();
        _isServerRunning = false;
        NotifyInfo("Server stopped.", "Server");
    }

    public void ConnectClient()
    {
        if (!EnsureUdpReady("connect client")) return;
        AddDebug("UDP client is connectionless; ready to send.");
        NotifyInfo("Client ready.", "Client");
    }

    public void DisconnectClient()
    {
        if (!EnsureUdpReady("disconnect client")) return;
        AddDebug("UDP client is connectionless; nothing to disconnect.");
        NotifyInfo("Client disconnected.", "Client");
    }

    public void SaveConfig()
    {
        if (SelectedInstance == null) { AddDebug("No instance selected."); return; }

        if (!TryParsePort(SelectedInstance.LocalPort, out _) ||
            !TryParsePort(SelectedInstance.RemotePort, out _))
        {
            AddDebug("Config save failed: invalid port value.");
            return;
        }

        _config.InstanceId = SelectedInstance.Name;
        _config.SaveInstances(_instances);
        AddDebug("Config saved.");
        RecreateUdpService();
    }

    public void SendJson()
    {
        if (!EnsureUdpReady("send JSON")) return;

        var token = new TelemetryData
        {
            m_airborneInd = true,
            m_vcs = "123",
            m_latitude = 33.333,
            m_longitude = 73.333,
            m_altitude = 10000
        };

        string jsonString = JsonConvert.SerializeObject(token);
        _udp.Send(jsonString);
        NotifyInfo("Telemetry JSON sent.", "Send JSON");

        _logs.Add(new LogEntry
        {
            TimeStampUtc = DateTime.UtcNow,
            Direction = "TX",
            Remote = SelectedInstance == null ? "" : $"{SelectedInstance.RemoteIp}:{SelectedInstance.RemotePort}",
            Json = jsonString
        });
    }

    public void AddInstance()
    {
        var name = $"Instance{_instances.Count + 1}";
        _instances.Add(new InstanceConfig { Name = name });
        SelectedInstance = _instances[^1];
        SelectedInstanceChanged?.Invoke(SelectedInstance);
    }

    public void RemoveSelectedInstance()
    {
        if (SelectedInstance == null) return;
        _instances.Remove(SelectedInstance);
        SelectedInstance = _instances.Count > 0 ? _instances[0] : null;
        SelectedInstanceChanged?.Invoke(SelectedInstance);
    }

    public void OnInstanceChanged(InstanceConfig? instance)
    {
        SelectedInstance = instance;
        RecreateUdpService();
    }

    private void LoadConfig()
    {
        _instances.Clear();
        foreach (var instance in _config.GetInstances())
            _instances.Add(instance);
        SelectedInstance = _instances.Count > 0 ? _instances[0] : null;
    }

    private void CreateUdpService()
    {
        if (SelectedInstance == null)
        {
            AddDebug("No instance selected. UDP service not started.");
            return;
        }

        if (!TryParsePort(SelectedInstance.LocalPort, out var localPort) ||
            !TryParsePort(SelectedInstance.RemotePort, out var remotePort))
        {
            localPort = 9000;
            remotePort = 9001;
            AddDebug("Invalid port in selected instance. Using defaults 9000/9001.");
        }

        _udp = new UdpTransport();

        // Wire up remote endpoint so Send() knows where to deliver
        _udp.Connect(SelectedInstance.RemoteIp, remotePort);

        AddDebug($"UDP service ready. Local={SelectedInstance.LocalIp}:{localPort} Remote={SelectedInstance.RemoteIp}:{remotePort}");

        _udp.MessageReceived += (json, remote) =>
        {
            var telem = JsonConvert.DeserializeObject<TelemetryData>(json);
            if (telem != null) telem.m_vcs = "modified";

            Dispatcher.UIThread.Post(() =>
            {
                _logs.Add(new LogEntry
                {
                    TimeStampUtc = DateTime.UtcNow,
                    Direction = "RX",
                    Remote = remote,
                    Json = JsonConvert.SerializeObject(telem)
                });
            });
        };

        _udp.DebugMessage += message => Dispatcher.UIThread.Post(() => AddDebug(message));
    }

    private void RecreateUdpService()
    {
        var wasRunning = _isServerRunning;
        if (_udp is not null && wasRunning)
            _udp.StopServer();

        _isServerRunning = false;
        CreateUdpService();

        if (wasRunning && _udp is not null && SelectedInstance != null &&
            TryParsePort(SelectedInstance.LocalPort, out var localPort))
        {
            _udp.StartServer(SelectedInstance.LocalIp, localPort);
            _isServerRunning = true;
        }
    }

    private void AddDebug(string message)
        => _debugLines.Add($"[{DateTime.UtcNow:HH:mm:ss}] {message}");

    private bool EnsureUdpReady(string action)
    {
        if (_udp is not null) return true;
        var message = $"Cannot {action}: UDP service is not initialized.";
        AddDebug(message);
        NotifyError(message, "Udp");
        return false;
    }

    private static void NotifyInfo(string message, string? title)
        => ApplicationViewModel.Instance.MessageService?.ShowMessage(message, title, NotificationType.Information);

    private static void NotifyError(string message, string? title)
        => ApplicationViewModel.Instance.MessageService?.ShowMessage(message, title, NotificationType.Error);

    private static bool TryParsePort(string input, out int port)
        => int.TryParse(input, out port) && port > 0 && port <= 65535;
}
