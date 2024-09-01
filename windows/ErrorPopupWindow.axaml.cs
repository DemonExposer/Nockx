using Avalonia.Controls;

namespace SecureChat.windows;

public partial class ErrorPopupWindow : PopupWindow {
	public ErrorPopupWindow(string message) {
		this.FindControl<TextBlock>("DisplayText")!.Text = message;
	}
}