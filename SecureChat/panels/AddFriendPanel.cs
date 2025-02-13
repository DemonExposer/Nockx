using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using SecureChat.windows;

namespace SecureChat.panels;

public class AddFriendPanel : StackPanel {
	public delegate void DataCallback(RsaKeyParameters publicKey);

	public void SetOnEnter(DataCallback callback) {
		using IEnumerator<ILogical> enumerator = this.GetLogicalDescendants().GetEnumerator();

		TextBox? modulusTextBox = null, exponentTextBox = null;
		while (enumerator.MoveNext()) {
			if (enumerator.Current.GetType() == typeof(TextBox)) {
				TextBox textBox = (TextBox) enumerator.Current;
				switch (textBox.Name) {
					case "FriendModulusTextBox":
						modulusTextBox = (TextBox) enumerator.Current;
						break;
					case "FriendExponentTextBox":
						exponentTextBox = (TextBox) enumerator.Current;
						break;
				}
			}
		}

		// TODO: add check to see whether TextBoxes are actually filled
		exponentTextBox!.KeyDown += (_, args) => {
			if (args.Key == Key.Enter) {
				RsaKeyParameters rsaKeyParameters;
				try {
					rsaKeyParameters = new RsaKeyParameters(
						false,
						new BigInteger(modulusTextBox!.Text, 16),
						new BigInteger(exponentTextBox.Text, 16)
					);
				} catch (ArgumentException e) {
					switch (e.ParamName) {
						case "modulus":
							new ErrorPopupWindow("The specified modulus is invalid").Show(MainWindow.Instance);
							break;
						case "exponent":
							new ErrorPopupWindow("The specified exponent is invalid").Show(MainWindow.Instance);
							break;
					}

					return;
				}

				callback(rsaKeyParameters);
			}
		};
	}
}