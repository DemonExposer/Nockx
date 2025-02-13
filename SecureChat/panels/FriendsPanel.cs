using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using SecureChat.extended_controls;
using SecureChat.util;

namespace SecureChat.panels;

public class FriendsPanel : DockPanel {
	private readonly FriendsPanelController _controller;
	private readonly List<FriendDockPanel> _friends = [];
	private StackPanel? _friendsPanel;
	private bool _isShowCalled;
	public FriendsPanel() {
		_controller = new FriendsPanelController(this);
		
		Dispatcher.UIThread.InvokeAsync(() => {
			using IEnumerator<ILogical> enumerator = this.GetLogicalDescendants().GetEnumerator();
			while (enumerator.MoveNext()) {
				if (enumerator.Current.GetType() == typeof(StackPanel)) {
					_friendsPanel = (StackPanel) enumerator.Current;
				}
			}
		});
	}

	public void Show() {
		_controller.GetFriends();
		_isShowCalled = true;
	}

	public void Unshow() {
		if (!_isShowCalled)
			return;
		_friendsPanel.Children.RemoveAll(_friends.Select(block => block.Border));
		_friends.Clear();
	}

	public void AddFriend(FriendRequest friendRequest) {
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
		bool amISender = friendRequest.SenderModulus == _controller.PersonalPublicKey.Modulus.ToString(16);
		SelectableTextBlock textBlock = new () {
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(5),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Width = double.NaN,
			Text = amISender ? friendRequest.ReceiverName : friendRequest.SenderName
		};
		if (friendRequest.Accepted) {
			button.Content = "Remove";
			button.Classes.Add("reject");
			button.Click += (_, _) => { _controller.DeleteFriendRequest(friendRequest); Unshow(); Show(); };
		} else {
			if (amISender) {
				button.Content = "Cancel";
				button.Classes.Add("cancel");
				button.Click += (_, _) => { _controller.DeleteFriendRequest(friendRequest); Unshow(); Show(); };
			} else {
				button.Content = "Accept";
				button.Classes.Add("accept");
				button.Click += (_, _) => { _controller.AcceptFriendRequest(friendRequest); Unshow(); Show(); };
			}
		}

		DockPanel dockPanel = new();
		dockPanel.Children.Add(button);
		dockPanel.Children.Add(textBlock);
		
		FriendDockPanel friendDockPanel = new() {
			DockPanel = dockPanel,
			Border = border,
			SenderModulus = friendRequest.SenderModulus,
			SenderExponent = friendRequest.SenderExponent,
			ReceiverModulus = friendRequest.ReceiverModulus,
			ReceiverExponent = friendRequest.ReceiverExponent
		};
		
		border.Child = dockPanel;

		_friends.Add(friendDockPanel);
		_friendsPanel.Children.Add(border);
	}
}