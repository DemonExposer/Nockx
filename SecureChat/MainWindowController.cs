using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using LessAnnoyingHttp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using SecureChat.Audio;
using SecureChat.ClassExtensions;
using SecureChat.Util;
using SecureChat.Windows;

namespace SecureChat;

public class MainWindowController {
	public CallPopupWindow? CallWindow;
	
	private readonly RsaKeyParameters _publicKey, _privateKey;

	private readonly MainWindow _context;
	private readonly MainWindowModel _model;
	private ClientWebSocket _webSocket;

	private bool _isWebsocketInitialized;
	private Timer _keepAliveTimer;

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
		string getVariables = $"key={HttpUtility.UrlEncode(_publicKey.ToBase64String())}&timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
		Response response = Http.Get($"https://{Settings.GetInstance().Hostname}:5000/chats?{getVariables}", [new Header { Name = "Signature", Value = Cryptography.Sign(getVariables, _privateKey) }]);
		if (!response.IsSuccessful) {
			_context.ShowPopupWindowOnTop(new ErrorPopupWindow($"Could not retrieve chats from server ({Settings.GetInstance().Hostname})"));
			return;
		}

		JsonArray chats = JsonNode.Parse(response.Body)!.AsArray();
		foreach (JsonNode? jsonNode in chats) {
			JsonObject chatObject = jsonNode!.AsObject();
			if (chatObject["user1"]!["key"]!.GetValue<string>() != _publicKey.ToBase64String()) {
				_context.AddUser(RsaKeyParametersExtension.FromBase64String(chatObject["user1"]!["key"]!.GetValue<string>()), chatObject["user1"]!["displayName"]!.GetValue<string>(), false);
				_model.SetChatReadStatus(chatObject["user1"]!["key"]!.GetValue<string>(), chatObject["isRead"]!.GetValue<bool>());
			} else {
				_context.AddUser(RsaKeyParametersExtension.FromBase64String(chatObject["user2"]!["key"]!.GetValue<string>()), chatObject["user2"]!["displayName"]!.GetValue<string>(), false);
				_model.SetChatReadStatus(chatObject["user2"]!["key"]!.GetValue<string>(), chatObject["isRead"]!.GetValue<bool>());
			}
		}
	}

	private async Task InitializeWebsocket(bool isCalledFromUI) {
		using CancellationTokenSource cts = new (5000);
		try {
			await _webSocket.ConnectAsync(new Uri($"wss://{Settings.GetInstance().Hostname}:5000/ws"), cts.Token);

			string keyBase64 = _publicKey.ToBase64String();
			long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			JsonObject message = new () {
				["key"] = keyBase64,
				["timestamp"] = timestamp,
				["signature"] = Cryptography.Sign(keyBase64 + timestamp, _privateKey)
			};
			await _webSocket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)), WebSocketMessageType.Text, true, CancellationToken.None);

			_keepAliveTimer = new Timer(_ => { // Send ping
				try {
					_webSocket.SendAsync((byte[]) [0x05], WebSocketMessageType.Binary, true, CancellationToken.None).Wait();
				} catch (Exception) {
					// ignored
				}
			}, null, 0, 5000);
			
			_isWebsocketInitialized = true;
		} catch (OperationCanceledException) when (cts.IsCancellationRequested) {
			if (!isCalledFromUI)
				return;
			_context.ShowPopupWindowOnTop(new ErrorPopupWindow($"Websocket timeout ({Settings.GetInstance().Hostname})"));
		} catch (WebSocketException) {
			if (!isCalledFromUI)
				return;
			_context.ShowPopupWindowOnTop(new ErrorPopupWindow($"Websocket could not be connected ({Settings.GetInstance().Hostname})"));
		} catch (Exception e) {
			Console.WriteLine(e.ToString());
		}
	}

	private async Task ReinitializeWebsocket() { // TODO: retrieve all messages after reinitialization, so that missed messages are loaded
		if (_isWebsocketInitialized) {
			_isWebsocketInitialized = false;
			await _keepAliveTimer.DisposeAsync();
			_webSocket.Abort();
			_webSocket.Dispose();
		} else {
			_isWebsocketInitialized = false;
		}

		_webSocket = new ClientWebSocket();

		await InitializeWebsocket(false);
	}

	public async Task ListenOnWebsocket() {
		try {
			while (!_isWebsocketInitialized)
				await Task.Delay(1000);

			const int arrSize = 1024;
			byte[] buffer = new byte[arrSize];

			Timer timer = new (_ => {
				if (_webSocket.State != WebSocketState.Open)
					ReinitializeWebsocket().Wait();
			}, null, 0, 5000);

			CancellationTokenSource websocketAutoCloseTokenSource = new ();
			Task.Run(async () => {
				while (true) try {
					await Task.Delay(10000, websocketAutoCloseTokenSource.Token);
					if (_webSocket.State == WebSocketState.Open)
						await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "timeout", CancellationToken.None);
				} catch (Exception) {
					websocketAutoCloseTokenSource.Dispose();
					websocketAutoCloseTokenSource = new CancellationTokenSource();
				}
			});
			
			while (true) {
				bool doContinue = false;
				List<byte> bytes = [];
				WebSocketReceiveResult result;
				try {
					if (!_isWebsocketInitialized)
						throw new WebSocketException();

					do {
						result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);

						if (result is { MessageType: WebSocketMessageType.Binary, Count: 1 } && buffer[0] == 0x06) {
							await websocketAutoCloseTokenSource.CancelAsync();
							doContinue = true;
							break;
						}
						bytes.AddRange(buffer[..result.Count]);
					} while (result.Count == arrSize);
				} catch (Exception e) {
					Console.WriteLine(e.ToString());
					await Task.Delay(500);
					continue;
				}
				
				if (doContinue)
					continue;

				Dictionary<string, Action<JsonObject>> actions = new () {
					["add"] = message => {
						Sounds.Notification.Play();
						AddMessage(message);
					},
					["delete"] = DeleteMessage,
					["callStart"] = message => {
						Sounds.Ringtone.Repeat();

						string senderKeyBase64 = message["sender"]!.GetValue<string>();
						long timestamp = message["timestamp"]!.GetValue<long>();
						string signature = message["signature"]!.GetValue<string>();

						if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timestamp > 10000) // Unable to verify authenticity, because of possible replay attack
							return;
						
						// TODO: check whether caller is a friend
						if (!Cryptography.Verify(senderKeyBase64 + timestamp, signature, null, RsaKeyParametersExtension.FromBase64String(senderKeyBase64), false))
							return;
						
						ShowCallPrompt(RsaKeyParametersExtension.FromBase64String(senderKeyBase64), timestamp);
					},
					["callClose"] = message => {
						if (CallWindow == null)
							return;
						
						if (message["sender"]!.GetValue<string>() == CallWindow.ForeignKey.ToBase64String())
							CallWindow.Close();
					}
				};

				JsonObject messageJson = JsonNode.Parse(Encoding.UTF8.GetString(bytes.ToArray()))!.AsObject();
				actions[messageJson["action"]!.GetValue<string>()](messageJson);
			}
		} catch (Exception e) {
			Console.WriteLine(e);
		}
	}

	private void AddMessage(JsonObject messageJson) {
		Message message = Message.Parse(messageJson);
		
		// Add new chat if receiver does not yet have a chat with sender
		if (!Chats.ChatExists(message.Sender))
			Chats.Add(message.Sender);
		
		_context.AddUser(message.Sender, message.SenderDisplayName, false);

		RsaKeyParameters? currentChatForeignPublicKey = _context.GetCurrentChatIdentity();
		if (currentChatForeignPublicKey == null || !currentChatForeignPublicKey.Equals(message.Sender)) {
			if (!message.IsRead)
				_model.SetChatReadStatus(message.Sender.ToBase64String(), false);
			
			return;
		}

		_context.ChatPanel.DecryptAndAddMessage(message);
	}

	private void DeleteMessage(JsonObject messageJson) {
		RsaKeyParameters sender = RsaKeyParametersExtension.FromBase64String(messageJson["sender"]!["key"]!.GetValue<string>());
		
		RsaKeyParameters? currentChatForeignPublicKey = _context.GetCurrentChatIdentity();
		if (currentChatForeignPublicKey == null || !currentChatForeignPublicKey.Equals(sender))
			return;

		_context.ChatPanel.RemoveMessage(messageJson["id"]!.GetValue<long>());
	}

	private void ShowCallPrompt(RsaKeyParameters foreignPublicKey, long timestamp) {
		new CallRequestPopupWindow(_publicKey, foreignPublicKey, timestamp).Show(_context);
	}

	public void SendFriendRequest(RsaKeyParameters publicKey) {
		JsonObject body = new () {
			["sender"] = new JsonObject {
				["key"] = _publicKey.ToBase64String(),
				["displayName"] = _model.DisplayName
			},
			["receiver"] = new JsonObject {
				["key"] = publicKey.ToBase64String(),
				["displayName"] = ""
			},
			["accepted"] = false
		};
		string json = JsonSerializer.Serialize(body);
		Response response = Http.Post($"https://{Settings.GetInstance().Hostname}:5000/friends", json, [new Header { Name = "Signature", Value = Cryptography.Sign(json, _privateKey) }]);
		if (!response.IsSuccessful) {
			Console.WriteLine(response.Body);
			_context.ShowPopupWindowOnTop(new ErrorPopupWindow($"Could not add friend on server ({Settings.GetInstance().Hostname})"));
			return;
		}
	}
}
