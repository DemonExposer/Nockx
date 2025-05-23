using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Org.BouncyCastle.Crypto.Parameters;
using SecureChat.ClassExtensions;
using SecureChat.Model;
using SecureChat.Panels;
using SecureChat.Util;
using SecureChat.Windows;

namespace SecureChat;

public partial class MainWindow : Window {
	private DockPanel _mainPanel;
	private Panel? _uiPanel;
	private Button? _pressedButton;

	public ChatPanel ChatPanel { get; private set; }
	public UserInfoPanel UserInfoPanel { get; private set; }

	public CallPopupWindow? CallPopupWindow;
	
	private bool _isRendered;
	private readonly List<Action> _onRenderedActions = [];

	private readonly MainWindowController _controller;
	public readonly MainWindowModel Model;

	public static MainWindow Instance { get; private set; }
	
	public MainWindow() {
		Model = new MainWindowModel();
		
		Activated += OnRendered;
		
		InitializeComponent();
		
		_controller = new MainWindowController(this, Model);
		_ = _controller.ListenOnWebsocket();

		Instance = this;

		Activated += _controller.OnWindowActivated;
		Deactivated += _controller.OnWindowDeactivated;
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
		ChatPanel = new ChatPanel();
		ChatPanel.SetMainWindowModel(Model);
		UserInfoPanel = new UserInfoPanel();
		UserInfoPanel.SetMainWindowModel(Model);

		_mainPanel = this.FindControl<DockPanel>("MainPanel")!;
		
		AddFriendPanel addFriendPanel = new ();
		addFriendPanel.SetOnEnter(OnAddFriend);

		Button addFriendButton = this.FindControl<Button>("AddFriendButton")!;
		addFriendButton.Click += (_, _) => {
			SetPressedButton(addFriendButton);
			SetUiPanel(addFriendPanel);
		};
		
		FriendsPanel friendsPanel = new ();
		
		Button friendsButton = this.FindControl<Button>("FriendsButton")!;
		friendsButton.Click += (_, _) => {
			SetPressedButton(friendsButton);
			SetUiPanel(friendsPanel);
			friendsPanel.Show(_controller.PublicKey, this);
		};

		Button userInfoButton = this.FindControl<Button>("UserInfoButton")!;
		userInfoButton.Click += (_, _) => {
			SetPressedButton(userInfoButton);
			SetUiPanel(UserInfoPanel);
		};

		Button settingsButton = this.FindControl<Button>("SettingsButton")!;
		settingsButton.Click += (_, _) => ShowPopupWindowOnTop(new SettingsPopupWindow());
	}

	private void SetPressedButton(Button button) {
		_pressedButton?.Classes.Remove("selected");

		_pressedButton = button;
		button.Classes.Add("selected");
	}

	private void SetUiPanel(IContentPanel panel) {
		if (_uiPanel != null) {
			if (_uiPanel is ChatPanel chatPanel)
				chatPanel.Unshow(this);
			if (_uiPanel is FriendsPanel friendsPanel)
				friendsPanel.Unshow();
			_mainPanel.Children.Remove(_uiPanel);
		}

		_uiPanel = (Panel) panel;
		_mainPanel.Children.Add((Panel) panel);
	}

	public RsaKeyParameters? GetCurrentChatIdentity() => _uiPanel is not Panels.ChatPanel ? null : ChatPanel.GetForeignPublicKey();

	public void AddFriendRequest(FriendRequest friendRequest) {
		if (_uiPanel is FriendsPanel friendsPanel)
			friendsPanel.AddFriend(friendRequest, this);
	}
	
	public void AddUser(RsaKeyParameters publicKey, string name, bool doAutoFocus) {
		if (Model.ContainsChat(publicKey.ToBase64String()))
			return;

		Chats.Add(publicKey, name);

		Button chatButton = new ();
		chatButton.Classes.Add("chat_selector");

		Model.AddChat(new Chat(chatButton, name, publicKey.ToBase64String()));
		
		if (doAutoFocus)
			FocusPanel(publicKey, ChatPanel, chatButton);
		
		chatButton.Click += (_, _) => {
			SetPressedButton(chatButton);
			SetUiPanel(ChatPanel);
			ChatPanel.Show(publicKey, this);
		};
		
		this.FindControl<StackPanel>("ChatListPanel")!.Children.Add(chatButton);
	}

	public void FocusPanel(RsaKeyParameters publicKey, IContentPanel panel, Button button) {
		SetUiPanel(panel);
		panel.Show(publicKey, this);
		SetPressedButton(button);
	}
	
	private void OnAddFriend(RsaKeyParameters publicKey) {
		_controller.SendFriendRequest(publicKey);
	}
	
	private void OnRendered(object? sender, EventArgs e) {
		_isRendered = true;
		_onRenderedActions.ForEach(task => task());
		Activated -= OnRendered;
	}

	public void RemoveFriendRequest(string foreignKey) {
		if (_uiPanel is FriendsPanel friendsPanel)
			friendsPanel.RemoveFriend(foreignKey);
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