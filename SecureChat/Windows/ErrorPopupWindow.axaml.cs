using Avalonia.Controls;

namespace SecureChat.Windows;

public partial class ErrorPopupWindow : PopupWindow {
	public ErrorPopupWindow(string message) {
		InitializeComponent();
		this.FindControl<TextBlock>("DisplayText")!.Text = message;
	}
}