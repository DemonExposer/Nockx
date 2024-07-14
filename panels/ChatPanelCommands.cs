using System;
using System.Windows.Input;
using Avalonia.Controls;
using SecureChat.util;

namespace SecureChat.panels;

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
		
		private readonly MessageTextBlock _textBlock;
		private readonly Callback _callback;

		public DeleteMessageCommand(MessageTextBlock textBlock, Callback callback) {
			_textBlock = textBlock;
			_callback = callback;
		}

		public bool CanExecute(object? parameter) => true;

		public void Execute(object? parameter) => _callback(_textBlock.Id);

		public event EventHandler? CanExecuteChanged;
	}
}