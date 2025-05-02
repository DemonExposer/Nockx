using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SecureChat.ClassExtensions;
using SecureChat.ExtendedControls;
using SecureChat.Util;

namespace SecureChat.Panels;

public partial class FriendsPanel : DockPanel {
	private readonly FriendsPanelController _controller;
	private readonly List<FriendDockPanel> _friends = [];
	private StackPanel _mainPanel;
	private bool _isShowCalled;

	public FriendsPanel() {
		_controller = new FriendsPanelController();

		InitializeComponent();

		_mainPanel = this.FindControl<StackPanel>("MainPanel")!;
	}

	public void Show(MainWindow window) {
		List<FriendRequest> friends = _controller.GetFriends();
		foreach (FriendRequest friend in friends)
			AddFriend(friend, window);
		
		_isShowCalled = true;
	}

	public void Unshow() {
		if (!_isShowCalled)
			return;

		_mainPanel.Children.RemoveAll(_friends.Select(block => block.Border));
		_friends.Clear();
	}

	public void AddFriend(FriendRequest friendRequest, MainWindow window) {
		IBrush originalBackground = new SolidColorBrush(Color.Parse("Transparent"));
		Border border = new () {
			Background = originalBackground
		};
		border.PointerEntered += (_, _) => border.Background = new SolidColorBrush(Color.Parse("#252525"));
		border.PointerExited += (_, _) => border.Background = originalBackground;

		Button button = new () {
			Margin = new Thickness(5)
		};
		SetDock(button, Dock.Right);
		bool amISender = friendRequest.SenderKey == _controller.PersonalPublicKey.ToBase64String();
		string foreignKey = amISender ? friendRequest.ReceiverKey : friendRequest.SenderKey;

		SelectableTextBlock textBlock = new () {
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(5),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Width = double.NaN,
			Text = amISender ? friendRequest.ReceiverName : friendRequest.SenderName
		};
		Button rejectButton = new () {
			Margin = new Thickness(5),
			Content = "Reject",
			Classes = { "reject" }
		};
		SetDock(rejectButton, Dock.Right);
		// TODO: check whether request succeeded and based on that, update the page. Do not refresh it.
		rejectButton.Click += (_, _) => { _controller.DeleteFriendRequest(_controller.PersonalPublicKey.ToBase64String(), foreignKey); Unshow(); Show(window); };
		if (friendRequest.Accepted) {
			button.Content = "Remove";
			button.Classes.Add("reject");
			button.Click += (_, _) => { _controller.DeleteFriendRequest(_controller.PersonalPublicKey.ToBase64String(), foreignKey); Unshow(); Show(window); };
		} else {
			if (amISender) {
				button.Content = "Cancel";
				button.Classes.Add("cancel");
				button.Click += (_, _) => { _controller.DeleteFriendRequest(_controller.PersonalPublicKey.ToBase64String(), foreignKey); Unshow(); Show(window); };
			} else {
				button.Content = "Accept";
				button.Classes.Add("accept");
				button.Click += (_, _) => { _controller.AcceptFriendRequest(friendRequest); Unshow(); Show(window); };
			}
		}

		Button chatButton = new () {
			Margin = new Thickness(5),
			Content = "Chat"
		};
		SetDock(chatButton, Dock.Left);
		string key = friendRequest.SenderKey;
		string name = friendRequest.SenderName;
		if (amISender) {
			key = friendRequest.ReceiverKey;
			name = friendRequest.ReceiverName;
		}
		chatButton.Click += (_, _) => window.AddUser(RsaKeyParametersExtension.FromBase64String(key), name, true);

		DockPanel dockPanel = new ();
		if (!friendRequest.Accepted && !amISender)
			dockPanel.Children.Add(rejectButton);
		dockPanel.Children.Add(button);
		if (friendRequest.Accepted)
			dockPanel.Children.Add(chatButton);
		dockPanel.Children.Add(textBlock);

		FriendDockPanel friendDockPanel = new () {
			DockPanel = dockPanel,
			Border = border,
			SenderKey = friendRequest.SenderKey,
			ReceiverKey = friendRequest.ReceiverKey,
		};
		
		border.Child = dockPanel;

		_friends.Add(friendDockPanel);
		_mainPanel.Children.Add(border);
	}

	public void RemoveFriend(string foreignKey) {
		FriendDockPanel? friendDockPanel = _friends.Find(friendPanel => friendPanel.ReceiverKey == foreignKey || friendPanel.SenderKey == foreignKey);
		if (friendDockPanel == null)
			return;
		
		_friends.Remove(friendDockPanel);
		_mainPanel.Children.Remove(friendDockPanel.Border);
	}
}