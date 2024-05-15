using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace SecureChat;

public partial class MainWindow : Window {
	private readonly MainWindowController _controller;
	
	public MainWindow() {
		_controller = new MainWindowController("", ""); // TODO: add keys here
		
		InitializeComponent();
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);

		StackPanel messagePanel = this.FindControl<StackPanel>("MessagePanel")!;
		// TODO: fill the message panel with all the previous messages
		
		TextBox messageBox = this.FindControl<TextBox>("MessageBox")!;
		messageBox.KeyDown += (_, args) => {
			if (args.Key == Key.Enter) {
				if (messageBox.Text == null)
					return;
				
				_controller.Send(messageBox.Text);
			}
		};
	}
}