using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using LessAnnoyingHttp;
using SecureChat.audio;
using SecureChat.util;

namespace SecureChat.windows;

public class CallPopupWindowController {
	private readonly CallPopupWindow _context;

	private readonly Sender _sender;
	private readonly Receiver _receiver;
	private readonly Network _network;

	private readonly Popup _volumeSliderTooltip;
	
	public CallPopupWindowController(CallPopupWindow context) {
		_context = context;
		_sender = new Sender();
		_receiver = new Receiver();
		_network = new Network(_receiver.OnReceive, new IPEndPoint(Settings.GetInstance().IpAddress, 5001), RegisterVoiceChat);
		_context.ConnectionStatusTextBlock.Text = "Pinging server...";
		_network.Run();

		_volumeSliderTooltip = _context.FindControl<Popup>("MovableTooltip")!;
		_context.TooltipTextBlock = _context.FindControl<TextBlock>("TooltipText")!;
		
		IEnumerable<ComboBoxItem> comboBoxItems = _sender.Devices.Select(inputDevice => new ComboBoxItem { Content = inputDevice });
		comboBoxItems.ToList().ForEach(item => _context.InputSelectorComboBox.Items.Add(item));
		_context.InputSelectorComboBox.SelectedIndex = 0;

		string initializationMessage = $"{_context.PersonalKey.Modulus.ToString(16)}-{_context.ForeignKey.Modulus.ToString(16)}";
		_network.Send(Encoding.UTF8.GetBytes(initializationMessage), initializationMessage.Length);
		_sender.Run(_network);
	}

	public void OnInputDeviceChanged(object? sender, SelectionChangedEventArgs e) {
		ComboBox inputSelector = (ComboBox) sender!;
		_sender.SetInputDevice(inputSelector.SelectedIndex);
	}

	public void OnMousePointerEnteredSlider(object? sender, PointerEventArgs e) {
		Point position = e.GetPosition(_context);
		_volumeSliderTooltip.HorizontalOffset = position.X;
		_volumeSliderTooltip.VerticalOffset = position.Y;
		_volumeSliderTooltip.IsOpen = true;
	}

	public void OnMousePointerExitedSlider(object? sender, PointerEventArgs e) => _volumeSliderTooltip.IsOpen = false;
	
	public void OnMousePointerMovedInSlider(object? sender, PointerEventArgs e) {
		if (!_volumeSliderTooltip.IsOpen)
			return;
			
		Point position = e.GetPosition(_context);
		_volumeSliderTooltip.HorizontalOffset = position.X;
		_volumeSliderTooltip.VerticalOffset = position.Y;
	}

	public void OnVolumeSliderValueChanged(object? sender, RangeBaseValueChangedEventArgs e) {
		_context.TooltipTextBlock.Text = (int) e.NewValue + "%";
		_receiver.SetVolume((float) e.NewValue / 100);
	}

	private void RegisterVoiceChat(int port) {
		Dispatcher.UIThread.InvokeAsync(() => _context.ConnectionStatusTextBlock.Text = "Registering voice chat...");
		byte[] aesKey = Cryptography.GenerateAesKey();
		byte[] encryptedAesKey = Cryptography.EncryptAesKey(aesKey, _context.ForeignKey);
		Response response = Http.Put($"http://{Settings.GetInstance().IpAddress}:5000/voiceChat?personalModulus={_context.PersonalKey.Modulus.ToString(16)}&foreignModulus={_context.ForeignKey.Modulus.ToString(16)}&encryptedKeyBase64={Convert.ToBase64String(encryptedAesKey)}&port={port}&timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", "{}");
		
	}
}