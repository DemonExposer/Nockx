using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Org.BouncyCastle.Crypto.Parameters;
using System.Collections.Generic;
using SecureChat.ClassExtensions;
using SecureChat.Windows;

namespace SecureChat.Panels;

public class AddUserPanel : StackPanel {
	public delegate void DataCallback(RsaKeyParameters publicKey, string name);

	public void SetOnEnter(DataCallback callback) {
		using IEnumerator<ILogical> enumerator = this.GetLogicalDescendants().GetEnumerator();

		TextBox? keyTextBox = null;
		while (enumerator.MoveNext()) {
			if (enumerator.Current.GetType() == typeof(TextBox)) {
				TextBox textBox = (TextBox) enumerator.Current;
				switch (textBox.Name) {
					case "KeyTextBox":
						keyTextBox = (TextBox) enumerator.Current;
						break;
				}
			}
		}

		// TODO: add check to see whether TextBoxes are actually filled
		keyTextBox!.KeyDown += (_, args) => {
			if (args.Key == Key.Enter) {
				RsaKeyParameters rsaKeyParameters;
				try {
					rsaKeyParameters = RsaKeyParametersExtension.FromBase64String(keyTextBox.Text!);
				} catch (Exception) {
					new ErrorPopupWindow("The specified key is invalid").Show(MainWindow.Instance);
					return;
				}

				callback(rsaKeyParameters, keyTextBox.Text!);
			}
		};
	}
}

