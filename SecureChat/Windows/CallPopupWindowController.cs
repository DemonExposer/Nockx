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
using SecureChat.Audio;
using SecureChat.ClassExtensions;
using SecureChat.Util;

namespace SecureChat.Windows;

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
		_sender = new Sender(context.VolumeProgressBar);
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

	public void OnWindowClosing(object? sender, WindowClosingEventArgs e) {
		_sender.Close();
		JsonObject body = new () {
			["key"] = _context.PersonalKey.ToBase64String(),
			["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};

		string bodyString = JsonSerializer.Serialize(body);
		Response response = Http.Delete($"https://{Settings.GetInstance().Hostname}:5000/voiceChat", bodyString, [new Header { Name = "Signature", Value = Cryptography.Sign(bodyString, _privateKey) }]);
		
		if (_context.Owner is MainWindow mainWindow)
			mainWindow.SetCallWindow(null);
	}

	public void OnWindowOpened(object? sender, EventArgs e) {
		if (_context.Owner is not MainWindow mainWindow)
			return;
		
		mainWindow.SetCallWindow(_context);
	}

	private void RegisterVoiceChat(int port) {
		Dispatcher.UIThread.InvokeAsync(() => _context.ConnectionStatusTextBlock.Text = "Registering voice chat...");
		byte[] aesKey = Cryptography.GenerateAesKey();
		byte[] foreignEncryptedAesKey = Cryptography.EncryptAesKey(aesKey, _context.ForeignKey);
		byte[] personalEncryptedAesKey = Cryptography.EncryptAesKey(aesKey, _context.PersonalKey);
		long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		JsonObject body = new () {
			["personalKey"] = _context.PersonalKey.ToBase64String(),
			["foreignKey"] = _context.ForeignKey.ToBase64String(),
			["personalEncryptedKeyBase64"] = Convert.ToBase64String(personalEncryptedAesKey),
			["foreignEncryptedKeyBase64"] = Convert.ToBase64String(foreignEncryptedAesKey),
			["port"] = port,
			["timestamp"] = timestamp,
			["signature"] = Cryptography.Sign(Convert.ToBase64String(aesKey) + timestamp, _privateKey),
			["signatureWebSocket"] = Cryptography.Sign(_context.PersonalKey.ToBase64String() + timestamp, _privateKey)
		};
		
		string bodyString = JsonSerializer.Serialize(body);
		Response response = Http.Put($"https://{Settings.GetInstance().Hostname}:5000/voiceChat", bodyString, [new Header { Name = "Signature", Value = Cryptography.Sign(bodyString, _privateKey) }]);
		Dispatcher.UIThread.InvokeAsync(() => _context.ConnectionStatusTextBlock.Text = "Verifying...");
		JsonObject responseObj = JsonNode.Parse(response.Body)!.AsObject();
		
		long receivedTimestamp = responseObj["timestamp"]!.GetValue<long>();
		if (_context.Timestamp != null && receivedTimestamp != _context.Timestamp.Value) // Check timestamp the server sent vs the timestamp that was sent with the call request (if timestamp is null, this user is the initiator of the call)
			return; // TODO: inform user that they are under attack
		
		byte[] receivedAesKey = Cryptography.DecryptAesKey(Convert.FromBase64String(responseObj["encryptedSymmetricKey"]!.GetValue<string>()), _privateKey);
		if ((_context.Timestamp == null && receivedAesKey.SequenceEqual(aesKey)) || (_context.Timestamp != null && Cryptography.Verify(Convert.ToBase64String(receivedAesKey) + receivedTimestamp, responseObj["signature"]!.GetValue<string>(), null, _context.ForeignKey, false))) {
			_network.SetKey(receivedAesKey);
			Dispatcher.UIThread.InvokeAsync(() => _context.ConnectionStatusTextBlock.Text = "Connected");
		} else {
			Dispatcher.UIThread.InvokeAsync(() => _context.ConnectionStatusTextBlock.Text = "Unable to verify. Possible man-in-the-middle attack");
		}
	}
}