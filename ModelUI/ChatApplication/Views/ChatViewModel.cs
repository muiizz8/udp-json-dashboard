using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChatApplication.Common;
using ChatApplication.Controllers;

namespace ChatApplication.Views;

public sealed class ChatViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly ChatController _controller;

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<string> DebugLines { get; } = new();
    public ObservableCollection<InstanceConfig> Instances { get; } = new();
    public List<string> Protocols { get; } = new() { "UDP", "TCP" };

    private string selectedProtocol = "UDP";
    public string SelectedProtocol
    {
        get => selectedProtocol;
        set
        {
            if (selectedProtocol == value) return;
            _controller.StopServerIfRunning();
            selectedProtocol = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTcp));
        }
    }

    public bool IsTcp => selectedProtocol == "TCP";

    private InstanceConfig? selectedInstance;
    public InstanceConfig? SelectedInstance
    {
        get => selectedInstance;
        set
        {
            selectedInstance = value;
            _controller.SelectedInstance = value;
            OnPropertyChanged();
        }
    }

    private string messageText = "";
    public string MessageText
    {
        get => messageText;
        set { messageText = value; OnPropertyChanged(); }
    }

    private string connectionStatus = "Not connected";
    public string ConnectionStatus
    {
        get => connectionStatus;
        set { connectionStatus = value; OnPropertyChanged(); }
    }

    public ChatViewModel()
    {
        _controller = new ChatController(Messages, DebugLines, Instances,
            status => ConnectionStatus = status);

        // Sync initial selected instance
        selectedInstance = _controller.SelectedInstance;
    }

    public void StartServer() => _controller.StartServer(selectedProtocol);
    public void StopServer() => _controller.StopServer();
    public void ConnectClient() => _controller.ConnectClient(selectedProtocol);
    public void DisconnectClient() => _controller.DisconnectClient(selectedProtocol);

    public void SendMessage()
    {
        var text = MessageText.Trim();
        if (string.IsNullOrEmpty(text)) return;

        MessageText = "";
        var remote = SelectedInstance == null ? "" : $"{SelectedInstance.RemoteIp}:{SelectedInstance.RemotePort}";
        _controller.SendMessage(selectedProtocol, text, remote);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
