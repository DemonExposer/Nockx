using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace SecureChat.Windows;

public partial class DialogWindow : PopupWindow {
	private readonly Action<bool> _action;

	public DialogWindow(string message, Action<bool> actionOnResult) {
		InitializeComponent();
		this.FindControl<TextBlock>("DisplayText")!.Text = message;
		_action = actionOnResult;
	}

	private void AcceptButton_OnClick(object? sender, RoutedEventArgs e) {
		_action(true);
		Close();
	}

	private void RejectButton_OnClick(object? sender, RoutedEventArgs e) {
		_action(false);
		Close();
	}
}
