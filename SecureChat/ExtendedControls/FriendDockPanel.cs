﻿using Avalonia.Controls;

namespace SecureChat.ExtendedControls;

public class FriendDockPanel {
	public string SenderKey { get; set; }
	public string ReceiverKey { get; set; }
	public Border Border { get; set; }
	public DockPanel DockPanel { get; set; }
}