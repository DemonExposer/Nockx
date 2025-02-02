using System.Collections.Generic;
using System.Linq;
using SecureChat.model;

namespace SecureChat;

public class MainWindowModel {
	private readonly List<Chat> _chats = [];

	public void AddChat(Chat chat) => _chats.Add(chat);

	public bool ContainsChat(string modulus) => _chats.Any(chat => chat.Modulus == modulus);
	
	public void UpdateName(string modulus, string name) => _chats.First(chat => chat.Modulus == modulus).UpdateName(name);

	public Chat? GetChat(string modulus) => _chats.Find(chat => chat.Modulus == modulus);
	
	public void SetChatReadStatus(string modulus, bool isRead) => GetChat(modulus)!.ChatButton.Tag = isRead ? "" : "\u25cf";
	
	public bool GetChatReadStatus(string modulus) => (string) GetChat(modulus)!.ChatButton.Tag! == "";

	public string DisplayName = "";
}