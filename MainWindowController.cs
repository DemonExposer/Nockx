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
		bool doReturn = false;
		Thread? timeoutThread = null;
		Thread connectThread = new (async () => {
			try {
				await _webSocket.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None);
				timeoutThread!.Interrupt();
			} catch (ThreadInterruptedException) {
				Console.WriteLine("interrupted");
				doReturn = true;
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
				doReturn = true;
			}
		});

		timeoutThread = new Thread(async () => {
			try {
				await Task.Delay(5000);
				if (connectThread.IsAlive) {
					connectThread.Interrupt();
					Console.WriteLine("websocket timeout");
					return;
				}
			} catch (ThreadInterruptedException) {
				Console.WriteLine("second interrupted");
			}
		});

		connectThread.Start();
		timeoutThread.Start();

		timeoutThread.Join();
		if (doReturn)
			return;

		byte[] modulusStrBytes = Encoding.UTF8.GetBytes(PublicKey.Modulus.ToString(16));
		await _webSocket.SendAsync(modulusStrBytes, WebSocketMessageType.Text, true, CancellationToken.None);
	}

	public void ListenOnWebsocket() {
		byte[] buffer = new byte[1024];

		while (true) {

		}
	}
}
