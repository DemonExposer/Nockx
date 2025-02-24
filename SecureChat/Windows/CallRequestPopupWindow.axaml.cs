using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LessAnnoyingHttp;
using Org.BouncyCastle.Crypto.Parameters;
using SecureChat.Audio;
using SecureChat.ClassExtensions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SecureChat.Windows;

public partial class CallRequestPopupWindow : PopupWindow {
	private readonly RsaKeyParameters _personalKey, _foreignKey;
	private readonly long _timestamp;

	private bool _isAccepted;
	
	public CallRequestPopupWindow(RsaKeyParameters personalKey, RsaKeyParameters foreignKey, long timestamp) {
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
			JsonObject body = new () {
				["personalKey"] = _personalKey.ToBase64String(),
				["foreignKey"] = _foreignKey.ToBase64String()
			};
			Http.Delete($"http://{Settings.GetInstance().IpAddress}:5000/voiceChat/reject", JsonSerializer.Serialize(body));
		}
	}

	public void RejectButton_OnClick(object? sender, RoutedEventArgs e) {
		Close();
	}
}