using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SecureChat.Panels;

namespace SecureChat.CustomControls;

public partial class MessageItem : UserControl {
	public long Id { get; init; }
	public bool IsPersonal { get; init; }
	public string SenderName {
		get => _nameBlock.Text;
		set => _nameBlock.Text = value;
	}
	public string Message {
		get => _messageBlock.Text;
		set => _messageBlock.Text = value;
	}

	private readonly StackPanel _stackPanel;
	private readonly SelectableTextBlock _nameBlock, _messageBlock;
	private readonly MenuItem _copyMenuItem, _deleteMenuItem;
	private readonly ContextMenu _contextMenu;

	public MessageItem() : this(null) {	}

	public MessageItem(ChatPanelCommands.DeleteMessageCommand.Callback? callback) {
		InitializeComponent();

		_stackPanel = this.FindControl<StackPanel>("PART_Panel")!;
		_nameBlock = this.FindControl<SelectableTextBlock>("PART_Name")!;
		_messageBlock = this.FindControl<SelectableTextBlock>("PART_Message")!;

		_nameBlock.ContextFlyout = null;
		_messageBlock.ContextFlyout = null;

		_copyMenuItem = new MenuItem {
			Header = "Copy",
			Name = "CopyMenuItem",
			Cursor = new Cursor(StandardCursorType.Arrow),
			Command = new ChatPanelCommands.CopyCommand(_messageBlock) // TODO: Make this what is actually selected in the MessageItem, not just in PART_Message
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

		PointerEntered += OnPointerEntered;
		PointerExited += OnPointerExited;
		PointerPressed += OnPointerPressed;
	}

	public void OnPointerEntered(object? sender, PointerEventArgs e) => _stackPanel.Background = new SolidColorBrush(Color.Parse("#252525"));

	public void OnPointerExited(object? sender, PointerEventArgs e) => _stackPanel.Background = (SolidColorBrush) Resources["ApplicationBackgroundColor"]!;

	public void OnPointerPressed(object? sender, PointerPressedEventArgs e) {
		if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed)
			return;

		_copyMenuItem.IsVisible = false;
		_deleteMenuItem.IsVisible = IsPersonal;
		_contextMenu.Open((Control) e.Source!);
	}
}
