using Avalonia.Controls;
using SecureChat.ClassExtensions;

namespace SecureChat.Model;

public class Chat {
	public Button ChatButton;
	public string Name;
	public string Key;

	public Chat(Button chatButton, string name, string key) {
		ChatButton = chatButton;
		Name = name;
		Key = key;
	}

	public void UpdateName(string name) {
		Name = name;
		ChatButton.Content = Name.Crop(20);
	}
}