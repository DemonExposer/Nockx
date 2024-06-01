using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SecureChat.panels;

namespace SecureChat;

public partial class MainWindow : Window {
	public MainWindow() => InitializeComponent();

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);

		ChatPanel chatPanel = this.FindControl<ChatPanel>("ChatPanel")!;
		chatPanel.Show();
	}
}