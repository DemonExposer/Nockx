using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using System.Collections.Generic;
using Avalonia.Input;
using SecureChat.ClassExtensions;
using Avalonia.Interactivity;
using SecureChat.Windows;

namespace SecureChat.Panels;

public class UserInfoPanel : StackPanel {
	private readonly UserInfoPanelController _controller = new ();
	private ConnectAppWindow? _connectAppWindow;

	public UserInfoPanel() {
		Dispatcher.UIThread.InvokeAsync(() => {
			TextBox? keyBox = null;

			using IEnumerator<ILogical> enumerator = this.GetLogicalChildren().GetEnumerator();
			while (enumerator.MoveNext()) {
				if (enumerator.Current.GetType() == typeof(TextBox)) {
					TextBox textBox = (TextBox) enumerator.Current;
					switch (textBox.Name) {
						case "KeyBox":
							keyBox = textBox;
							break;
						case "DisplayNameBox":
							textBox.Text = _controller.DisplayName;
							textBox.KeyDown += (_, args) => {
								if (args.Key == Key.Enter) {
									if (textBox.Text == null)
										return;
									if(!_controller.SetDisplayName(textBox.Text))
										textBox.Text = _controller.DisplayName;
								}
							};
							break;
					}
				} else if (enumerator.Current.GetType() == typeof(Button)) {
					Button button = (Button) enumerator.Current;
					switch (button.Name) {
						case "ConnectAppButton":
							button.Click += ConnectAppButton_OnClick;
							break;
					}
				}
			}
			keyBox!.Text = _controller.PublicKey.ToBase64String();
		});
	}
	
	public void SetMainWindowModel(MainWindowModel mainWindowModel) {
		_controller.SetMainWindowModel(mainWindowModel);
	}

	private void ConnectAppButton_OnClick(object? sender, RoutedEventArgs e) {
		_connectAppWindow = new ConnectAppWindow();
		_connectAppWindow.Closed += (_, _) => _connectAppWindow = null;
		// TODO: also put timestamp with signature in the QR code so that the scanner can be verified as being a real scanner
		_connectAppWindow.SetQrCode(_controller.PublicKey.ToBase64String());
		_connectAppWindow.Show(MainWindow.Instance);
	}
	
	public void SetQrCode(string qrCodeData) => _connectAppWindow?.SetQrCode(qrCodeData);
}
