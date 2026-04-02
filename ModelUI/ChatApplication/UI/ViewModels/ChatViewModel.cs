using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using ChatApplication.Implementations.Config;
using ChatApplication.Implementations.Storage;
using ChatApplication.Implementations.Transports;
using ChatCore;
using ChatCore.Engine;
using ChatCore.Interfaces;
using ChatCore.Models;

namespace ChatApplication.UI.ViewModels;

public sealed class ChatViewModel : INotifyPropertyChanged
{
    private readonly ChatEngine _engine;

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    public ObservableCollection<string> DebugLines { get; } = new();
    public ObservableCollection<InstanceConfig> Instances { get; } = new();
    public ObservableCollection<string> Contacts { get; } = new();

    public List<string> Protocols { get; } = ["UDP", "TCP"];
    public List<string> MessageTypeNames { get; } = ["MSG", "MR_Req", "MR_Rec", "PR_Req", "PR_Rec"];

    private string _selectedProtocol = "UDP";
    public string SelectedProtocol
    {
        get => _selectedProtocol;
        set
        {
            if (_selectedProtocol == value) return;
            _selectedProtocol = value;
            OnPropertyChanged();
            SwitchTransport(value);
        }
    }

    private string _selectedMessageTypeName = "MSG";
    public string SelectedMessageTypeName
    {
        get => _selectedMessageTypeName;
        set
        {
            _selectedMessageTypeName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRequestMessageType));
            // Clear RequiresYesNo when switching away from a request type
            if (!IsRequestMessageType) RequiresYesNo = false;
        }
    }

    public bool IsRequestMessageType =>
        SelectedMessageTypeName is "MR_Req" or "PR_Req";

    private bool _requiresYesNo;
    public bool RequiresYesNo
    {
        get => _requiresYesNo;
        set { _requiresYesNo = value; OnPropertyChanged(); }
    }

    private InstanceConfig? _selectedInstance;
    public InstanceConfig? SelectedInstance
    {
        get => _selectedInstance;
        set
        {
            _selectedInstance = value;
            OnPropertyChanged();
            if (value != null) _engine.SetCurrentInstance(value);
        }
    }

    private string _messageText = string.Empty;
    public string MessageText
    {
        get => _messageText;
        set { _messageText = value; OnPropertyChanged(); }
    }

    private string _connectionStatus = "Idle";
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set { _connectionStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsTcp)); }
    }

    public bool IsTcp => SelectedProtocol == "TCP";

    public ChatViewModel()
    {
        var storage = new SqliteChatStorage();
        var config = new IniConfigProvider();

        _engine = new ChatModuleBuilder()
            .WithStorage(storage)
            .WithConfig(config)
            .WithTransport(new UdpTransport())
            .Build();

        _engine.MessageAdded += msg =>
            Dispatcher.UIThread.Post(() =>
            {
                Messages.Add(msg);
                if (!Contacts.Contains(msg.Remote)) Contacts.Add(msg.Remote);
            });

        _engine.ContactAdded += remote =>
            Dispatcher.UIThread.Post(() =>
            {
                if (!Contacts.Contains(remote)) Contacts.Add(remote);
            });

        _engine.DebugMessageAdded += text =>
            Dispatcher.UIThread.Post(() => DebugLines.Add(text));

        _engine.ConnectionStatusChanged += status =>
            Dispatcher.UIThread.Post(() => ConnectionStatus = status);

        _engine.MessageDelivered += msgId =>
            Dispatcher.UIThread.Post(() =>
            {
                var msg = Messages.FirstOrDefault(m => m.MessageId == msgId);
                if (msg != null) msg.IsDelivered = true;
            });

        LoadInitialData();
    }

    private void LoadInitialData()
    {
        foreach (var instance in _engine.GetInstances())
            Instances.Add(instance);

        if (Instances.Count > 0)
        {
            SelectedInstance = Instances[0];
            _engine.SetCurrentInstance(Instances[0]);
        }

        foreach (var msg in _engine.LoadHistory())
            Messages.Add(msg);

        foreach (var contact in _engine.LoadContacts())
            if (!Contacts.Contains(contact)) Contacts.Add(contact);
    }

    private void SwitchTransport(string protocol)
    {
        IMessagingTransport transport = protocol == "TCP"
            ? new TcpTransport()
            : new UdpTransport();
        _engine.SetTransport(transport);
        OnPropertyChanged(nameof(IsTcp));
    }

    public void StartServer() => _engine.StartServer();
    public void StopServer() => _engine.StopServer();
    public void Connect() => _engine.Connect();
    public void Disconnect() => _engine.Disconnect();

    public void SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageText)) return;
        var msgType = SelectedMessageTypeName switch
        {
            "MR_Req" => MessageType.MachineRequest,
            "MR_Rec" => MessageType.MachineResponse,
            "PR_Req" => MessageType.PilotRequest,
            "PR_Rec" => MessageType.PilotResponse,
            _        => MessageType.Message
        };
        _engine.SendMessage(MessageText, msgType, RequiresYesNo);
        MessageText = string.Empty;
    }

    public void SendYesNoResponse(ChatMessage originalMsg, string response) =>
        _engine.SendResponse(originalMsg, response);

    public void ClearChatHistory()
    {
        _engine.ClearHistory();
        Messages.Clear();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
