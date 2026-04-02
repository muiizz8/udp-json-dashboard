using System;
using Newtonsoft.Json;

namespace ChatCore.Models;

/// <summary>
/// JSON structure sent over the wire. Wraps the text with type metadata.
/// </summary>
public sealed class WireMessage
{
    [JsonProperty("msgType")]
    public string MsgType { get; set; } = nameof(MessageType.Message);

    [JsonProperty("text")]
    public string Text { get; set; } = "";

    [JsonProperty("requiresYesNo")]
    public bool RequiresYesNo { get; set; }

    /// <summary>Unique ID of this message (sender-assigned GUID). Used for delivery acknowledgment.</summary>
    [JsonProperty("msgId")]
    public string MsgId { get; set; } = "";

    public static string Serialize(ChatMessage msg)
    {
        return JsonConvert.SerializeObject(new WireMessage
        {
            MsgType = msg.MessageType.ToString(),
            Text = msg.Text,
            RequiresYesNo = msg.RequiresYesNo,
            MsgId = msg.MessageId
        });
    }

    /// <summary>Serializes a bare Ack for the given original message ID.</summary>
    public static string SerializeAck(string originalMsgId)
    {
        return JsonConvert.SerializeObject(new WireMessage
        {
            MsgType = nameof(MessageType.Ack),
            Text = "",
            MsgId = originalMsgId
        });
    }

    /// <summary>
    /// Parses an incoming wire string. Falls back gracefully if it is plain text (legacy).
    /// </summary>
    public static (string text, MessageType type, bool requiresYesNo, string messageId) Parse(string raw)
    {
        if (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith('{'))
        {
            try
            {
                var w = JsonConvert.DeserializeObject<WireMessage>(raw);
                if (w != null)
                {
                    var type = Enum.TryParse<MessageType>(w.MsgType, out var t) ? t : MessageType.Message;
                    if (type == MessageType.Ack || !string.IsNullOrEmpty(w.Text))
                        return (w.Text, type, w.RequiresYesNo, w.MsgId ?? "");
                }
            }
            catch { /* fall through to plain text */ }
        }

        return (raw, MessageType.Message, false, "");
    }
}
