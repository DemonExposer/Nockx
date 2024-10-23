using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using System.Collections.Generic;

namespace SecureChat.panels;

public class UserInfoPanel : StackPanel {
	private UserInfoPanelController _controller = new ();

	public UserInfoPanel() {
		Dispatcher.UIThread.InvokeAsync(() => {
			TextBox? modulusBox = null, exponentBox = null;

			using IEnumerator<ILogical> enumerator = this.GetLogicalChildren().GetEnumerator();
			while (enumerator.MoveNext()) {
				if (enumerator.Current.GetType() == typeof(TextBox)) {
					TextBox textBox = (TextBox) enumerator.Current;
					switch (textBox.Name) {
						case "ModulusBox":
							modulusBox = textBox;
							break;
						case "ExponentBox":
							exponentBox = textBox;
							break;
					}
				}
			}

			modulusBox!.Text = _controller.PublicKey.Modulus.ToString(16);
			exponentBox!.Text = _controller.PublicKey.Exponent.ToString(16);
		});
	}
}
