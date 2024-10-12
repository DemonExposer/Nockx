using Avalonia.Controls;

namespace SecureChat.model;

public class Chat {
	public Button ChatButton;
	public string Name;

	public Chat(Button chatButton, string name) {
		ChatButton = chatButton;
		Name = name;
	}
}