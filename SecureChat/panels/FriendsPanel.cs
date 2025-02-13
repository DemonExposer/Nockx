﻿using System;
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
using SecureChat.extended_controls;
using SecureChat.util;

namespace SecureChat.panels;

public class FriendsPanel : DockPanel {
	private readonly FriendsPanelController _controller;
	private readonly List<FriendDockPanel> _friends = [];
	private StackPanel? _friendsPanel;
	private bool _isShowCalled;

	public delegate void DataCallback(RsaKeyParameters publicKey, string name, bool doAutoFocus);

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
		string mod = friendRequest.SenderModulus;
		string exp = friendRequest.SenderExponent;
		string name = friendRequest.SenderName;
		if (amISender) {
			mod = friendRequest.ReceiverModulus;
			exp = friendRequest.ReceiverExponent;
			name = friendRequest.ReceiverName;
		}
		RsaKeyParameters publicKey = new (false, new BigInteger(mod, 16), new BigInteger(exp, 16));
		chatButton.Click += (_, _) => { window.AddUser(publicKey, name, true); };

		DockPanel dockPanel = new();
		dockPanel.Children.Add(button);
		if (friendRequest.Accepted)
			dockPanel.Children.Add(chatButton);
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