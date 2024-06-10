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
	private readonly ClientWebSocket _webSocket;

	private bool isWebsocketInitialized = false;

	public MainWindowController(MainWindow context) {
		_context = context;

		using StreamReader reader = File.OpenText(Constants.PublicKeyFile);
		PemReader pemReader = new (reader);
		PublicKey = (RsaKeyParameters) pemReader.ReadObject();

		_webSocket = new ClientWebSocket();
		_ = InitializeWebsocket();
	}

	public async Task InitializeWebsocket() {
		using CancellationTokenSource cts = new (5000);
		try {
			await _webSocket.ConnectAsync(new Uri("ws://localhost:5000/ws"), cts.Token);

			byte[] modulusStrBytes = Encoding.UTF8.GetBytes(PublicKey.Modulus.ToString(16));
			await _webSocket.SendAsync(modulusStrBytes, WebSocketMessageType.Text, true, CancellationToken.None);
			isWebsocketInitialized = true;
		} catch (OperationCanceledException) when (cts.IsCancellationRequested) {
			Console.WriteLine("websocket timeout"); // TODO: handle this differently
			return;
		} catch (Exception e) {
			Console.WriteLine(e.ToString());
			return;
		}
	}

	public async Task ListenOnWebsocket() {
		while (!isWebsocketInitialized)
			await Task.Delay(1000);

		int arrSize = 1024;
		byte[] buffer = new byte[arrSize];

		while (true) {
			List<byte> bytes = new ();
			WebSocketReceiveResult result;
			do {
				result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
				bytes.AddRange(buffer[..result.Count]);
			} while (result.Count == arrSize);

			JsonObject messageJson = JsonNode.Parse(Encoding.UTF8.GetString(bytes.ToArray()))!.AsObject();
			Message message = new () {
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
