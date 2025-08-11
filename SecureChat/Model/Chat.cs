using Avalonia.Controls;
using Org.BouncyCastle.Crypto.Parameters;
using SecureChat.ClassExtensions;

namespace SecureChat.Model;

public class Chat(Button chatButton, long id, string name, RsaKeyParameters key, string keyString) {
	public Button ChatButton = chatButton;
	public long Id = id;
	public string Name = name;
	public string KeyString = keyString;
	public RsaKeyParameters Key = key;

	public void UpdateName(string name) {
		Name = name;
		ChatButton.Content = Name.Crop(20);
	}
}