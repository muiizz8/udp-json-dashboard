using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ChatCore.Models;

public sealed class ChatMessage : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string MessageId { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime TimeStamp { get; set; }
    public bool IsSent { get; set; }
    public bool IsReceived => !IsSent;
    public string Remote { get; set; } = "";
    public MessageType MessageType { get; set; } = MessageType.Message;
    public bool RequiresYesNo { get; set; }

    private bool _isDelivered;
    /// <summary>True once the remote side has acknowledged receipt of this sent message.</summary>
    public bool IsDelivered
    {
        get => _isDelivered;
        set
        {
            if (_isDelivered == value) return;
            _isDelivered = value;
            OnPropertyChanged();
        }
    }

    /// <summary>True when this received message has pending Yes/No options for the user to respond to.</summary>
    public bool HasYesNoOptions => IsReceived && RequiresYesNo;

    /// <summary>Short label shown on the bubble (empty for plain messages).</summary>
    public string TypeLabel => MessageType switch
    {
        MessageType.MachineRequest  => "MR_Req",
        MessageType.MachineResponse => "MR_Rec",
        MessageType.PilotRequest    => "PR_Req",
        MessageType.PilotResponse   => "PR_Rec",
        _                           => ""
    };

    public bool HasTypeLabel => MessageType != MessageType.Message;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
