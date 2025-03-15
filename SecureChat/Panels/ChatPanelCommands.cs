using System;
using System.Windows.Input;
using Avalonia.Controls;
using SecureChat.CustomControls;

namespace SecureChat.Panels;

public static class ChatPanelCommands {
	public class CopyCommand : ICommand {
		private readonly SelectableTextBlock _textBlock;

		public CopyCommand(SelectableTextBlock textBlock) => _textBlock = textBlock;

		public bool CanExecute(object? parameter) => true;
		
		public void Execute(object? parameter) => _textBlock.Copy();

		public event EventHandler? CanExecuteChanged;
	}

	public class DeleteMessageCommand : ICommand {
		public delegate void Callback(long id);
		
		private readonly MessageItem _messageItem;
		private readonly Callback _callback;

		public DeleteMessageCommand(MessageItem messageItem, Callback callback) {
			_messageItem = messageItem;
			_callback = callback;
		}

		public bool CanExecute(object? parameter) => true;

		public void Execute(object? parameter) => _callback(_messageItem.Id);

		public event EventHandler? CanExecuteChanged;
	}
}