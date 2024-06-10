using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Org.BouncyCastle.Crypto.Parameters;
using SecureChat.panels;
using System.Threading;

namespace SecureChat;

public partial class MainWindow : Window {
	private DockPanel _mainPanel;
	private Panel? _uiPanel;
	public ChatPanel ChatPanel { get; private set; }

	private readonly MainWindowController _controller;

	public MainWindow() {
		_controller = new MainWindowController(this);
		_ = _controller.ListenOnWebsocket();

		InitializeComponent();
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);

		AddUserPanel addUserPanel = (AddUserPanel) Resources["AddUserPanel"]!;
		ChatPanel = (ChatPanel) Resources["ChatPanel"]!;
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

	public RsaKeyParameters? GetCurrentChatIdentity() {
		if (_uiPanel is not panels.ChatPanel)
			return null;

		return ChatPanel.GetForeignPublicKey();
	}

	private void OnAddUser(RsaKeyParameters publicKey) {
		SetUiPanel(ChatPanel);
		ChatPanel.Show("someone", publicKey);
		Button chatButton = new () {
			Background = new SolidColorBrush(Color.Parse("Transparent")),
			Width = 200,
			Content = "chat"
		};
		chatButton.Click += (_, _) => {
			SetUiPanel(ChatPanel);
			ChatPanel.Show("someone", publicKey);
		};
		this.FindControl<StackPanel>("ChatListPanel")!.Children.Add(chatButton);
	}
}