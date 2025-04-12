using Avalonia.Controls;
using Avalonia.Input;
using SecureChat.ClassExtensions;
using Avalonia.Interactivity;
using SecureChat.Windows;
using System;

namespace SecureChat.Panels;

public partial class UserInfoPanel : StackPanel {
	public readonly TextBox DisplayNameTextBox;

	private readonly UserInfoPanelController _controller;
	private ConnectAppWindow? _connectAppWindow;

	public UserInfoPanel() {
		_controller = new UserInfoPanelController(this);

		InitializeComponent();

		TextBox keyBox = this.FindControl<TextBox>("KeyBox")!;
		DisplayNameTextBox = this.FindControl<TextBox>("DisplayNameBox")!;
		Button setDisplayNameButton = this.FindControl<Button>("SetDisplayNameButton")!;
		Button connectAppButton = this.FindControl<Button>("ConnectAppButton")!;
		keyBox.PointerReleased += (_, _) => keyBox.SelectAll();

		DisplayNameTextBox.KeyDown += (_, args) => {
			if (args.Key == Key.Enter)
				setDisplayNameButton?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
		};

		setDisplayNameButton.Click += (_, _) => {
			if (DisplayNameTextBox?.Text == null)
				return;

			_controller.SetDisplayName(DisplayNameTextBox.Text);
		};

		connectAppButton.Click += ConnectAppButton_OnClick;

		keyBox!.Text = _controller.PublicKey.ToBase64String();
	}
	
	public void SetMainWindowModel(MainWindowModel mainWindowModel) {
		_controller.SetMainWindowModel(mainWindowModel);
	}

	private void ConnectAppButton_OnClick(object? sender, RoutedEventArgs e) {
		_connectAppWindow = new ConnectAppWindow();
		_connectAppWindow.Closed += (_, _) => _connectAppWindow = null;
		// TODO: also put timestamp with signature in the QR code so that the scanner can be verified as being a real scanner
		_connectAppWindow.SetQrCode(_controller.PublicKey.ToBase64String());
		_connectAppWindow.Show();
	}

	public void ShowQrCodeConfirmationWindow(string key, byte[] onConfirmData) {
		if (_connectAppWindow == null)
			return;

		new DialogWindow(
			$"{key} is trying to retrieve your private key. If this key does not correspond to the one on the device you scanned your QR code with, DO NOT ACCEPT THIS REQUEST, BECAUSE YOU CAN LOSE YOUR ACCOUNT!!!",
			accepted => {
				if (accepted)
					_connectAppWindow?.SetQrCode(Convert.ToBase64String(onConfirmData));
			}
		).Show(_connectAppWindow);
	}
}
