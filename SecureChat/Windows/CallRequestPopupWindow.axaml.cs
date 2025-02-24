using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LessAnnoyingHttp;
using Org.BouncyCastle.Crypto.Parameters;
using SecureChat.Audio;
using SecureChat.ClassExtensions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using SecureChat.Util;

namespace SecureChat.Windows;

public partial class CallRequestPopupWindow : PopupWindow {
	private readonly RsaKeyParameters _personalKey, _foreignKey, _privateKey;
	private readonly long _timestamp;

	private bool _isAccepted;
	
	public CallRequestPopupWindow(RsaKeyParameters personalKey, RsaKeyParameters foreignKey, long timestamp) {
		using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
			PemReader pemReader = new (reader);
			_privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
		}
		
		_personalKey = personalKey;
		_foreignKey = foreignKey;
		_timestamp = timestamp;
		
		InitializeComponent();
	}

	public void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
		TextBlock nameTextBlock = this.FindControl<TextBlock>("CallerName")!;
		nameTextBlock.Text = _foreignKey.ToBase64String(); // TODO: change to display name

		Closing += OnClosing;
	}

	public void AcceptButton_OnClick(object? sender, RoutedEventArgs e) {
		_isAccepted = true;
		new CallPopupWindow(_personalKey, _foreignKey, _timestamp).Show(MainWindow.Instance);
		Close();
	}

	private void OnClosing(object? sender, WindowClosingEventArgs e) {
		Sounds.Ringtone.StopRepeat();
		if (!_isAccepted) {
			long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			JsonObject body = new () {
				["personalKey"] = _personalKey.ToBase64String(),
				["foreignKey"] = _foreignKey.ToBase64String(),
				["timestamp"] = timestamp
			};
			
			string bodyString = JsonSerializer.Serialize(body);
			Http.Delete($"http://{Settings.GetInstance().IpAddress}:5000/voiceChat/reject", bodyString, [new Header { Name = "Signature", Value = Cryptography.Sign(_personalKey.ToBase64String() + timestamp, _privateKey)}]);
		}
	}

	public void RejectButton_OnClick(object? sender, RoutedEventArgs e) {
		Close();
	}
}