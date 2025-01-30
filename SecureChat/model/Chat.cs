using Avalonia.Controls;

namespace SecureChat.model;

public class Chat {
	public Button ChatButton;
	public string Name;
	public string Modulus;

	public Chat(Button chatButton, string name, string modulus) {
		ChatButton = chatButton;
		Name = name;
		Modulus = modulus;
	}

	public void UpdateName(string name) {
		Name = name;
		ChatButton.Content = Name;
	}
}