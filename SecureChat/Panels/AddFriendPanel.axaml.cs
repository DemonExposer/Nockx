using System;
using Avalonia.Controls;
using Avalonia.Input;
using Nockx.Base.ClassExtensions;
using Org.BouncyCastle.Crypto.Parameters;
using SecureChat.Windows;

namespace SecureChat.Panels;

public partial class AddFriendPanel : StackPanel, IContentPanel {
	public delegate void DataCallback(RsaKeyParameters publicKey);

	private readonly TextBox _keyTextBox;

	public AddFriendPanel() {
		InitializeComponent();

		_keyTextBox = this.FindControl<TextBox>("KeyTextBox")!;
	}

	public void SetOnEnter(DataCallback callback) {
		_keyTextBox!.KeyDown += (_, args) => {
			if (args.Key == Key.Enter) {
				RsaKeyParameters rsaKeyParameters;
				try {
					rsaKeyParameters = RsaKeyParametersExtension.FromBase64String(_keyTextBox.Text);
				} catch (Exception) {
					new ErrorPopupWindow("The specified key is invalid").Show(MainWindow.Instance);
					return;
				}

				callback(rsaKeyParameters);
			}
		};
	}

	public void Show(MainWindow context) { }
}