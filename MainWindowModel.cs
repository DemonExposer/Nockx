using System.Collections.Generic;

namespace SecureChat;

public class MainWindowModel {
	private readonly List<string> _chats = new ();

	public void AddChat(string name) => _chats.Add(name);

	public bool ContainsChat(string name) => _chats.Contains(name);
}