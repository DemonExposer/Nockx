using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LessAnnoyingHttp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using SecureChat.Audio;
using SecureChat.ClassExtensions;
using SecureChat.Util;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SecureChat.Windows;

public partial class CallRequestPopupWindow : PopupWindow {
	private readonly RsaKeyParameters _personalKey, _foreignKey, _privateKey;

	private bool _isAccepted;
	
	public CallRequestPopupWindow(RsaKeyParameters personalKey, RsaKeyParameters foreignKey) {
		using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
			PemReader pemReader = new (reader);
			_privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
		}

		_personalKey = personalKey;
		_foreignKey = foreignKey;
		
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
		new CallPopupWindow(_personalKey, _foreignKey).Show(MainWindow.Instance);
		Close();
	}

	private void OnClosing(object? sender, WindowClosingEventArgs e) {
		Sounds.Ringtone.StopRepeat();
		if (!_isAccepted) {
			JsonObject body = new () {
				["personalKey"] = _personalKey.ToBase64String(),
				["foreignKey"] = _foreignKey.ToBase64String()
			};

			string bodyString = JsonSerializer.Serialize(body);
			Http.Delete($"http://{Settings.GetInstance().IpAddress}:5000/voiceChat/reject", bodyString, [new Header { Name = "Signature", Value = Cryptography.Sign(bodyString, _privateKey) }]);
		}
	}

	public void RejectButton_OnClick(object? sender, RoutedEventArgs e) => Close();
}