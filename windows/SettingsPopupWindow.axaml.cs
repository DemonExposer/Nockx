using System;
using Avalonia.Controls;

namespace SecureChat.windows;

public partial class SettingsPopupWindow : PopupWindow {
	public SettingsPopupWindow() {
		InitializeComponent();
		
		PostInitialization();

		Settings settings = Settings.GetInstance();
		
		TextBox serverAddressTextBox = this.FindControl<TextBox>("ServerAddressTextBox")!;
		serverAddressTextBox.Text = settings.IpAddress.ToString();
		
		Button setServerButton = this.FindControl<Button>("SetServerButton")!;
		setServerButton.Click += (_, _) => {
			Console.WriteLine(serverAddressTextBox.Text);
		};
	}
}