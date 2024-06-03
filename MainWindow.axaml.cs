using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SecureChat.panels;

namespace SecureChat;

public partial class MainWindow : Window {
	private DockPanel _mainPanel;
	private Panel? _uiPanel;

	public MainWindow() => InitializeComponent();

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);

		AddUserPanel addUserPanel = (AddUserPanel) Resources["AddUserPanel"]!;
		ChatPanel chatPanel = (ChatPanel) Resources["ChatPanel"]!;
		UserInfoPanel userInfoPanel = (UserInfoPanel) Resources["UserInfoPanel"]!;

		_mainPanel = this.FindControl<DockPanel>("MainPanel")!;

		addUserPanel.SetOnEnter(publicKey => {
			SetUiPanel(chatPanel);
			chatPanel.Show("someone", publicKey);
		});

		SetUiPanel(addUserPanel);
		this.FindControl<Button>("AddChatButton")!.Click += (_, _) => SetUiPanel(addUserPanel);
		this.FindControl<Button>("UserInfoButton")!.Click += (_, _) => SetUiPanel(userInfoPanel);
	}

	private void SetUiPanel(Panel panel) {
		if (_uiPanel != null)
			_mainPanel.Children.Remove(_uiPanel);

		_uiPanel = panel;
		_mainPanel.Children.Add(panel);
	}
}