using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using SecureChat.ClassExtensions;
using SecureChat.Panels;
using System;
using System.Threading.Tasks;

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
	public string PublicKey {
		get => string.Join("", (_tooltipTextBlock.Text ?? "").Split('\n'));
		set => _tooltipTextBlock.Text = value.Wrap(50);
	}

	private readonly SelectableTextBlock _nameBlock, _dateTimeBlock, _messageBlock;
	private readonly MenuItem _copyMenuItem, _deleteMenuItem;
	private readonly ContextMenu _contextMenu;
	private readonly Popup _tooltip;
	private readonly TextBlock _tooltipTextBlock;

	private bool _isPointerOverName;

	public MessageItem() : this(null) {	}

	public MessageItem(ChatPanelCommands.DeleteMessageCommand.Callback? callback) {
		InitializeComponent();

		_nameBlock = this.FindControl<SelectableTextBlock>("PART_Name")!;
		_dateTimeBlock = this.FindControl<SelectableTextBlock>("PART_DateTime")!;
		_messageBlock = this.FindControl<SelectableTextBlock>("PART_Message")!;
		_tooltip = this.FindControl<Popup>("Tooltip")!;
		_tooltipTextBlock = this.FindControl<TextBlock>("TooltipText")!;

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

		_nameBlock.PointerEntered += NameBlock_OnPointerEntered;
		_nameBlock.PointerExited += NameBlock_OnPointerExited;

		// PointerReleased because PointerPressed doesn't work on SelectableTextBlock
		_nameBlock.PointerReleased += TextBlock_OnPointerReleased;
		_messageBlock.PointerReleased += TextBlock_OnPointerReleased;

		PointerEntered += OnPointerEntered;
		PointerExited += OnPointerExited;
		PointerPressed += OnPointerPressed;
	}

	private void NameBlock_OnPointerEntered(object? sender, PointerEventArgs e) {
		_isPointerOverName = true;

		Task.Run(() => {
			Task.Delay(500).Wait();
			if (!_isPointerOverName)
				return;

			Dispatcher.UIThread.InvokeAsync(() => {
				Point position = e.GetPosition(this);
				_tooltip.HorizontalOffset = position.X;
				_tooltip.VerticalOffset = position.Y;
				_tooltip.IsOpen = true;
			});
		});
	}

	private void NameBlock_OnPointerExited(object? sender, PointerEventArgs e) {
		_isPointerOverName = false;
		_tooltip.IsOpen = false;
	}

	private void OnPointerEntered(object? sender, PointerEventArgs e) => Background = new SolidColorBrush(Color.Parse("#252525"));

	private void OnPointerExited(object? sender, PointerEventArgs e) => Background = (SolidColorBrush) App.Current!.Resources["ApplicationBackgroundColor"]!;

	private void OnPointerPressed(object? sender, PointerPressedEventArgs e) {
		if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed)
			return;

		_copyMenuItem.IsVisible = false;
		_deleteMenuItem.IsVisible = IsPersonal;
		_contextMenu.Open((Control) e.Source!);
	}

	private void TextBlock_OnPointerReleased(object? sender, PointerReleasedEventArgs e) {
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
