using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using SecureChat.util;
using System.Collections.Generic;
using System.IO;

namespace SecureChat.panels;

public class AddUserPanel : StackPanel {
	public delegate void DataCallback(RsaKeyParameters publicKey);

	public void SetOnEnter(DataCallback callback) {
		IEnumerator<ILogical> enumerator = this.GetLogicalDescendants().GetEnumerator();

		TextBox? modulusTextBox = null, exponentTextBox = null;
		while (enumerator.MoveNext()) {
			if (enumerator.Current.GetType() == typeof(TextBox)) {
				TextBox textBox = (TextBox) enumerator.Current;
				switch (textBox.Name) {
					case "ModulusTextBox":
						modulusTextBox = (TextBox) enumerator.Current;
						break;
					case "ExponentTextBox":
						exponentTextBox = (TextBox) enumerator.Current;
						break;
				}
			}
		}

		exponentTextBox!.KeyDown += (_, args) => {
			if (args.Key == Key.Enter) {
				callback(new RsaKeyParameters(false, new Org.BouncyCastle.Math.BigInteger(modulusTextBox!.Text, 16), new Org.BouncyCastle.Math.BigInteger(exponentTextBox.Text, 16)));
			}
		};
	}
}

