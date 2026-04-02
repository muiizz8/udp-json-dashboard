using System;
using System.Collections.Generic;
using ChatCore.Interfaces;
using ChatCore.Models;

namespace ChatCore.Engine;

public sealed class ChatEngine
{
    private readonly IChatStorage _storage;
    private readonly IConfigProvider _configProvider;
    private IMessagingTransport? _transport;
    private InstanceConfig? _currentInstance;
    private string _currentProtocol = "UDP";

    // Events — subscribe in your UI or service layer
    public event Action<ChatMessage>? MessageAdded;
    public event Action<string>? ContactAdded;
    public event Action<string>? DebugMessageAdded;
    public event Action<string>? ConnectionStatusChanged;
    public event Action<string>? MessageDelivered;   // delivers msgId

    public InstanceConfig? CurrentInstance => _currentInstance;
    public string CurrentProtocol => _currentProtocol;

    public ChatEngine(IChatStorage storage, IConfigProvider configProvider)
    {
        _storage = storage;
        _configProvider = configProvider;
    }

    public void SetTransport(IMessagingTransport transport)
    {
        if (_transport != null)
        {
            _transport.DebugMessage -= OnDebug;
            _transport.MessageReceived -= OnMessageReceived;
            _transport.Dispose();
        }
        _transport = transport;
        _transport.DebugMessage += OnDebug;
        _transport.MessageReceived += OnMessageReceived;
        _currentProtocol = transport.Protocol;
    }

    public IEnumerable<InstanceConfig> GetInstances() => _configProvider.GetInstances();

    public void SetCurrentInstance(InstanceConfig instance)
    {
        _currentInstance = instance;
    }

    public IEnumerable<ChatMessage> LoadHistory()
    {
        return _storage.LoadMessages();
    }

    public IEnumerable<string> LoadContacts()
    {
        return _storage.GetContacts();
    }

    public void StartServer()
    {
        if (_currentInstance == null) { OnDebug("No instance selected."); return; }
        if (!TryParseEndpoint(_currentInstance.LocalIp, _currentInstance.LocalPort, out var ip, out var port)) return;
        _transport?.StartServer(ip, port);
        ConnectionStatusChanged?.Invoke("Listening");
    }

    public void StopServer()
    {
        _transport?.StopServer();
        ConnectionStatusChanged?.Invoke("Stopped");
    }

    public void Connect()
    {
        if (_currentInstance == null) { OnDebug("No instance selected."); return; }
        if (!TryParseEndpoint(_currentInstance.RemoteIp, _currentInstance.RemotePort, out var ip, out var port)) return;
        _transport?.Connect(ip, port);
        ConnectionStatusChanged?.Invoke("Connected");
    }

    public void Disconnect()
    {
        _transport?.Disconnect();
        ConnectionStatusChanged?.Invoke("Disconnected");
    }

    public void SendMessage(string text, MessageType type = MessageType.Message, bool requiresYesNo = false)
    {
        if (_currentInstance == null) { OnDebug("No instance selected."); return; }
        var remote = $"{_currentInstance.RemoteIp}:{_currentInstance.RemotePort}";

        var msg = new ChatMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Text = text,
            TimeStamp = DateTime.UtcNow,
            IsSent = true,
            Remote = remote,
            MessageType = type,
            RequiresYesNo = requiresYesNo
        };

        var wire = WireMessage.Serialize(msg);
        _transport?.Send(wire);
        _storage.SaveMessage(msg);
        MessageAdded?.Invoke(msg);
        ContactAdded?.Invoke(remote);
    }

    public void SendResponse(ChatMessage originalMsg, string response)
    {
        var responseType = originalMsg.MessageType switch
        {
            MessageType.MachineRequest => MessageType.PilotResponse,
            MessageType.PilotRequest   => MessageType.MachineResponse,
            _                          => MessageType.Message
        };
        SendMessage(response, responseType, false);
        // Update original message to remove the yes/no prompt
        originalMsg.RequiresYesNo = false;
    }

    public void ClearHistory()
    {
        _storage.ClearMessages();
    }

    // ── Private ──────────────────────────────────────────────────────────

    private void OnDebug(string text) => DebugMessageAdded?.Invoke(text);

    private void OnMessageReceived(string raw, string remote)
    {
        var (text, msgType, requiresYesNo, msgId) = WireMessage.Parse(raw);

        if (msgType == MessageType.Ack)
        {
            MessageDelivered?.Invoke(msgId);
            return;
        }

        // Auto-ack
        if (!string.IsNullOrEmpty(msgId))
            _transport?.Send(WireMessage.SerializeAck(msgId));

        var msg = new ChatMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Text = text,
            TimeStamp = DateTime.UtcNow,
            IsSent = false,
            Remote = remote,
            MessageType = msgType,
            RequiresYesNo = requiresYesNo
        };

        _storage.SaveMessage(msg);
        MessageAdded?.Invoke(msg);
        ContactAdded?.Invoke(remote);
    }

    private static bool TryParseEndpoint(string ip, string portStr, out string outIp, out int outPort)
    {
        outIp = ip;
        if (!int.TryParse(portStr, out outPort) || outPort < 1 || outPort > 65535)
            return false;
        return true;
    }
}
