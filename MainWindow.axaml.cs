using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SecureChat;

public partial class MainWindow : Window {
	public MainWindow() {
		InitializeComponent();
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);

		StackPanel messagePanel = this.FindControl<StackPanel>("MessagePanel")!;
	}
}