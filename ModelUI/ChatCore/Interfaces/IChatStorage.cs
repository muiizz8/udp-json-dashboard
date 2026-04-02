using System.Collections.Generic;
using ChatCore.Models;

namespace ChatCore.Interfaces;

public interface IChatStorage
{
    void SaveMessage(ChatMessage message);
    IEnumerable<ChatMessage> LoadMessages();
    void ClearMessages();
    IEnumerable<string> GetContacts();
}
