using System.Collections.Generic;
using System.Linq;
using SecureChat.model;

namespace SecureChat;

public class MainWindowModel {
	private readonly List<Chat> _chats = [];

	public void AddChat(Chat chat) => _chats.Add(chat);

	public bool ContainsChat(string name) => _chats.Any(chat => chat.Name == name);

	public Chat? GetChat(string name) => _chats.Find(chat => chat.Name == name);
	
	public void SetChatReadStatus(string name, bool isRead) => GetChat(name)!.ChatButton.Tag = isRead ? "" : "\u25cf";
}