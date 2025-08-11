using System.Collections.Generic;
using System.Linq;
using SecureChat.Model;

namespace SecureChat;

public class MainWindowModel {
	private readonly List<Chat> _chats = [];

	public string DisplayName = "";

	public void AddChat(Chat chat) {
		_chats.Add(chat);
		chat.UpdateName(chat.Name);
	}

	public bool ContainsChat(long id) => _chats.Any(chat => chat.Id == id);

	public bool ContainsPersonalChat(string key) => _chats.Any(chat => chat.KeyString == key);
	
	public void UpdateName(long id, string name) => _chats.First(chat => chat.Id == id).UpdateName(name);

	public Chat? GetChat(long id) => _chats.Find(chat => chat.Id == id);
	
	public Chat? GetPersonalChat(string key) => _chats.Find(chat => chat.KeyString == key);
	
	public void SetChatReadStatus(long id, bool isRead) => GetChat(id)!.ChatButton.Tag = isRead ? "" : "\u25cf";
	
	public bool GetChatReadStatus(long id) => (string) GetChat(id)!.ChatButton.Tag! == "";
}