using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Org.BouncyCastle.Crypto.Parameters;
using System.Collections.Generic;
using Avalonia.Threading;
using SecureChat.util;

namespace SecureChat.panels;

public class ChatPanel : DockPanel {
	private readonly ChatPanelController _controller = new ();
	private readonly List<SelectableTextBlock> _messages = new ();

	private StackPanel? _messagePanel;

	public ChatPanel() {
		Dispatcher.UIThread.InvokeAsync(() => {
			using IEnumerator<ILogical> enumerator = this.GetLogicalDescendants().GetEnumerator();
			while (enumerator.MoveNext()) {
				if (enumerator.Current.GetType() == typeof(TextBox)) {
					TextBox textBox = (TextBox) enumerator.Current;
					if (textBox.Name == "MessageBox") {
						textBox.KeyDown += (_, args) => {
							if (args.Key == Key.Enter) {
								if (textBox.Text == null)
									return;

								_controller.Send(textBox.Text);
							}
						};
					}
				} else if (enumerator.Current.GetType() == typeof(StackPanel)) {
					StackPanel stackPanel = (StackPanel) enumerator.Current;
					if (stackPanel.Name == "MessagePanel")
						_messagePanel = stackPanel;
				}
			}
		});
	}

	private void AddMessage(string text) {
		if (_messagePanel == null) {
			Console.WriteLine("Show was called before initialization. Not displaying anything");
			return;
		}

		SelectableTextBlock messageTextBlock = new () {
			Text = text,
			Margin = new Thickness(5)
		};
		_messages.Add(messageTextBlock);
		_messagePanel.Children.Add(messageTextBlock);
	}

	public void DecryptAndAddMessage(Message message) => AddMessage(_controller.Decrypt(message, false));

	public void Show(string username, RsaKeyParameters publicKey) {
		// _messagePanel should never be null, because a user cannot open this panel before the UI is done.
		// However, in theory, when the program is loaded from a saved state, it is theoretically possible to trigger this. So this is just for debug.
		if (_messagePanel == null) {
			Console.WriteLine("Show was called before initialization. Not displaying anything");
			return;
		}
		
		_controller.ForeignPublicKey = publicKey;
		
		_messagePanel.Children.RemoveAll(_messages);
		_messages.Clear();
					
		string[] messages = _controller.GetPastMessages();

		foreach (string message in messages) {
			string fullMessage = $"{username} | {message}";
			AddMessage(fullMessage);
		}
	}

	public RsaKeyParameters GetForeignPublicKey() => _controller.ForeignPublicKey;
}
