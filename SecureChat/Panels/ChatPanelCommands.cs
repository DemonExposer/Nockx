using System;
using System.Windows.Input;
using Avalonia.Controls;
using SecureChat.CustomControls;

namespace SecureChat.Panels;

public static class ChatPanelCommands {
	public class CopyCommand(SelectableTextBlock textBlock) : ICommand {
		public bool CanExecute(object? parameter) => true;
		
		public void Execute(object? parameter) => textBlock.Copy();

		public event EventHandler? CanExecuteChanged;
	}

	public class CopyPublicKeyCommand(MessageItem messageItem, SelectableTextBlock textBlock) : ICommand {
		public bool CanExecute(object? parameter) => true;

		public void Execute(object? parameter) => TopLevel.GetTopLevel(textBlock)?.Clipboard?.SetTextAsync(messageItem.PublicKey).Wait();

		public event EventHandler? CanExecuteChanged;
	}

	public class DeleteMessageCommand(MessageItem messageItem, DeleteMessageCommand.Callback callback) : ICommand {
		public delegate void Callback(long id);

		public bool CanExecute(object? parameter) => true;

		public void Execute(object? parameter) => callback(messageItem.Id);

		public event EventHandler? CanExecuteChanged;
	}
}