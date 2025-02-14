using System.Net;
using Avalonia.Controls;

namespace SecureChat.Windows;

public partial class SettingsPopupWindow : PopupWindow {
	public SettingsPopupWindow() {
		InitializeComponent();

		Settings settings = Settings.GetInstance();
		
		TextBox serverAddressTextBox = this.FindControl<TextBox>("ServerAddressTextBox")!;
		serverAddressTextBox.Text = settings.IpAddress.ToString();
		
		Button setServerButton = this.FindControl<Button>("SetServerButton")!;
		setServerButton.Click += (_, _) => {
			// TODO: check for errors, resolve FQDNs, maybe automatically check if server is online
			settings.IpAddress = IPAddress.Parse(serverAddressTextBox.Text);
			Settings.Save();
		};
	}
}