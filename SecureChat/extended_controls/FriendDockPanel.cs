using Avalonia.Controls;

namespace SecureChat.extended_controls;

public class FriendDockPanel {
	public string SenderModulus { get; set; }
	public string SenderExponent { get; set; }
	public string ReceiverModulus { get; set; }
	public string ReceiverExponent { get; set; }
	public Border Border { get; set; }
	public DockPanel DockPanel { get; set; }
}