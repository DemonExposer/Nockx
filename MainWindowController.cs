using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using HarfBuzzSharp;
using Org.BouncyCastle.Crypto.Parameters;
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

			byte[] buffer = new byte[1024];

			// This code is duplicate with the code in ListenOnWebsocket, this is temporary
			List<byte> bytes = new ();
			WebSocketReceiveResult result;
			do {
				result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
				bytes.AddRange(buffer[..result.Count]);
			} while (result.Count == 1024);

			File.WriteAllText("out.txt", Encoding.UTF8.GetString(bytes.ToArray()));
		} catch (OperationCanceledException) when (cts.IsCancellationRequested) {
			Console.WriteLine("websocket timeout"); // TODO: handle this differently
			return;
		} catch (Exception e) {
			Console.WriteLine(e.ToString());
			return;
		}
	}

	public async void ListenOnWebsocket() {
		while (!isWebsocketInitialized) {
			await Task.Delay(1000);
		}

		byte[] buffer = new byte[1024];

		while (true) {
			List<byte> bytes = new ();
			WebSocketReceiveResult result;
			do {
				result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
				bytes.AddRange(buffer[..result.Count]);
			} while (result.Count == 1024);
			
			File.WriteAllText("out.txt", Encoding.UTF8.GetString(bytes.ToArray()));
		}
	}
}
