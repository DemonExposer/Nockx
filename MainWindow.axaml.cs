using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Org.BouncyCastle.Crypto.Parameters;
using SecureChat.panels;

namespace SecureChat;

public partial class MainWindow : Window {
	private DockPanel _mainPanel;
	private Panel? _uiPanel;
	private ChatPanel _chatPanel;

	public MainWindow() => InitializeComponent();

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);

		AddUserPanel addUserPanel = (AddUserPanel) Resources["AddUserPanel"]!;
		_chatPanel = (ChatPanel) Resources["ChatPanel"]!;
		UserInfoPanel userInfoPanel = (UserInfoPanel) Resources["UserInfoPanel"]!;

		_mainPanel = this.FindControl<DockPanel>("MainPanel")!;

		addUserPanel.SetOnEnter(OnAddUser);

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
	
	public void OnAddUser(RsaKeyParameters publicKey) {
		SetUiPanel(_chatPanel);
	//	_chatPanel.Show("someone", publicKey);
		this.FindControl<StackPanel>("ChatListPanel")!.Children.Add(new Button {
			Background = new SolidColorBrush(Color.Parse("Transparent")),
			Width = 200,
			Content = "chat"
		});
	}
}