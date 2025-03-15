using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SecureChat.Panels;
using System;

namespace SecureChat.CustomControls;

public partial class MessageItem : UserControl {
	public long Id { get; init; }
	public bool IsPersonal { get; init; }
	public string SenderName {
		get => _nameBlock.Text ?? "";
		set => _nameBlock.Text = value;
	}
	public string Message {
		get => _messageBlock.Text ?? "";
		set => _messageBlock.Text = value;
	}
	public long Timestamp { // Just an approximation, so the returned timestamp will only be accurate to the minute
		get => DateTimeOffset.ParseExact(_dateTimeBlock.Text, "dd MMMM yyyy HH:mm", null).ToUnixTimeMilliseconds();
		set => _dateTimeBlock.Text = DateTimeOffset.FromUnixTimeMilliseconds(value).LocalDateTime.ToString("dd MMMM yyyy HH:mm");
	}

	private readonly SelectableTextBlock _nameBlock, _dateTimeBlock, _messageBlock;
	private readonly MenuItem _copyMenuItem, _deleteMenuItem;
	private readonly ContextMenu _contextMenu;

	public MessageItem() : this(null) {	}

	public MessageItem(ChatPanelCommands.DeleteMessageCommand.Callback? callback) {
		InitializeComponent();

		_nameBlock = this.FindControl<SelectableTextBlock>("PART_Name")!;
		_dateTimeBlock = this.FindControl<SelectableTextBlock>("PART_DateTime")!;
		_messageBlock = this.FindControl<SelectableTextBlock>("PART_Message")!;

		_nameBlock.ContextFlyout = null;
		_messageBlock.ContextFlyout = null;

		_copyMenuItem = new MenuItem {
			Header = "Copy",
			Name = "CopyMenuItem",
			Cursor = new Cursor(StandardCursorType.Arrow)
		};

		_deleteMenuItem = new MenuItem {
			Header = "Delete message",
			Name = "DeleteMenuItem",
			Cursor = new Cursor(StandardCursorType.Arrow),
			Command = new ChatPanelCommands.DeleteMessageCommand(this, callback)
		};

		_deleteMenuItem.Classes.Add("delete_menu_item");

		_contextMenu = new ContextMenu {
			Items = {
				_copyMenuItem,
				_deleteMenuItem
			}
		};

		// PointerReleased because PointerPressed doesn't work on SelectableTextBlock
		_nameBlock.PointerReleased += TextBlock_OnPointerReleased;
		_messageBlock.PointerReleased += TextBlock_OnPointerReleased;

		PointerEntered += OnPointerEntered;
		PointerExited += OnPointerExited;
		PointerPressed += OnPointerPressed;
	}

	public void OnPointerEntered(object? sender, PointerEventArgs e) => Background = new SolidColorBrush(Color.Parse("#252525"));

	public void OnPointerExited(object? sender, PointerEventArgs e) => Background = (SolidColorBrush) App.Current!.Resources["ApplicationBackgroundColor"]!;

	public void OnPointerPressed(object? sender, PointerPressedEventArgs e) {
		if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed)
			return;

		_copyMenuItem.IsVisible = false;
		_deleteMenuItem.IsVisible = IsPersonal;
		_contextMenu.Open((Control) e.Source!);
	}

	public void TextBlock_OnPointerReleased(object? sender, PointerReleasedEventArgs e) {
		if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.RightButtonReleased)
			return;

		SelectableTextBlock source = (SelectableTextBlock) sender!;

		int selectionStart = source.SelectionStart;
		int selectionEnd = source.SelectionEnd;

		_copyMenuItem.Command = new ChatPanelCommands.CopyCommand(source);
		_copyMenuItem.IsVisible = source.SelectedText != "";
		_deleteMenuItem!.IsVisible = IsPersonal;
		_contextMenu.Open((Control) e.Source!);

		source.SelectionStart = selectionStart;
		source.SelectionEnd = selectionEnd;
	}
}
