using System.Collections.Generic;
using System.Linq;
using SecureChat.Model;

namespace SecureChat;

public class MainWindowModel {
	private readonly List<Chat> _chats = [];

	public void AddChat(Chat chat) {
		_chats.Add(chat);
		chat.UpdateName(chat.Name);
	}

	public bool ContainsChat(string key) => _chats.Any(chat => chat.Key == key);
	
	public void UpdateName(string key, string name) => _chats.First(chat => chat.Key == key).UpdateName(name);

	public Chat? GetChat(string key) => _chats.Find(chat => chat.Key == key);
	
	public void SetChatReadStatus(string key, bool isRead) => GetChat(key)!.ChatButton.Tag = isRead ? "" : "\u25cf";
	
	public bool GetChatReadStatus(string key) => (string) GetChat(key)!.ChatButton.Tag! == "";

	public string DisplayName = "";
}