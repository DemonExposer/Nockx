using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using LessAnnoyingHttp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using SecureChat.audio;
using SecureChat.ClassExtensions;
using SecureChat.util;

namespace SecureChat.windows;

public class CallPopupWindowController {
	private readonly CallPopupWindow _context;

	private readonly Sender _sender;
	private readonly Receiver _receiver;
	private readonly Network _network;

	private readonly Popup _volumeSliderTooltip;

	private readonly RsaKeyParameters _privateKey; 
	
	public CallPopupWindowController(CallPopupWindow context) {
		using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
			PemReader pemReader = new (reader);
			_privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
		}
		
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

		string initializationMessage = $"{_context.PersonalKey.ToBase64String()}-{_context.ForeignKey.ToBase64String()}";
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

	public void OnWindowClosing(object? sender, WindowClosingEventArgs e) => _sender.Close();

	private void RegisterVoiceChat(int port) {
		Dispatcher.UIThread.InvokeAsync(() => _context.ConnectionStatusTextBlock.Text = "Registering voice chat...");
		byte[] aesKey = Cryptography.GenerateAesKey();
		byte[] foreignEncryptedAesKey = Cryptography.EncryptAesKey(aesKey, _context.ForeignKey);
		byte[] personalEncryptedAesKey = Cryptography.EncryptAesKey(aesKey, _context.PersonalKey);
		JsonObject body = new () {
			["personalKey"] = _context.PersonalKey.ToBase64String(),
			["foreignKey"] = _context.ForeignKey.ToBase64String(),
			["personalEncryptedKeyBase64"] = Convert.ToBase64String(personalEncryptedAesKey),
			["foreignEncryptedKeyBase64"] = Convert.ToBase64String(foreignEncryptedAesKey),
			["port"] = port
		};
		Response response = Http.Put($"http://{Settings.GetInstance().IpAddress}:5000/voiceChat?timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", JsonSerializer.Serialize(body));
		// TODO: add verification for the encrypted key, so that the server can't spoof its users
		_network.SetKey(Cryptography.DecryptAesKey(Convert.FromBase64String(response.Body), _privateKey));
	}
}