using System;
using System.Windows.Input;
using Avalonia.Controls;

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
		private readonly SelectableTextBlock _textBlock;

		public DeleteMessageCommand(SelectableTextBlock textBlock) => _textBlock = textBlock;
		
		public bool CanExecute(object? parameter) => true;

		public void Execute(object? parameter) {
			throw new NotImplementedException();
		}

		public event EventHandler? CanExecuteChanged;
	}
}