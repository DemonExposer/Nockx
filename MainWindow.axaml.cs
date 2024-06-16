using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Org.BouncyCastle.Crypto.Parameters;
using SecureChat.panels;

namespace SecureChat;

public partial class MainWindow : Window {
	private DockPanel _mainPanel;
	private Panel? _uiPanel;
	private Button? _pressedButton;
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
		
		Button addChatButton = this.FindControl<Button>("AddChatButton")!;
		SetPressedButton(addChatButton);
		addChatButton.Click += (_, _) => {
			SetPressedButton(addChatButton);
			SetUiPanel(addUserPanel);
		};
		
		Button userInfoButton = this.FindControl<Button>("UserInfoButton")!;
		userInfoButton.Click += (_, _) => {
			SetPressedButton(userInfoButton);
			SetUiPanel(userInfoPanel);
		};
	}

	private void SetPressedButton(Button button) {
		_pressedButton?.Classes.Remove("selected");

		_pressedButton = button;
		button.Classes.Add("selected");
	}

	private void SetUiPanel(Panel panel) {
		if (_uiPanel != null)
			_mainPanel.Children.Remove(_uiPanel);

		_uiPanel = panel;
		_mainPanel.Children.Add(panel);
	}

	public RsaKeyParameters? GetCurrentChatIdentity() => _uiPanel is not panels.ChatPanel ? null : ChatPanel.GetForeignPublicKey();

	private void OnAddUser(RsaKeyParameters publicKey) {
		SetUiPanel(ChatPanel);
		ChatPanel.Show("someone", publicKey);

		Button chatButton = new () {
			Background = new SolidColorBrush(Color.Parse("Transparent")),
			Width = 200,
			Content = "chat"
		};
		SetPressedButton(chatButton);
		chatButton.Click += (_, _) => {
			SetPressedButton(chatButton);
			SetUiPanel(ChatPanel);
			ChatPanel.Show("someone", publicKey);
		};

		this.FindControl<StackPanel>("ChatListPanel")!.Children.Add(chatButton);
	}
}