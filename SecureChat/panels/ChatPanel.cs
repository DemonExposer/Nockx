using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Org.BouncyCastle.Crypto.Parameters;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using SecureChat.ClassExtensions;
using SecureChat.extended_controls;
using SecureChat.model;
using SecureChat.util;

namespace SecureChat.panels;

public class ChatPanel : DockPanel {
	private MainWindowModel? _mainWindowModel;
	
	private readonly ChatPanelController _controller;
	private readonly List<MessageTextBlock> _messages = [];

	private StackPanel? _messagePanel;
	private ScrollViewer? _messageScrollView;
	private TextBlock? _headerUsernameTextBlock;
	private Button? _callButton;

	private double _distanceScrolledFromBottom;

	private bool _isResized;
	private bool _isShowCalled;

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
								if (_mainWindowModel == null)
									throw new InvalidOperationException("Cannot add message before _mainWindowModel is set, using SetMainWindowModel");
								long id = _controller.SendMessage(textBox.Text);
								AddMessage(new DecryptedMessage { Body = textBox.Text, DateTime = DateTime.MinValue, Id = id, Sender = _controller.PersonalPublicKey.ToBase64String(), DisplayName = _mainWindowModel.DisplayName});
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
				} else if (enumerator.Current.GetType() == typeof(TextBlock)) {
					TextBlock textBlock = (TextBlock) enumerator.Current;
					if (textBlock.Name == "HeaderForeignUsername")
						_headerUsernameTextBlock = textBlock;
				} else if (enumerator.Current.GetType() == typeof(Button)) {
					Button button = (Button) enumerator.Current;
					if (button.Name == "CallButton")
						_callButton = button;
				}
			}
		});
	}

	public void SetMainWindowModel(MainWindowModel mainWindowModel) {
		if (_mainWindowModel != null)
			return;
		
		_mainWindowModel = mainWindowModel;
		_controller.SetMainWindowModel(mainWindowModel);
	}
	
	private void AddMessage(DecryptedMessage message) {
		if (_messagePanel == null) {
			Console.WriteLine("Show was called before initialization. Not displaying anything");
			return;
		}

		IBrush originalBackground = new SolidColorBrush(Color.Parse("Transparent"));
		Border border = new () {
			Background = originalBackground
		};

		SelectableTextBlock block = new () {
			Text = $"{message.DisplayName.Crop(16)} | {message.Body}",
			Margin = new Thickness(5),
			TextWrapping = TextWrapping.Wrap
		};
		
		MessageTextBlock messageTextBlock = new () {
			Id = message.Id,
			Border = border,
			TextBlock = block,
			Sender = message.Sender
		};

		MenuItem copyMenuItem = new () {
			Header = "Copy",
			Name = "CopyMenuItem",
			Cursor = new Cursor(StandardCursorType.Arrow),
			Command = new ChatPanelCommands.CopyCommand(messageTextBlock.TextBlock)
		};

		MenuItem deleteMenuItem = new () {
			Header = "Delete message",
			Name = "DeleteMenuItem",
			Cursor = new Cursor(StandardCursorType.Arrow),
			Command = new ChatPanelCommands.DeleteMessageCommand(messageTextBlock, _controller.DeleteMessage)
		};
		
		deleteMenuItem.Classes.Add("delete_menu_item");
		
		ContextMenu contextMenu = new () {
			Items = {
				copyMenuItem,
				deleteMenuItem
			}
		};

		_controller.AddListenersToContextMenu(messageTextBlock, originalBackground, contextMenu);

		border.Child = block;
        
		_messages.Add(messageTextBlock);
		_messagePanel.Children.Add(border);
		
		// Scrolling to end twice is necessary, otherwise it does not always scroll to the end
		if (_distanceScrolledFromBottom == 0) {
			_messageScrollView!.ScrollToEnd();
			_messageScrollView.ScrollToEnd();
		}
	}

	public void DecryptAndAddMessage(Message message) {
		if (!_controller.Decrypt(message, false, out DecryptedMessage? decryptedMessage))
			return;

		AddMessage(decryptedMessage!);
	}

	// TODO: this should probably be moved to the controller
	private void OnSizeChanged(object? sender, SizeChangedEventArgs args) {
		_isResized = true;
		_messageScrollView!.Offset = _messageScrollView.Offset.WithY(_messageScrollView.ScrollBarMaximum.Y - _distanceScrolledFromBottom);
	}

	public void ChangeDisplayName() {
		if (_controller.ForeignDisplayName != null)
			_headerUsernameTextBlock!.Text = _controller.ForeignDisplayName.Crop(30);
	}
	
	public void Show(RsaKeyParameters publicKey, MainWindow context) {
		if (_mainWindowModel == null)
			throw new InvalidOperationException("Show may not be called before _mainWindowModel is set, using SetMainWindowModel");
		
		// _messagePanel should never be null, because a user cannot open this panel before the UI is done.
		// However, in theory, when the program is loaded from a saved state, it is theoretically possible to trigger this. So this is just for debug.
		if (_messagePanel == null) {
			Console.WriteLine("Show was called before initialization. Retrying with delay");
			Dispatcher.UIThread.InvokeAsync(() => Show(publicKey, context)); // Just call itself after UI has been initialized
			return;
		}
		
		_controller.ForeignPublicKey = publicKey;
		ChangeDisplayName();

		// If chat is unread, make it read
		if (!_mainWindowModel.GetChatReadStatus(publicKey.ToBase64String())) {
			_mainWindowModel.SetChatReadStatus(publicKey.ToBase64String(), true);
			_controller.SetChatRead();
		}
		
		DecryptedMessage[] messages = _controller.GetPastMessages();

		messages.ToList().ForEach(AddMessage);

		_callButton.Click += _controller.OnCallButtonClicked;

		context.SizeChanged += OnSizeChanged;

		_isShowCalled = true;
	}

	public void RemoveMessage(long id) {
		// TODO: maybe change to a binary search, since ids are always increasing
		for (int i = 0; i < _messages.Count; i++) {
			if (_messages[i].Id == id) {
				_messages.RemoveAt(i);
				_messagePanel.Children.RemoveAt(i);
			}
		}
	}

	public void Unshow(MainWindow context) {
		if (!_isShowCalled)
			return;
		
		_messagePanel.Children.RemoveAll(_messages.Select(block => block.Border));
		_messages.Clear();

		_callButton.Click -= _controller.OnCallButtonClicked;
		
		context.SizeChanged -= OnSizeChanged;
	}

	public RsaKeyParameters GetForeignPublicKey() => _controller.ForeignPublicKey;
}
