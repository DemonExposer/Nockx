using Avalonia.Controls;

namespace SecureChat.util;

public class MessageTextBlock : SelectableTextBlock {
	public long Id { get; init; }
	public string Sender { get; init; }
	public string Receiver { get; init; }
}