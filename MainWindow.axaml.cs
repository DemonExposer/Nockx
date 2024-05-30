using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace SecureChat;

public partial class MainWindow : Window {
	private readonly MainWindowController _controller;
	
	public MainWindow() {
		_controller = new MainWindowController(null); // TODO: pass the foreign public key to the controller
		
		InitializeComponent();
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);

		StackPanel messagePanel = this.FindControl<StackPanel>("MessagePanel")!;
		// TODO: fill the message panel with all the previous messages
		
		foreach (string message in _controller.GetPastMessages()) {
			Console.WriteLine(message);
		}
        
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