using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SecureChat.panels;

namespace SecureChat;

public partial class MainWindow : Window {
	private DockPanel mainPanel;

	public MainWindow() => InitializeComponent();

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);

		AddUserPanel addUserPanel = (AddUserPanel) Resources["AddUserPanel"]!;
		ChatPanel chatPanel = (ChatPanel) Resources["ChatPanel"]!;

		mainPanel = this.FindControl<DockPanel>("MainPanel")!;

		addUserPanel.SetOnEnter(publicKey => {
			mainPanel.Children.Remove(addUserPanel);
			mainPanel.Children.Add(chatPanel);

			chatPanel.Show("someone", publicKey);
		});

		mainPanel.Children.Add(addUserPanel);
	}
}