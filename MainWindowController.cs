using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using SecureChat.util;

namespace SecureChat;

public class MainWindowController {
	public RsaKeyParameters PublicKey;

	private readonly MainWindow _context;
	private ClientWebSocket _webSocket;

	private bool isWebsocketInitialized = false;

	public MainWindowController(MainWindow context) {
		_context = context;

		using StreamReader reader = File.OpenText(Constants.PublicKeyFile);
		PemReader pemReader = new (reader);
		PublicKey = (RsaKeyParameters) pemReader.ReadObject();

		// Load chats from file
		Chats.Show(_context);
		
		// Check if new chats were created while offline
		CheckForNewChats();

		_webSocket = new ClientWebSocket();
		_ = InitializeWebsocket();
	}

	private void CheckForNewChats() {
		// TODO: Implement this. Note: this request should be signed
	//	string getVariables = $"modulus={PublicKey.Modulus.ToString(16)}&exponent={PublicKey.Exponent.ToString(16)}";
	//	Https.Get($"http://{Settings.GetInstance().IpAddress}:5000/chats?{getVariables}");
	}

	private async Task InitializeWebsocket() {
		using CancellationTokenSource cts = new (5000);
		try {
			await _webSocket.ConnectAsync(new Uri($"ws://{Settings.GetInstance().IpAddress}:5000/ws"), cts.Token);

			byte[] modulusStrBytes = Encoding.UTF8.GetBytes(PublicKey.Modulus.ToString(16));
			await _webSocket.SendAsync(modulusStrBytes, WebSocketMessageType.Text, true, CancellationToken.None);
			isWebsocketInitialized = true;
		} catch (OperationCanceledException) when (cts.IsCancellationRequested) {
			Console.WriteLine("websocket timeout"); // TODO: handle this differently
		} catch (WebSocketException) {
			Console.WriteLine("connection failed");
		} catch (Exception e) {
			Console.WriteLine(e.ToString());
		}
	}

	private async Task ReinitializeWebsocket() { // TODO: retrieve all messages after reinitialization, so that missed messages are loaded
		if (isWebsocketInitialized) {
			isWebsocketInitialized = false;
			_webSocket.Abort();
			_webSocket.Dispose();
		} else {
			isWebsocketInitialized = false;
		}

		_webSocket = new ClientWebSocket();

		await InitializeWebsocket();
	}

	public async Task ListenOnWebsocket() { // TODO: put async methods in a big try/catch because they can silent fail
		while (!isWebsocketInitialized)
			await Task.Delay(1000);
		
		int arrSize = 1024;
		byte[] buffer = new byte[arrSize];

		Timer timer = new (_ => {
			if (_webSocket.State != WebSocketState.Open)
				ReinitializeWebsocket().Wait();
		}, null, 0, 5000);
		
		while (true) {
			List<byte> bytes = new ();
			WebSocketReceiveResult result;
			try {
				if (!isWebsocketInitialized)
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

			IDictionary<string, Action<JsonObject>> actions = new Dictionary<string, Action<JsonObject>>();
			actions["add"] = AddMessage;
			actions["delete"] = DeleteMessage;
			
			JsonObject messageJson = JsonNode.Parse(Encoding.UTF8.GetString(bytes.ToArray()))!.AsObject();
			actions[messageJson["action"]!.GetValue<string>()](messageJson);
		}
	}

	private void AddMessage(JsonObject messageJson) {
		Message message = new () {
			Id = messageJson["id"]!.GetValue<long>(),
			Body = messageJson["text"]!.GetValue<string>(),
			Sender = new RsaKeyParameters(false, new BigInteger(messageJson["sender"]!["modulus"]!.GetValue<string>(), 16), new BigInteger(messageJson["sender"]!["exponent"]!.GetValue<string>(), 16)),
			ReceiverEncryptedKey = messageJson["receiverEncryptedKey"]!.GetValue<string>(),
			Signature = messageJson["signature"]!.GetValue<string>()
		};
		
		// Add new chat if receiver does not yet have a chat with sender
		if (!Chats.ChatExists(message.Sender)) {
			Chats.Add(message.Sender);
			_context.AddUser(message.Sender, message.Sender.Modulus.ToString(16), false);
		}

		RsaKeyParameters? currentChatForeignPublicKey = _context.GetCurrentChatIdentity();
		if (currentChatForeignPublicKey == null || !currentChatForeignPublicKey.Equals(message.Sender))
			return;

		_context.ChatPanel.DecryptAndAddMessage(message);
	}

	private void DeleteMessage(JsonObject messageJson) {
		RsaKeyParameters sender = new RsaKeyParameters(
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
