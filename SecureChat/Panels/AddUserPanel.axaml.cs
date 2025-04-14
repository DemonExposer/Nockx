using System;
using Avalonia.Controls;
using Avalonia.Input;
using Org.BouncyCastle.Crypto.Parameters;
using SecureChat.ClassExtensions;
using SecureChat.Windows;

namespace SecureChat.Panels;

public partial class AddUserPanel : StackPanel {
	public delegate void DataCallback(RsaKeyParameters publicKey, string name);

	private readonly TextBox _keyTextBox;

	public AddUserPanel() {
		InitializeComponent();

		_keyTextBox = this.FindControl<TextBox>("KeyTextBox")!;
	}

	public void SetOnEnter(DataCallback callback) {
		// TODO: add check to see whether TextBoxes are actually filled
		_keyTextBox!.KeyDown += (_, args) => {
			if (args.Key == Key.Enter) {
				RsaKeyParameters rsaKeyParameters;
				try {
					rsaKeyParameters = RsaKeyParametersExtension.FromBase64String(_keyTextBox.Text!);
				} catch (Exception) {
					new ErrorPopupWindow("The specified key is invalid").Show(MainWindow.Instance);
					return;
				}

				callback(rsaKeyParameters, _keyTextBox.Text!);
			}
		};
	}
}

