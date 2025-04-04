using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Org.BouncyCastle.Crypto.Parameters;
using SecureChat.ClassExtensions;
using SecureChat.Model;
using SecureChat.Panels;
using SecureChat.Windows;

namespace SecureChat;

public partial class MainWindow : Window {
	private DockPanel _mainPanel;
	private Panel? _uiPanel;
	private Button? _pressedButton;

	public ChatPanel ChatPanel { get; private set; }
	public UserInfoPanel UserInfoPanel { get; private set; }
	
	private bool _isRendered;
	private readonly List<Action> _onRenderedActions = [];

	private readonly MainWindowController _controller;
	private readonly MainWindowModel _model;

	public static MainWindow Instance { get; private set; }
	
	public MainWindow() {
		_model = new MainWindowModel();
		
		InitializeComponent();
		
		_controller = new MainWindowController(this, _model);
		_ = _controller.ListenOnWebsocket();

		Instance = this;

		Activated += _controller.OnWindowActivated;
		Deactivated += _controller.OnWindowDeactivated;
		PositionChanged += OnRendered;
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);

		AddUserPanel addUserPanel = (AddUserPanel) Resources["AddUserPanel"]!;
		ChatPanel = (ChatPanel) Resources["ChatPanel"]!;
		ChatPanel.SetMainWindowModel(_model);
		UserInfoPanel = (UserInfoPanel) Resources["UserInfoPanel"]!;
		UserInfoPanel.SetMainWindowModel(_model);

		_mainPanel = this.FindControl<DockPanel>("MainPanel")!;

		addUserPanel.SetOnEnter(OnAddUser);

		SetUiPanel(addUserPanel);
		
		Button addChatButton = this.FindControl<Button>("AddChatButton")!;
		SetPressedButton(addChatButton);
		addChatButton.Click += (_, _) => {
			SetPressedButton(addChatButton);
			SetUiPanel(addUserPanel);
		};
		
		AddFriendPanel addFriendPanel = (AddFriendPanel) Resources["AddFriendPanel"]!;
		addFriendPanel.SetOnEnter(OnAddFriend);

		Button addFriendButton = this.FindControl<Button>("AddFriendButton")!;
		addFriendButton.Click += (_, _) => {
			SetPressedButton(addFriendButton);
			SetUiPanel(addFriendPanel);
		};
		
		FriendsPanel friendsPanel = (FriendsPanel) Resources["FriendsPanel"]!;
		
		Button friendsButton = this.FindControl<Button>("FriendsButton")!;
		friendsButton.Click += (_, _) => {
			SetPressedButton(friendsButton);
			SetUiPanel(friendsPanel);
			friendsPanel.Show(this);
		};

		Button userInfoButton = this.FindControl<Button>("UserInfoButton")!;
		userInfoButton.Click += (_, _) => {
			SetPressedButton(userInfoButton);
			SetUiPanel(UserInfoPanel);
		};

		Button settingsButton = this.FindControl<Button>("SettingsButton")!;
		settingsButton.Click += (_, _) => {
			ShowPopupWindowOnTop(new SettingsPopupWindow());
		};
	}

	private void SetPressedButton(Button button) {
		_pressedButton?.Classes.Remove("selected");

		_pressedButton = button;
		button.Classes.Add("selected");
	}

	private void SetUiPanel(Panel panel) {
		if (_uiPanel != null) {
			if (_uiPanel is ChatPanel chatPanel)
				chatPanel.Unshow(this);
			if (_uiPanel is FriendsPanel friendsPanel)
				friendsPanel.Unshow();
			_mainPanel.Children.Remove(_uiPanel);
		}

		_uiPanel = panel;
		_mainPanel.Children.Add(panel);
	}

	public RsaKeyParameters? GetCurrentChatIdentity() => _uiPanel is not Panels.ChatPanel ? null : ChatPanel.GetForeignPublicKey();

	public void AddUser(RsaKeyParameters publicKey, string name, bool doAutoFocus) {
		if (_model.ContainsChat(publicKey.ToBase64String()))
			return;

		Chats.Add(publicKey, name);

		Button chatButton = new ();
		chatButton.Classes.Add("chat_selector");

		_model.AddChat(new Chat(chatButton, name, publicKey.ToBase64String()));
		
		if (doAutoFocus) {
			SetUiPanel(ChatPanel);
			ChatPanel.Show(publicKey, this);
			SetPressedButton(chatButton);
		}
		
		chatButton.Click += (_, _) => {
			SetPressedButton(chatButton);
			SetUiPanel(ChatPanel);
			ChatPanel.Show(publicKey, this);
		};
		
		this.FindControl<StackPanel>("ChatListPanel")!.Children.Add(chatButton);
	}
	
	private void OnAddUser(RsaKeyParameters publicKey, string name) {
		AddUser(publicKey, name, true);
	}

	private void OnAddFriend(RsaKeyParameters publicKey) {
		_controller.SendFriendRequest(publicKey);
	}
	
	private void OnRendered(object? sender, PixelPointEventArgs e) {
		_isRendered = true;
		_onRenderedActions.ForEach(task => task());
		PositionChanged -= OnRendered;
	}

	public void SetCallWindow(CallPopupWindow? window) => _controller.CallWindow = window;

	public void ShowPopupWindowOnTop(PopupWindow popupWindow) {
		if (_isRendered)
			popupWindow.Show(this);
		else
			_onRenderedActions.Add(() => popupWindow.Show(this));
		
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