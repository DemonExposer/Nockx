using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using SecureChat.audio;
using SecureChat.util;
using SecureChat.windows;

namespace SecureChat;

public class MainWindowController {
	private readonly RsaKeyParameters _publicKey, _privateKey;

	private readonly MainWindow _context;
	private readonly MainWindowModel _model;
	private ClientWebSocket _webSocket;

	private bool _isWebsocketInitialized = false;

	public MainWindowController(MainWindow context, MainWindowModel model) {
		_context = context;
		_model = model;

		using (StreamReader reader = File.OpenText(Constants.PublicKeyFile)) {
			PemReader pemReader = new (reader);
			_publicKey = (RsaKeyParameters) pemReader.ReadObject();
		}

		using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
			PemReader pemReader = new (reader);
			_privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
		}

		// Load chats from file
		Chats.Show(_context);
		
		// Check if new chats were created while offline
		CheckForNewChats();

		_webSocket = new ClientWebSocket();
		_ = InitializeWebsocket(true);
	}

	private void CheckForNewChats() {
		string getVariables = $"modulus={_publicKey.Modulus.ToString(16)}&exponent={_publicKey.Exponent.ToString(16)}&timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
		Https.Response response = Https.Get($"http://{Settings.GetInstance().IpAddress}:5000/chats?{getVariables}", [new Https.Header {Name = "Signature", Value = Cryptography.Sign(getVariables, _privateKey)}]);
		if (!response.IsSuccessful) {
			_context.ShowPopupWindowOnTop(new ErrorPopupWindow($"Could not retrieve chats from server ({Settings.GetInstance().IpAddress})"));
			return;
		}

		JsonArray chats = JsonNode.Parse(response.Body)!.AsArray();
		foreach (JsonNode? jsonNode in chats) {
			JsonObject chatObject = jsonNode!.AsObject();
			if (chatObject["user1"]!["modulus"]!.GetValue<string>() != _publicKey.Modulus.ToString(16)) {
				_context.AddUser(new RsaKeyParameters(
					false,
					new BigInteger(chatObject["user1"]!["modulus"]!.GetValue<string>(), 16),
					new BigInteger(chatObject["user1"]!["exponent"]!.GetValue<string>(), 16)
				), chatObject["user1"]!["modulus"]!.GetValue<string>(), false);
				_model.SetChatReadStatus(chatObject["user1"]!["modulus"]!.GetValue<string>(), chatObject["isRead"]!.GetValue<bool>());
			} else {
				_context.AddUser(new RsaKeyParameters(
					false,
					new BigInteger(chatObject["user2"]!["modulus"]!.GetValue<string>(), 16),
					new BigInteger(chatObject["user2"]!["exponent"]!.GetValue<string>(), 16)
				), chatObject["user2"]!["modulus"]!.GetValue<string>(), false);
				_model.SetChatReadStatus(chatObject["user2"]!["modulus"]!.GetValue<string>(), chatObject["isRead"]!.GetValue<bool>());
			}
		}
	}

	private async Task InitializeWebsocket(bool isCalledFromUI) {
		using CancellationTokenSource cts = new (5000);
		try {
			await _webSocket.ConnectAsync(new Uri($"ws://{Settings.GetInstance().IpAddress}:5000/ws"), cts.Token);

			byte[] modulusStrBytes = Encoding.UTF8.GetBytes(_publicKey.Modulus.ToString(16));
			await _webSocket.SendAsync(modulusStrBytes, WebSocketMessageType.Text, true, CancellationToken.None);
			_isWebsocketInitialized = true;
		} catch (OperationCanceledException) when (cts.IsCancellationRequested) {
			if (!isCalledFromUI)
				return;
			_context.ShowPopupWindowOnTop(new ErrorPopupWindow($"Websocket timeout ({Settings.GetInstance().IpAddress})"));
		} catch (WebSocketException) {
			if (!isCalledFromUI)
				return;
			_context.ShowPopupWindowOnTop(new ErrorPopupWindow($"Websocket could not be connected ({Settings.GetInstance().IpAddress})"));
		} catch (Exception e) {
			Console.WriteLine(e.ToString());
		}
	}

	private async Task ReinitializeWebsocket() { // TODO: retrieve all messages after reinitialization, so that missed messages are loaded
		if (_isWebsocketInitialized) {
			_isWebsocketInitialized = false;
			_webSocket.Abort();
			_webSocket.Dispose();
		} else {
			_isWebsocketInitialized = false;
		}

		_webSocket = new ClientWebSocket();

		await InitializeWebsocket(false);
	}

	public async Task ListenOnWebsocket() { // TODO: put async methods in a big try/catch because they can silent fail
		while (!_isWebsocketInitialized)
			await Task.Delay(1000);
		
		int arrSize = 1024;
		byte[] buffer = new byte[arrSize];

		Timer timer = new (_ => {
			if (_webSocket.State != WebSocketState.Open)
				ReinitializeWebsocket().Wait();
		}, null, 0, 5000);
		
		while (true) {
			List<byte> bytes = [];
			WebSocketReceiveResult result;
			try {
				if (!_isWebsocketInitialized)
					throw new WebSocketException();
				
				do {
					result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
					bytes.AddRange(buffer[..result.Count]);
				} while (result.Count == arrSize);
			} catch (WebSocketException e) {
				Console.WriteLine(e.ToString());
				await Task.Delay(500);
				continue;
			}

			Dictionary<string, Action<JsonObject>> actions = new () {
				["add"] = message => {
					Sounds.Notification.Play();
					AddMessage(message);
				},
				["delete"] = DeleteMessage
			};
			
			JsonObject messageJson = JsonNode.Parse(Encoding.UTF8.GetString(bytes.ToArray()))!.AsObject();
			actions[messageJson["action"]!.GetValue<string>()](messageJson);
		}
	}

	private void AddMessage(JsonObject messageJson) {
		Message message = Message.Parse(messageJson);
		
		// Add new chat if receiver does not yet have a chat with sender
		if (!Chats.ChatExists(message.Sender)) {
			Chats.Add(message.Sender);
			_context.AddUser(message.Sender, message.Sender.Modulus.ToString(16), false);
		}

		RsaKeyParameters? currentChatForeignPublicKey = _context.GetCurrentChatIdentity();
		if (currentChatForeignPublicKey == null || !currentChatForeignPublicKey.Equals(message.Sender)) {
			if (!message.IsRead)
				_model.SetChatReadStatus(message.Sender.Modulus.ToString(16), false);
			
			return;
		}

		_context.ChatPanel.DecryptAndAddMessage(message);
	}

	private void DeleteMessage(JsonObject messageJson) {
		RsaKeyParameters sender = new (
			false,
			new BigInteger(messageJson["sender"]!["modulus"]!.GetValue<string>(), 16),
			new BigInteger(messageJson["sender"]!["exponent"]!.GetValue<string>(), 16)
		);
		
		RsaKeyParameters? currentChatForeignPublicKey = _context.GetCurrentChatIdentity();
		if (currentChatForeignPublicKey == null || !currentChatForeignPublicKey.Equals(sender))
			return;

		_context.ChatPanel.RemoveMessage(messageJson["id"]!.GetValue<long>());
	}
}
