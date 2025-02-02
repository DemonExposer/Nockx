using System;

namespace SecureChat.model;

public class DecryptedMessage {
	public long Id;
	public string Body;
	public DateTime DateTime;
	public string Sender, DisplayName;
}