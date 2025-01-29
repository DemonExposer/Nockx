using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using System.Collections.Generic;
using Avalonia.Input;

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
						case "NicknameBox":
							textBox.Text = _controller.Nickname;
							textBox.KeyDown += (_, args) => {
								if (args.Key == Key.Enter) {
									if (textBox.Text == null)
										return;
									if(!_controller.SetNickname(textBox.Text))
										textBox.Text = _controller.Nickname;
								}
							};
							break;
					}
				}
			}
			modulusBox!.Text = _controller.PublicKey.Modulus.ToString(16);
			exponentBox!.Text = _controller.PublicKey.Exponent.ToString(16);
		});
	}
}
