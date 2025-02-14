using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using SecureChat.ClassExtensions;
using SecureChat.extended_controls;
using SecureChat.util;

namespace SecureChat.Panels;

public class FriendsPanel : DockPanel {
	private readonly FriendsPanelController _controller;
	private readonly List<FriendDockPanel> _friends = [];
	private StackPanel? _friendsPanel;
	private bool _isShowCalled;

	public FriendsPanel() {
		_controller = new FriendsPanelController();
		
		Dispatcher.UIThread.InvokeAsync(() => {
			using IEnumerator<ILogical> enumerator = this.GetLogicalDescendants().GetEnumerator();
			while (enumerator.MoveNext()) {
				if (enumerator.Current.GetType() == typeof(StackPanel)) {
					_friendsPanel = (StackPanel) enumerator.Current;
				}
			}
		});
	}

	public void Show(MainWindow window) {
		List<FriendRequest> friends = _controller.GetFriends();
		foreach (FriendRequest friend in friends) {
			AddFriend(friend, window);
		}
		_isShowCalled = true;
	}

	public void Unshow() {
		if (!_isShowCalled)
			return;
		_friendsPanel.Children.RemoveAll(_friends.Select(block => block.Border));
		_friends.Clear();
	}

	private void AddFriend(FriendRequest friendRequest, MainWindow window) {
		if (_friendsPanel == null) {
			Console.WriteLine("AddFriend: FriendsPanel is not initialized");
			return;
		}
		
		IBrush originalBackground = new SolidColorBrush(Color.Parse("Transparent"));
		Border border = new() {
			Background = originalBackground
		};

		Button button = new () {
			Margin = new Thickness(5)
		};
		SetDock(button, Dock.Right);
		bool amISender = friendRequest.SenderKey == _controller.PersonalPublicKey.ToBase64String();
		SelectableTextBlock textBlock = new () {
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(5),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Width = double.NaN,
			Text = amISender ? friendRequest.ReceiverName : friendRequest.SenderName
		};
		Button rejectButton = new() {
			Margin = new Thickness(5),
			Content = "Reject",
			Classes = { "reject" }
		};
		SetDock(rejectButton, Dock.Right);
		rejectButton.Click += (_, _) => { _controller.DeleteFriendRequest(friendRequest); Unshow(); Show(window); };
		if (friendRequest.Accepted) {
			button.Content = "Remove";
			button.Classes.Add("reject");
			button.Click += (_, _) => { _controller.DeleteFriendRequest(friendRequest); Unshow(); Show(window); };
		} else {
			if (amISender) {
				button.Content = "Cancel";
				button.Classes.Add("cancel");
				button.Click += (_, _) => { _controller.DeleteFriendRequest(friendRequest); Unshow(); Show(window); };
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
		chatButton.Click += (_, _) => { window.AddUser(RsaKeyParametersExtension.FromBase64String(key), name, true); };

		DockPanel dockPanel = new();
		if (!friendRequest.Accepted && !amISender)
			dockPanel.Children.Add(rejectButton);
		dockPanel.Children.Add(button);
		if (friendRequest.Accepted)
			dockPanel.Children.Add(chatButton);
		dockPanel.Children.Add(textBlock);

		FriendDockPanel friendDockPanel = new() {
			DockPanel = dockPanel,
			Border = border,
			SenderKey = friendRequest.SenderKey,
			ReceiverKey = friendRequest.ReceiverKey,
		};
		
		border.Child = dockPanel;

		_friends.Add(friendDockPanel);
		_friendsPanel.Children.Add(border);
	}
}