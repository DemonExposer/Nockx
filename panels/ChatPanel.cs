﻿using System;
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
	private ScrollViewer? _messageScrollView;

	private double _distanceScrolledFromBottom;

	private bool _isResized = false;
	private bool _isShowCalled = false;

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

		SelectableTextBlock messageTextBlock = new () {
			Text = text,
			Margin = new Thickness(5)
		};
		_messages.Add(messageTextBlock);
		_messagePanel.Children.Add(messageTextBlock);
		
		// Scrolling to end twice is necessary, otherwise it does not always scroll to the end
		if (_distanceScrolledFromBottom == 0) {
			_messageScrollView!.ScrollToEnd();
			_messageScrollView.ScrollToEnd();
		}
	}

	public void DecryptAndAddMessage(Message message) => AddMessage(_controller.Decrypt(message, false));
	
	// TODO: this should probably be moved to the controller
	private void OnSizeChanged(object? sender, SizeChangedEventArgs args) {
		_isResized = true;
		_messageScrollView!.Offset = _messageScrollView.Offset.WithY(_messageScrollView.ScrollBarMaximum.Y - _distanceScrolledFromBottom);
	}
	
	public void Show(string username, RsaKeyParameters publicKey, MainWindow context) {
		// _messagePanel should never be null, because a user cannot open this panel before the UI is done.
		// However, in theory, when the program is loaded from a saved state, it is theoretically possible to trigger this. So this is just for debug.
		if (_messagePanel == null) {
			Console.WriteLine("Show was called before initialization. Retrying with delay");
			Dispatcher.UIThread.InvokeAsync(() => Show(username, publicKey, context)); // Just call itself after UI has been initialized
			return;
		}
		
		_controller.ForeignPublicKey = publicKey;
					
		string[] messages = _controller.GetPastMessages();

		foreach (string message in messages) {
			string fullMessage = $"{username} | {message}";
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
