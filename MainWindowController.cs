using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using SecureChat.util;
using SecureChat.windows;

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

		LoadChats();

		_webSocket = new ClientWebSocket();
		_ = InitializeWebsocket();
	}

	private void LoadChats() {
		try {
			JsonArray chats = JsonNode.Parse(File.ReadAllText(Constants.ChatsFile))!.AsArray();
			foreach (JsonNode? jsonNode in chats) {
				JsonObject chat = jsonNode!.AsObject();
				RsaKeyParameters publicKey = new (
					false,
					new BigInteger(chat["publicKey"]!["modulus"]!.GetValue<string>(), 16),
					new BigInteger(chat["publicKey"]!["exponent"]!.GetValue<string>(), 16)
				);
				string name = chat["name"]!.GetValue<string>();
				_context.AddUser(publicKey, name);
			}
		} catch (JsonException) {
			_context.ShowPopupWindowOnTop(new ErrorPopupWindow($"{Constants.ChatsFile} not found or corrupted"));
		}
	}

	public void AddChatToFile(RsaKeyParameters publicKey, string name) {
		try {
			JsonArray chats = JsonNode.Parse(File.ReadAllText(Constants.ChatsFile))!.AsArray();
			chats.Add(new JsonObject {
				["publicKey"] = new JsonObject {
					["modulus"] = publicKey.Modulus.ToString(16),
					["exponent"] = publicKey.Exponent.ToString(16)
				},
				["name"] = name
			});
			File.WriteAllText(Constants.ChatsFile, JsonSerializer.Serialize(chats, new JsonSerializerOptions { WriteIndented = true }));
		} catch (JsonException) {
			_context.ShowPopupWindowOnTop(new ErrorPopupWindow($"{Constants.ChatsFile} not found or corrupted"));
		}
	}

	private async Task InitializeWebsocket() {
		using CancellationTokenSource cts = new (5000);
		try {
			await _webSocket.ConnectAsync(new Uri("ws://localhost:5000/ws"), cts.Token);

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

	public async Task ListenOnWebsocket() {
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

			JsonObject messageJson = JsonNode.Parse(Encoding.UTF8.GetString(bytes.ToArray()))!.AsObject();
			Message message = new () {
				Id = messageJson["id"]!.GetValue<long>(),
				Body = messageJson["text"]!.GetValue<string>(),
				Sender = new RsaKeyParameters(false, new BigInteger(messageJson["sender"]!["modulus"]!.GetValue<string>(), 16), new BigInteger(messageJson["sender"]!["exponent"]!.GetValue<string>(), 16)),
				ReceiverEncryptedKey = messageJson["receiverEncryptedKey"]!.GetValue<string>(),
				Signature = messageJson["signature"]!.GetValue<string>()
			};

			RsaKeyParameters? currentChatForeignPublicKey = _context.GetCurrentChatIdentity();
			if (currentChatForeignPublicKey == null || !currentChatForeignPublicKey.Equals(message.Sender))
				return;

			_context.ChatPanel.DecryptAndAddMessage(message);
		}
	}
}
