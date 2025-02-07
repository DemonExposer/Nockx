using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Org.BouncyCastle.Crypto.Parameters;
using SecureChat.audio;

namespace SecureChat.windows;

public partial class CallRequestPopupWindow : PopupWindow {
	private readonly RsaKeyParameters _personalKey, _foreignKey;
	
	public CallRequestPopupWindow(RsaKeyParameters personalKey, RsaKeyParameters foreignKey) {
		_personalKey = personalKey;
		_foreignKey = foreignKey;
		
		InitializeComponent();
	}

	public void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
		TextBlock nameTextBlock = this.FindControl<TextBlock>("CallerName")!;
		nameTextBlock.Text = _foreignKey.Modulus.ToString(16); // TODO: change to display name
	}

	public void AcceptButton_OnClick(object? sender, RoutedEventArgs e) {
		Sounds.Ringtone.StopRepeat();
		new CallPopupWindow(_personalKey, _foreignKey).Show(MainWindow.Instance);
		Close();
	}

	public void RejectButton_OnClick(object? sender, RoutedEventArgs e) {
		Sounds.Ringtone.StopRepeat();
		Close();
	}
}