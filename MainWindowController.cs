using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using SecureChat.util;

namespace SecureChat;

public class MainWindowController {
	public RsaKeyParameters PublicKey;

	private readonly MainWindow _context;
	private readonly ClientWebSocket _webSocket;

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
		} catch (OperationCanceledException) when (cts.IsCancellationRequested) {
			Console.WriteLine("websocket timeout"); // TODO: handle this differently
			return;
		} catch (Exception e) {
			Console.WriteLine(e.ToString());
			return;
		}
	}

	public void ListenOnWebsocket() {
		byte[] buffer = new byte[1024];

		while (true) {

		}
	}
}
