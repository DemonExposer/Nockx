using System;
using Avalonia.Controls;
using Avalonia.Input;
using Org.BouncyCastle.Crypto.Parameters;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using SecureChat.ClassExtensions;
using SecureChat.CustomControls;
using System.Threading.Tasks;
using Spinner = SecureChat.CustomControls.Spinner;
using System.Text;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Nockx.Base;
using Nockx.Base.ClassExtensions;
using Nockx.Base.Util;

namespace SecureChat.Panels;

public partial class ChatPanel : DockPanel, IContentPanel {
	private MainWindowModel? _mainWindowModel;
	
	private readonly ChatPanelController _controller;
	private readonly List<MessageItem> _messages = [];

	private StackPanel? _messagePanel;
	private ScrollViewer? _messageScrollView;
	private TextBlock? _headerUsernameTextBlock;
	private Button? _callButton;

	private double _distanceScrolledFromBottom;

	private bool _isResized;
	private bool _isShowCalled;

	public ChatPanel() {
		_controller = new ChatPanelController(this);

		InitializeComponent();

		TextBox messageBox = this.FindControl<TextBox>("MessageBox")!;
		messageBox.KeyDown += (_, args) => {
			switch (args.Key) {
				case Key.Enter:
					if (messageBox.Text == null)
						return;
					if (_mainWindowModel == null)
						throw new InvalidOperationException("Cannot add message before _mainWindowModel is set using SetMainWindowModel");

					long id = _controller.SendMessage(messageBox.Text);
					AddMessage(new DecryptedMessage { Body = messageBox.Text, Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Id = id, Sender = _controller.PersonalPublicKey.ToBase64String(), DisplayName = _mainWindowModel.DisplayName });
					messageBox.Text = null;
					break;
				case Key.CapsLock:
					if (messageBox.SelectionStart > messageBox.SelectionEnd)
						(messageBox.SelectionEnd, messageBox.SelectionStart) = (messageBox.SelectionStart, messageBox.SelectionEnd);
					
					StringBuilder sb = new (messageBox.Text[..messageBox.SelectionStart]); // TODO: this crashes on no selection, fix it
					for (int i = messageBox.SelectionStart; i < messageBox.SelectionEnd; i++) {
						if (messageBox.Text[i] <= 'z' && messageBox.Text[i] >= 'a')
							sb.Append((char) (messageBox.Text[i] + ('A' - 'a')));
						else if (messageBox.Text[i] <= 'Z' && messageBox.Text[i] >= 'A')
							sb.Append((char) (messageBox.Text[i] - ('A' - 'a')));
						else
							sb.Append(messageBox.Text[i]);
					}

					sb.Append(messageBox.Text[messageBox.SelectionEnd..]);

					messageBox.Text = sb.ToString();
					break;
			}
		};

		_messagePanel = this.FindControl<StackPanel>("MessagePanel")!;

		_messageScrollView = this.FindControl<ScrollViewer>("MessageScrollView")!;
		_distanceScrolledFromBottom = _messageScrollView.ScrollBarMaximum.Y - _messageScrollView.Offset.Y;
		_messageScrollView.ScrollChanged += (sender, args) => {
			if (!_isResized)
				_distanceScrolledFromBottom = _messageScrollView.ScrollBarMaximum.Y - _messageScrollView.Offset.Y;
			else
				_isResized = false;
		};

		_headerUsernameTextBlock = this.FindControl<TextBlock>("HeaderForeignUsername")!;

		_callButton = this.FindControl<Button>("CallButton")!;
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

		MessageItem messageItem = new (_controller.DeleteMessage) {
			Id = message.Id,
			IsPersonal = message.Sender == _controller.PersonalPublicKey.ToBase64String(),
			SenderName = message.DisplayName.Crop(32),
			Message = message.Body,
			Timestamp = message.Timestamp,
			PublicKey = message.Sender
		};

		if (message.Sender == _controller.ForeignPublicKey.ToBase64String() && _controller.ForeignDisplayName != message.DisplayName) {
			_controller.ForeignDisplayName = message.DisplayName;
			_headerUsernameTextBlock!.Text = _controller.ForeignDisplayName.Crop(30);
		}

		_messages.Add(messageItem);
		_messagePanel.Children.Add(messageItem);
		
		// Scrolling to end twice is necessary, otherwise it does not always scroll to the end
		if (_distanceScrolledFromBottom == 0) {
			_messageScrollView!.ScrollToEnd();
			_messageScrollView.ScrollToEnd();
		}
	}

	private void AddAttachment(object? sender, RoutedEventArgs e) {
		Dispatcher.UIThread.Invoke(async () => {
			try {
				IReadOnlyList<IStorageFile> list = await MainWindow.Instance.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
					AllowMultiple = true
				});

				string fileName = list[0].Name;
				await using (Stream stream = await list[0].OpenReadAsync())
					_controller.AddAttachment(stream, fileName);
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		});
	}

	public void DecryptAndAddMessage(Message message) {
		if (!_controller.Decrypt(message, false, out DecryptedMessage? decryptedMessage))
			return;

		AddMessage(decryptedMessage!);
	}

	private void OnSizeChanged(object? sender, SizeChangedEventArgs args) {
		_isResized = true;
		_messageScrollView!.Offset = _messageScrollView.Offset.WithY(_messageScrollView.ScrollBarMaximum.Y - _distanceScrolledFromBottom);
	}
	
	public void Show(RsaKeyParameters publicKey, MainWindow context) {
		if (_mainWindowModel == null)
			throw new InvalidOperationException("Show may not be called before _mainWindowModel is set using SetMainWindowModel");
		
		// _messagePanel should never be null, because a user cannot open this panel before the UI is done.
		// However, in theory, when the program is loaded from a saved state, it is theoretically possible to trigger this. So this is just for debug.
		if (_messagePanel == null) {
			Console.WriteLine("Show was called before initialization. Retrying with delay");
			Dispatcher.UIThread.InvokeAsync(() => Show(publicKey, context)); // Just call itself after UI has been initialized
			return;
		}
		
		_controller.ForeignPublicKey = publicKey;
		_headerUsernameTextBlock.Text = _controller.ForeignDisplayName.Crop(30);

		// If chat is unread, make it read
		if (!_mainWindowModel.GetChatReadStatus(publicKey.ToBase64String())) {
			_mainWindowModel.SetChatReadStatus(publicKey.ToBase64String(), true);
			_controller.SetChatRead();
		}
		
		Spinner spinner = new () {
			Height = 128,
			Width = 128
		};
		_messagePanel.Children.Add(spinner);

		// Retrieve and decypt messages in the background
		Task.Run(() => {
			spinner.Spin();
			DecryptedMessage[] messages = _controller.GetPastMessages();
			spinner.StopSpinning();
			Dispatcher.UIThread.InvokeAsync(() => {
				_messagePanel.Children.Remove(spinner);
				messages.ToList().ForEach(AddMessage);
			});
		});

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

	public DecryptedMessage[] RetrieveUnretrievedMessages() {
		DecryptedMessage[] messages = _controller.GetPastMessages(_messages[^1].Id);
		Dispatcher.UIThread.InvokeAsync(() => messages.ToList().ForEach(AddMessage));
		
		return messages;
	}

	public void Unshow(MainWindow context) {
		if (!_isShowCalled)
			return;
		
		_messagePanel.Children.RemoveAll(_messages);
		_messages.Clear();

		_callButton.Click -= _controller.OnCallButtonClicked;
		
		context.SizeChanged -= OnSizeChanged;
	}

	public RsaKeyParameters GetForeignPublicKey() => _controller.ForeignPublicKey;
}
