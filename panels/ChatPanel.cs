using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Org.BouncyCastle.Crypto.Parameters;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Threading;
using SecureChat.util;

namespace SecureChat.panels;

public class ChatPanel : DockPanel {
	private readonly ChatPanelController _controller;
	private readonly List<Border> _messages = new ();

	private StackPanel? _messagePanel;
	private ScrollViewer? _messageScrollView;

	private double _distanceScrolledFromBottom;

	private bool _isResized = false;
	private bool _isShowCalled = false;

	public ChatPanel() {
		_controller = new ChatPanelController(this);
		
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
								AddMessage(textBox.Text);
								textBox.Text = null;
							}
						};
					}
				} else if (enumerator.Current.GetType() == typeof(StackPanel)) {
					StackPanel stackPanel = (StackPanel) enumerator.Current;
					if (stackPanel.Name == "MessagePanel")
						_messagePanel = stackPanel;
				} else if (enumerator.Current.GetType() == typeof(ScrollViewer)) {
					ScrollViewer scrollViewer = (ScrollViewer) enumerator.Current;
					if (scrollViewer.Name == "MessageScrollView") {
						_messageScrollView = scrollViewer;
						_distanceScrolledFromBottom = _messageScrollView.ScrollBarMaximum.Y - _messageScrollView.Offset.Y;
						_messageScrollView.ScrollChanged += (sender, args) => {
							if (!_isResized)
								_distanceScrolledFromBottom = _messageScrollView.ScrollBarMaximum.Y - _messageScrollView.Offset.Y;
							else
								_isResized = false;
						};
					}
				}
			}
		});
	}
	
	private void AddMessage(string text) {
		if (_messagePanel == null) {
			Console.WriteLine("Show was called before initialization. Not displaying anything");
			return;
		}

		IBrush originalBackground = new SolidColorBrush(Color.Parse("Transparent"));
		Border border = new () {
			Background = originalBackground
		};
		
		SelectableTextBlock messageTextBlock = new () {
			Text = text,
			Margin = new Thickness(5),
			TextWrapping = TextWrapping.Wrap
		};

		MenuItem copyMenuItem = new () {
			Header = "Copy",
			Cursor = new Cursor(StandardCursorType.Arrow),
			Command = new ChatPanelCommands.CopyCommand(messageTextBlock)
		};

		MenuItem deleteMenuItem = new () {
			Header = "Delete message",
			Name = "DeleteMenuItem",
			Cursor = new Cursor(StandardCursorType.Arrow),
			Command = new ChatPanelCommands.DeleteMessageCommand(messageTextBlock)
		};
		
		deleteMenuItem.Classes.Add("delete_menu_item");
		
		ContextMenu contextMenu = new () {
			Items = {
				copyMenuItem,
				deleteMenuItem
			}
		};

		_controller.AddListenersToContextMenu(copyMenuItem, messageTextBlock, border, originalBackground, contextMenu);

		border.Child = messageTextBlock;
		
		_messages.Add(border);
		_messagePanel.Children.Add(border);
		
		// Scrolling to end twice is necessary, otherwise it does not always scroll to the end
		if (_distanceScrolledFromBottom == 0) {
			_messageScrollView!.ScrollToEnd();
			_messageScrollView.ScrollToEnd();
		}
	}

	public void DecryptAndAddMessage(Message message) => AddMessage($"{message.Sender.Modulus.ToString(16).Crop(16)} | {_controller.Decrypt(message, false)}");
	
	// TODO: this should probably be moved to the controller
	private void OnSizeChanged(object? sender, SizeChangedEventArgs args) {
		_isResized = true;
		_messageScrollView!.Offset = _messageScrollView.Offset.WithY(_messageScrollView.ScrollBarMaximum.Y - _distanceScrolledFromBottom);
	}
	
	public void Show(RsaKeyParameters publicKey, MainWindow context) {
		// _messagePanel should never be null, because a user cannot open this panel before the UI is done.
		// However, in theory, when the program is loaded from a saved state, it is theoretically possible to trigger this. So this is just for debug.
		if (_messagePanel == null) {
			Console.WriteLine("Show was called before initialization. Retrying with delay");
			Dispatcher.UIThread.InvokeAsync(() => Show(publicKey, context)); // Just call itself after UI has been initialized
			return;
		}
		
		_controller.ForeignPublicKey = publicKey;
		
		model.Message[] messages = _controller.GetPastMessages();

		foreach (model.Message message in messages) {
			string fullMessage = $"{message.UserName.Crop(16)} | {message.Body}";
			AddMessage(fullMessage);
		}

		context.SizeChanged += OnSizeChanged;

		_isShowCalled = true;
	}

	public void Unshow(MainWindow context) {
		if (!_isShowCalled)
			return;
		
		_messagePanel.Children.RemoveAll(_messages);
		_messages.Clear();

		context.SizeChanged -= OnSizeChanged;
	}

	public RsaKeyParameters GetForeignPublicKey() => _controller.ForeignPublicKey;
}
