using System;

namespace SecureChat.Model;

public class DecryptedMessage {
	public long Id;
	public string Body;
	public DateTime DateTime;
	public string Sender, DisplayName;
}