using Avalonia.Controls;

namespace SecureChat.windows;

public partial class ErrorPopupWindow : PopupWindow {
	public ErrorPopupWindow(string message) {
		InitializeComponent();
		this.FindControl<TextBlock>("DisplayText")!.Text = message;
	}
}