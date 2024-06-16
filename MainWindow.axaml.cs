using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Org.BouncyCastle.Crypto.Parameters;
using SecureChat.panels;
using SecureChat.util;
using SecureChat.windows;

namespace SecureChat;

public partial class MainWindow : Window {
	private DockPanel _mainPanel;
	private Panel? _uiPanel;
	private Button? _pressedButton;
	public ChatPanel ChatPanel { get; private set; }

	private readonly MainWindowController _controller;

	public MainWindow() {
		InitializeComponent();
		
		_controller = new MainWindowController(this);
		_ = _controller.ListenOnWebsocket();
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

	public void AddUser(RsaKeyParameters publicKey, string name) {
		SetUiPanel(ChatPanel);
		ChatPanel.Show(name, publicKey);

		Button chatButton = new () {
			Background = new SolidColorBrush(Color.Parse("Transparent")),
			Width = 200,
			Content = name
		};
		SetPressedButton(chatButton);
		chatButton.Click += (_, _) => {
			SetPressedButton(chatButton);
			SetUiPanel(ChatPanel);
			ChatPanel.Show("someone", publicKey);
		};
		
		this.FindControl<StackPanel>("ChatListPanel")!.Children.Add(chatButton);
	}
	
	private void OnAddUser(RsaKeyParameters publicKey, string name) {
		AddUser(publicKey, name);
		_controller.AddChatToFile(publicKey, name);
	}

	public void ShowPopupWindowOnTop(PopupWindow popupWindow) {
		popupWindow.Show();
		EventHandler activatedEventHandler = (_, _) => popupWindow.Topmost = true;
		EventHandler deactivatedEventHandler = (_, _) => popupWindow.Topmost = false;
		Activated += activatedEventHandler;
		Deactivated += deactivatedEventHandler;

		popupWindow.Closed += (_, _) => {
			Activated -= activatedEventHandler;
			Deactivated -= deactivatedEventHandler;
		};
	}
}