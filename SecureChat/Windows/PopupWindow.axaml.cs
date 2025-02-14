using System;
using Avalonia;
using Avalonia.Controls;

namespace SecureChat.Windows;

public partial class PopupWindow : Window {
	protected PopupWindow() {
		InitializeComponent();
		Opened += OnOpened;
	}
	
	private void OnOpened(object? sender, EventArgs e) {
		// This because WindowStartupLocation does not work on Linux
		PixelRect screenBounds = Owner != null ? Screens.ScreenFromWindow(Owner)!.Bounds : Screens.Primary!.Bounds;
		int x = (int) (screenBounds.TopLeft.X + (screenBounds.Width - Width) / 2);
		int y = (int) (screenBounds.TopLeft.Y + (screenBounds.Height - Height) / 2);
		Position = new PixelPoint(x, y);
	}
}