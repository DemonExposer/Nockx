using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Org.BouncyCastle.Crypto.Parameters;
using System.Collections.Generic;

namespace SecureChat.panels;
public class ChatPanel : DockPanel {
	private ChatPanelController _controller;

	public ChatPanel() {
		_controller = new ChatPanelController();
	}

	public void Show(string username, RsaKeyParameters publicKey) {
		_controller.ForeignPublicKey = publicKey;

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
				if (stackPanel.Name == "MessagePanel") {
					string[] messages = _controller.GetPastMessages();

					foreach (string message in messages) {
						string fullMessage = $"{username} | {message}";
						TextBlock messageTextBlock = new () {
							Text = fullMessage,
							Margin = new Thickness(5)
						};
						stackPanel.Children.Add(messageTextBlock);
					}
				}
			}
		}
	}
}
