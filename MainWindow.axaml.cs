using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SecureChat.model;
using System;
using System.Collections.Generic;

namespace SecureChat {
	public partial class MainWindow : Window {
		private readonly MainWindowController _controller;
	
		public MainWindow() {
			_controller = new MainWindowController(null); // TODO: pass the foreign public key to the controller
		
			InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);

			StackPanel messagePanel = this.FindControl<StackPanel>("MessagePanel")!;

			List<Message> messages = new() {
				new Message { Body = "Hello there, how are you doing?", DateTime = DateTime.Now.AddMinutes(-10), UserName = "User1" },
				new Message { Body = "What's up? Any plans for the weekend?", DateTime = DateTime.Now.AddMinutes(-5), UserName = "User1" },
				new Message { Body = "Hey answer me!", DateTime = DateTime.Now, UserName = "User1" }
			}; 

			foreach (Message message in messages) {
				string fullMessage = $"{message.DateTime.ToString("yyyy-MM-dd HH:mm:ss")} | {message.UserName} | {message.Body}";
				TextBlock messageTextBlock = new() {
					Text = fullMessage,
					Margin = new Thickness(5)
				};
				messagePanel.Children.Add(messageTextBlock);
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
}