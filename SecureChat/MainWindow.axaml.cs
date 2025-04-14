using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using OpenTK.Mathematics;
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
		UserInfoPanel = new UserInfoPanel();
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
			
			Dispatcher.UIThread.InvokeAsync(async () => {
				try {
					List<Vector4> pixels = UserInfoPanel.GetPixelMap();
					bool hasDone = false;
				//double t = 0;
				//Timer timer = new (_ => {
				//	Dispatcher.UIThread.InvokeAsync(() => {
				//		Matrix2 m = new ((float) Math.Cos(t), (float) Math.Sin(t), (float) -Math.Sin(t), (float) Math.Cos(t));
				//		t -= Math.PI / 15;
				//		t %= Math.Tau;
				//		UserInfoPanel.Clear();
				//		for (int i = 0; i < pixels.Count; i++) {
				//			Vector4 v = new Vector4(pixels[i].X - 64, -pixels[i].Y + 64, pixels[i].Z, pixels[i].W);
				//			pixels[i] = v;
				//		}

				//		foreach (Vector4 v in pixels) {
				//			Vector2 v2 = m * new Vector2(v.X, v.Y);
				//			UserInfoPanel.DrawPixel((int) v2.X + 64, (int) v2.Y + 64, (byte) v.Z, (byte) v.Z, (byte) v.Z, (byte) v.W);
				//		}

				//		for (int i = 0; i < pixels.Count; i++) {
				//			Vector4 v = new Vector4(pixels[i].X + 64, -pixels[i].Y + 64, pixels[i].Z, pixels[i].W);
				//			pixels[i] = v;
				//		}
				//	});
				//}, null, 0, 1000/60);

					int imageIndex = 0;
					for (double t = 0; t > -Math.Tau; t -= Math.PI / 15) {
						Matrix2 m = new((float) Math.Cos(t), (float) Math.Sin(t), (float) -Math.Sin(t), (float) Math.Cos(t));
						UserInfoPanel.Clear();
						for (int i = 0; i < pixels.Count; i++) {
							Vector4 v = new Vector4(pixels[i].X - 64, -pixels[i].Y + 64, pixels[i].Z, pixels[i].W);
							pixels[i] = v;
						}

						foreach (Vector4 v in pixels) {
							Vector2 v2 = m * new Vector2(v.X, v.Y);
							float xOffset = v2.X % 1;
							float yOffset = v2.Y % 1;

							int xFactor = v2.X < 0 ? -1 : 1;
							int yFactor = v2.Y < 0 ? -1 : 1;

							xOffset *= xFactor;
							yOffset *= yFactor;

							int newX = xOffset > 0.75 ? (int) v2.X + xFactor : (int) v2.X;
							int newY = yOffset > 0.75 ? (int) v2.Y + yFactor : (int) v2.Y;

							if (xOffset > 0.25 && xOffset < 0.75) {
								if (yOffset > 0.25 && yOffset < 0.75) {
									UserInfoPanel.DrawPixel(newX + 64, newY + 64, (byte) v.Z, (byte) v.Z, (byte) v.Z, (byte) v.W);
									UserInfoPanel.DrawPixel(newX + 64 + xFactor, newY + 64, (byte) v.Z, (byte) v.Z, (byte) v.Z, (byte) v.W);
									UserInfoPanel.DrawPixel(newX + 64, newY + 64 + yFactor, (byte) v.Z, (byte) v.Z, (byte) v.Z, (byte) v.W);
									UserInfoPanel.DrawPixel(newX + 64 + xFactor, newY + 64 + yFactor, (byte) v.Z, (byte) v.Z, (byte) v.Z, (byte) v.W);
								} else {
									UserInfoPanel.DrawPixel(newX + 64, newY + 64, (byte) v.Z, (byte) v.Z, (byte) v.Z, (byte) v.W);
									UserInfoPanel.DrawPixel(newX + 64 + xFactor, newY + 64, (byte) v.Z, (byte) v.Z, (byte) v.Z, (byte) v.W);
								}
							} else {
								if (yOffset > 0.25 && yOffset < 0.75) {
									UserInfoPanel.DrawPixel(newX + 64, newY + 64, (byte) v.Z, (byte) v.Z, (byte) v.Z, (byte) v.W);
									UserInfoPanel.DrawPixel(newX + 64, newY + 64 + yFactor, (byte) v.Z, (byte) v.Z, (byte) v.Z, (byte) v.W);
								} else {
									UserInfoPanel.DrawPixel(newX + 64, newY + 64, (byte) v.Z, (byte) v.Z, (byte) v.Z, (byte) v.W);
								}
							}
						}

						UserInfoPanel.Save($"output/{imageIndex++}");

						for (int i = 0; i < pixels.Count; i++) {
							Vector4 v = new Vector4(pixels[i].X + 64, -pixels[i].Y + 64, pixels[i].Z, pixels[i].W);
							pixels[i] = v;
						}
					}

					await new TaskCompletionSource<bool>().Task;
				} catch (Exception e) {
					Debug.WriteLine(e);
				}
			});
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