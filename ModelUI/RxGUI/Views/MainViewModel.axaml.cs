using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using RxGUI;
using RxGUI.Common;
using RxGUI.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RxGUI.Views;

/// <summary>
/// ViewModel backing the main UI.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly IniConfigService config;
    private UdpMessagingService Udp = null!;
    private bool isServerRunning;

    public ObservableCollection<LogEntry> Logs { get; } = new();
    public ObservableCollection<string> DebugLines { get; } = new();

    public ObservableCollection<InstanceConfig> Instances { get; } = new();

    private InstanceConfig? selectedInstance;
    public InstanceConfig? SelectedInstance
    {
        get => selectedInstance;
        set
        {
            if (selectedInstance == value)
                return;

            selectedInstance = value;
            OnPropertyChanged();
            RecreateUdpService();
        }
    }

    private LogEntry? selectedLog;
    public LogEntry? SelectedLog
    {
        get => selectedLog;
        set { selectedLog = value; OnPropertyChanged(); SelectedJson = selectedLog?.Json ?? ""; }
    }

    private string selectedJson = "";
    public string SelectedJson
    {
        get => selectedJson;
        set { selectedJson = value; OnPropertyChanged(); }
    }

    private string jsonEditorText = @"{
  ""m_airborneInd"": false,
  ""m_hour"": 0,
  ""m_minute"": 0,
  ""m_vcs"": """",
  ""m_srcTN"": 418,
  ""m_latitude"": 33.03,
  ""m_longitude"": 73.64,
  ""m_altitude"": 600.0,
  ""m_course"": 21.0,
  ""m_speed"": 0.0
}";
    public string JsonEditorText
    {
        get => jsonEditorText;
        set { jsonEditorText = value; OnPropertyChanged(); }
    }

    public MainViewModel()
    {
        config = new IniConfigService();
        LoadConfig();
        CreateTcpService();
    }

    /// <summary>Starts the local TCP server.</summary>
    public void StartServer()
    {
        RecreateUdpService();
        if (!EnsureTcpReady("start server"))
            return;

        Udp.StartServer();
        isServerRunning = true;
        NotifyInfo("Server started.", "Server");
    }
    /// <summary>Stops the local TCP server.</summary>
    public void StopServer()
    {
        if (!EnsureTcpReady("stop server"))
            return;

        Udp.StopServer();
        isServerRunning = false;
        NotifyInfo("Server stopped.", "Server");
    }
    /// <summary>Connects the TCP client to remote endpoint.</summary>
    public void ConnectClient()
    {
        if (!EnsureTcpReady("connect client"))
            return;

        AddDebug("UDP client is connectionless; ready to send.");
        NotifyInfo("Client ready.", "Client");
    }

    /// <summary>Disconnects the TCP client from remote endpoint.</summary>
    public void DisconnectClient()
    {
        if (!EnsureTcpReady("disconnect client"))
            return;

        AddDebug("UDP client is connectionless; nothing to disconnect.");
        NotifyInfo("Client disconnected.", "Client");
    }

    /// <summary>Persists edited configuration values to Config.inf.</summary>
    public void SaveConfig()
    {
        if (SelectedInstance == null)
        {
            AddDebug("No instance selected.");
            return;
        }

        if (!TryParsePort(SelectedInstance.LocalPort, out var parsedLocalPort) ||
            !TryParsePort(SelectedInstance.RemotePort, out var parsedRemotePort))
        {
            AddDebug("Config save failed: invalid port value.");
            return;
        }

        config.InstanceId = SelectedInstance.Name;
        config.SaveInstances(Instances);
        AddDebug("Config saved.");
        RecreateUdpService();

    }

    public void SendJson()
    {
        // Validate JSON editor content
        if (!TryParseJson(JsonEditorText, out var token))
        {
            AddDebug("Send failed: JSON is invalid.");
            NotifyError("Send failed: JSON is invalid.", "Send JSON");
            return;
        }

        if (!EnsureTcpReady("send JSON"))
            return;

        string jsonString = JsonConvert.SerializeObject(token, Formatting.Indented);
        Udp.Send(jsonString);
        NotifyInfo("Telemetry JSON sent.", "Send JSON");

        Logs.Add(new LogEntry
        {
            TimeStampUtc = DateTime.UtcNow,
            Direction = "TX",
            Remote = SelectedInstance == null ? "" : $"{SelectedInstance.RemoteIp}:{SelectedInstance.RemotePort}",
            Json = jsonString
        });
    }

    private void LoadConfig()
    {
        Instances.Clear();
        foreach (var instance in config.LoadInstances())
            Instances.Add(instance);

        SelectedInstance = Instances.Count > 0 ? Instances[0] : null;
    }

    private void CreateTcpService()
    {
        if (SelectedInstance == null)
        {
            AddDebug("No instance selected. TCP service not started.");
            return;
        }

        if (!TryParsePort(SelectedInstance.LocalPort, out var parsedLocalPort) ||
            !TryParsePort(SelectedInstance.RemotePort, out var parsedRemotePort))
        {
            parsedLocalPort = 9000;
            parsedRemotePort = 9001;
            AddDebug("Invalid port in selected instance. Using defaults 9000/9001.");
        }

        Udp = new UdpMessagingService(
            SelectedInstance.LocalIp,
            parsedLocalPort,
            SelectedInstance.RemoteIp,
            parsedRemotePort
        );
        AddDebug($"UDP service ready. Local={SelectedInstance.LocalIp}:{parsedLocalPort} Remote={SelectedInstance.RemoteIp}:{parsedRemotePort}");

        Udp.MessageReceived += (json, remote) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                Logs.Add(new LogEntry
                {
                    TimeStampUtc = DateTime.UtcNow,
                    Direction = "RX",
                    Remote = remote,
                    Json = json
                });
            });
        };

        Udp.Debug += message => Dispatcher.UIThread.Post(() => AddDebug(message));
    }

    private void RecreateUdpService()
    {
        var wasRunning = isServerRunning;
        if (Udp is not null && wasRunning)
            Udp.StopServer();

        isServerRunning = false;
        CreateTcpService();

        if (wasRunning && Udp is not null)
        {
            Udp.StartServer();
            isServerRunning = true;
        }
    }
    

    private void AddDebug(string message)
    {
        DebugLines.Add($"[{DateTime.UtcNow:HH:mm:ss}] {message}");
    }

    private bool EnsureTcpReady(string action)
    {
        if (Udp is not null)
            return true;

        var message = $"Cannot {action}: Udp service is not initialized.";
        AddDebug(message);
        NotifyError(message, "Udp");
        return false;
    }

    private void NotifyInfo(string message, string? title)
        => ApplicationViewModel.Instance.MessageService?.ShowMessage(message, title, NotificationType.Information);

    private void NotifyError(string message, string? title)
        => ApplicationViewModel.Instance.MessageService?.ShowMessage(message, title, NotificationType.Error);

    private static bool TryParseJson(string jsonInput, out JToken token)
    {
        token = null!;
        try
        {
            token = JToken.Parse(jsonInput);
            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }

    private static bool TryParsePort(string input, out int port)
        => int.TryParse(input, out port) && port > 0 && port <= 65535;

    /// <summary>Adds a new instance with default values.</summary>
    public void AddInstance()
    {
        var index = Instances.Count + 1;
        var name = $"Instance{index}";
        Instances.Add(new InstanceConfig { Name = name });
        SelectedInstance = Instances[^1];
    }

    /// <summary>Removes the selected instance.</summary>
    public void RemoveSelectedInstance()
    {
        if (SelectedInstance == null)
            return;

        var toRemove = SelectedInstance;
        Instances.Remove(toRemove);
        SelectedInstance = Instances.Count > 0 ? Instances[0] : null;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
