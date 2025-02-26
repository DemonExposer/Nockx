using System;
using System.Net;
using System.Net.Sockets;
using Avalonia.Controls;

namespace SecureChat.Windows;

public partial class SettingsPopupWindow : PopupWindow {
	public SettingsPopupWindow() {
		InitializeComponent();

		Settings settings = Settings.GetInstance();
		
		TextBox serverAddressTextBox = this.FindControl<TextBox>("ServerAddressTextBox")!;
		serverAddressTextBox.Text = settings.Hostname;
		
		Button setServerButton = this.FindControl<Button>("SetServerButton")!;
		setServerButton.Click += (_, _) => {
			// TODO: maybe automatically check if server is online
			IPAddress ipAddress;
			try {
				ipAddress = IPAddress.Parse(serverAddressTextBox.Text);
			} catch (Exception) {
				try {
					IPHostEntry hostEntry = Dns.GetHostEntry(serverAddressTextBox.Text);
					ipAddress = hostEntry.AddressList[0];
					
				} catch (SocketException) {
					new ErrorPopupWindow("Domain name not found").Show(this);
					return;
				} catch (Exception) {
					new ErrorPopupWindow("IP-address is not correct").Show(this);
					return;
				}
			}
			
			settings.IpAddress = ipAddress;
			settings.Hostname = serverAddressTextBox.Text;
			Settings.Save();
		};
	}
}