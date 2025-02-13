using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using System.Collections.Generic;
using Avalonia.Input;
using SecureChat.ClassExtensions;

namespace SecureChat.panels;

public class UserInfoPanel : StackPanel {
	private UserInfoPanelController _controller = new ();

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
				}
			}
			keyBox!.Text = _controller.PublicKey.ToBase64String();
		});
	}
	
	public void SetMainWindowModel(MainWindowModel mainWindowModel) {
		_controller.SetMainWindowModel(mainWindowModel);
	}
}
