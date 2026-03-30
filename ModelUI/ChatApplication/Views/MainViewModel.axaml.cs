using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChatApplication.Common;
using ChatApplication.Controllers;
using Newtonsoft.Json;

namespace ChatApplication.Views;

public sealed class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly MainController _controller;

    public ObservableCollection<LogEntry> Logs { get; } = new();
    public ObservableCollection<string> DebugLines { get; } = new();
    public ObservableCollection<InstanceConfig> Instances { get; } = new();

    private InstanceConfig? selectedInstance;
    public InstanceConfig? SelectedInstance
    {
        get => selectedInstance;
        set
        {
            if (selectedInstance == value) return;
            selectedInstance = value;
            OnPropertyChanged();
            _controller.OnInstanceChanged(value);
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

    public MainViewModel()
    {
        _controller = new MainController(Logs, DebugLines, Instances);
        _controller.SelectedInstanceChanged += instance =>
        {
            selectedInstance = instance;
            OnPropertyChanged(nameof(SelectedInstance));
        };

        // Sync initial selected instance without triggering controller callback
        selectedInstance = _controller.SelectedInstance;
        SelectedJson = JsonConvert.SerializeObject(new TelemetryData());
    }

    public void StartServer() => _controller.StartServer();
    public void StopServer() => _controller.StopServer();
    public void ConnectClient() => _controller.ConnectClient();
    public void DisconnectClient() => _controller.DisconnectClient();
    public void SaveConfig() => _controller.SaveConfig();
    public void SendJson() => _controller.SendJson();
    public void AddInstance() => _controller.AddInstance();
    public void RemoveSelectedInstance() => _controller.RemoveSelectedInstance();

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
