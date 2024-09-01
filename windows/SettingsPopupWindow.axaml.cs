using System;
using Avalonia.Controls;

namespace SecureChat.windows;

public partial class SettingsPopupWindow : PopupWindow {
	public SettingsPopupWindow() {
		InitializeComponent();
		
		PostInitialization();

		TextBox serverAddressTextBox = this.FindControl<TextBox>("ServerAddressTextBox")!;
		
		Button setServerButton = this.FindControl<Button>("SetServerButton")!;
		setServerButton.Click += (_, _) => {
			Console.WriteLine(serverAddressTextBox.Text);
		};
	}
}