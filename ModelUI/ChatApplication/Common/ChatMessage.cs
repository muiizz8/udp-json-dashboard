using System;

namespace ChatApplication.Common;

public sealed class ChatMessage
{
    public string Text { get; set; } = "";
    public DateTime TimeStamp { get; set; }
    public bool IsSent { get; set; }
    public bool IsReceived => !IsSent;
    public string Remote { get; set; } = "";
}
