using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Threading;
using LessAnnoyingHttp;
using Nockx.Base;
using Nockx.Base.ClassExtensions;
using Nockx.Base.Util;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using OsNotifications;
using SecureChat.Audio;
using SecureChat.Util;
using SecureChat.Windows;

namespace SecureChat;

public class MainWindowController {
	public CallPopupWindow? CallWindow;
	
	public readonly RsaKeyParameters PublicKey;
	private readonly RsaKeyParameters _privateKey;

	private readonly MainWindow _context;
	private readonly MainWindowModel _model;
	private ClientWebSocket _webSocket;

	private bool _isWebsocketInitialized;
	private bool _isWebsocketReinitializing;
	private bool _isWindowActivated;
	private Timer _keepAliveTimer;
	private long _lastMessageId;

	public MainWindowController(MainWindow context, MainWindowModel model) {
		_context = context;
		_model = model;

		using (StreamReader reader = File.OpenText(Constants.PublicKeyFile)) {
			PemReader pemReader = new (reader);
			PublicKey = (RsaKeyParameters) pemReader.ReadObject();
		}

		using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
			PemReader pemReader = new (reader);
			_privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
		}

		// Load chats from file
		Chats.Show(_context);
		
		// Check if new chats were created while offline
		CheckForNewChats();

		long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		string getVariables = $"requestingUser={HttpUtility.UrlEncode(PublicKey.ToBase64String())}&timestamp={timestamp}";
		Response response = Http.Get($"https://{Settings.GetInstance().Hostname}:5000/messages/lastReceived?{getVariables}", [new Header { Name = "Signature", Value = Cryptography.Sign(timestamp.ToString(), _privateKey) }]);
		_lastMessageId = response.StatusCode == HttpStatusCode.NoContent ? -1 : JsonNode.Parse(response.Body)!["id"]!.GetValue<long>();
		
		_webSocket = new ClientWebSocket();
		_ = InitializeWebsocket(true, false);
	}

	private void CheckForNewChats() {
		string getVariables = $"key={HttpUtility.UrlEncode(PublicKey.ToBase64String())}&timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
		Response response = null!;
		bool hasTimedOut = false;
		try {
			response = Http.Get($"https://{Settings.GetInstance().Hostname}:5000/chats?{getVariables}", [new Header { Name = "Signature", Value = Cryptography.Sign(getVariables, _privateKey) }]);
		} catch (TimeoutException) {
			hasTimedOut = true;
		}

		if (hasTimedOut || !response.IsSuccessful) {
			_context.ShowPopupWindowOnTop(new ErrorPopupWindow($"Could not retrieve chats from server ({Settings.GetInstance().Hostname})"));
			return;
		}

		JsonArray chats = JsonNode.Parse(response.Body)!.AsArray();
		foreach (JsonNode? jsonNode in chats) {
			JsonObject chatObject = jsonNode!.AsObject();
			long chatId = chatObject["id"]!.GetValue<long>();
			if (chatObject["user1"]!["key"]!.GetValue<string>() != PublicKey.ToBase64String()) {
				RsaKeyParameters key = RsaKeyParametersExtension.FromBase64String(chatObject["user1"]!["key"]!.GetValue<string>());
				string name = chatObject["user1"]!["displayName"]!.GetValue<string>();
				
				_context.AddUser(chatId, key, name, false);
				_model.SetChatReadStatus(chatId, chatObject["isRead"]!.GetValue<bool>());
				_model.UpdateName(chatId, name);
			} else {
				string key = chatObject["user2"]!["key"]!.GetValue<string>();
				string name = chatObject["user2"]!["displayName"]!.GetValue<string>();

				_context.AddUser(chatId, RsaKeyParametersExtension.FromBase64String(key), name, false);
				_model.SetChatReadStatus(chatId, chatObject["isRead"]!.GetValue<bool>());
				_model.UpdateName(chatId, name);
			}
		}
	}

	private async Task InitializeWebsocket(bool isCalledFromUI, bool isReinitializing) {
		using CancellationTokenSource cts = new (5000);
		try {
			await _webSocket.ConnectAsync(new Uri($"wss://{Settings.GetInstance().Hostname}:5000/ws"), cts.Token);

			string keyBase64 = PublicKey.ToBase64String();
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

			// Retrieve lost messages in case of reinitialization due to connection loss
			if (isReinitializing) {
				timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				string getVariables = $"requestingUser={HttpUtility.UrlEncode(keyBase64)}&timestamp={timestamp}&lastMessageId={_lastMessageId}";
				Response response = Http.Get($"https://{Settings.GetInstance().Hostname}:5000/messages/all?{getVariables}", [new Header { Name = "Signature", Value = Cryptography.Sign(timestamp.ToString(), _privateKey) }]);
				JsonArray messagesJson = JsonNode.Parse(response.Body)!.AsArray();
				RsaKeyParameters? foreignKey = _context.GetCurrentChatIdentity();
				List<long> chatIds = messagesJson.DistinctBy(elem => elem!["chatId"]!.GetValue<long>()).Select(elem => elem!["chatId"]!.GetValue<long>()).ToList();
				
				// Flag chats as unread
				Dispatcher.UIThread.InvokeAsync(() => {
					foreach (long chatId in chatIds)
						_model.SetChatReadStatus(chatId, false);
				});
				
				if (chatIds.Count > 0)
					Sounds.Notification.Play();
				
				// Update current chat
				if (foreignKey != null) {
					DecryptedMessage[] messages = _context.ChatPanel.RetrieveUnretrievedMessages();
					if (messages.Length > 0) {
						if (chatIds.Count == 0)
							Sounds.Notification.Play();
						
						if (!_isWindowActivated) {
							try {
								Notifications.ShowNotification(messages[^1].DisplayName, messages[^1].Body);
							} catch (PlatformNotSupportedException e) {
								Console.WriteLine(e);
							}
						}
					}
				}
			}
		} catch (OperationCanceledException) when (cts.IsCancellationRequested) {
			if (!isCalledFromUI)
				return;
			_context.ShowPopupWindowOnTop(new ErrorPopupWindow($"Websocket timeout ({Settings.GetInstance().Hostname})"));
		} catch (WebSocketException) {
			if (!isCalledFromUI)
				return;
			_context.ShowPopupWindowOnTop(new ErrorPopupWindow($"Websocket could not be connected ({Settings.GetInstance().Hostname})"));
		} catch (Exception e) {
			Console.WriteLine(e);
		}
	}

	private async Task ReinitializeWebsocket() {
		_isWebsocketReinitializing = true;
		if (_isWebsocketInitialized) {
			_isWebsocketInitialized = false;
			await _keepAliveTimer.DisposeAsync();
			_webSocket.Abort();
			_webSocket.Dispose();
		} else {
			_isWebsocketInitialized = false;
		}

		_webSocket = new ClientWebSocket();

		await InitializeWebsocket(false, true);

		_isWebsocketReinitializing = false;
	}

	public async Task ListenOnWebsocket() {
		try {
			while (!_isWebsocketInitialized)
				await Task.Delay(1000);

			const int arrSize = 1024;
			byte[] buffer = new byte[arrSize];

			Timer timer = new (_ => {
				if (_webSocket.State != WebSocketState.Open && !_isWebsocketReinitializing)
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

				/*
				 * TODO: make sure that no feedback is given on whether ciphertext is correct or not. Let it fail silently if incorrect.
				 * Also make sure that any time plaintext is received, there is a signature which matches the plaintext, NOT JUST THE TIMESTAMP.
				 * Possibly check this in other sections of the code too
				 */
				Dictionary<string, Action<JsonObject>> actions = new () {
					["add"] = message => {
						Message parsedMessage = Message.Parse(message);
						bool isMessageAdded = AddMessage(parsedMessage);

						if (isMessageAdded) {
							_lastMessageId = parsedMessage.Id;
							Sounds.Notification.Play();
						} else if (TryDecryptMessage(parsedMessage, out DecryptedMessage? decryptedMessage)) {
							_lastMessageId = decryptedMessage!.Id;
							Sounds.Notification.Play();
							if (!_isWindowActivated) {
								try {
									Notifications.ShowNotification(decryptedMessage.DisplayName, decryptedMessage.Body);
								} catch (PlatformNotSupportedException e) {
									Console.WriteLine(e);
								}
							}
						}
					},
					["delete"] = DeleteMessage,
					["callAccepted"] = message => { // TODO: test this
						string senderKeyBase64 = message["sender"]!.GetValue<string>();
						long timestamp = message["timestamp"]!.GetValue<long>();
						string signature = message["signature"]!.GetValue<string>();
						
						if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timestamp > 10000) // Unable to verify authenticity, because of possible replay attack
							return;
						
						// TODO: sign the unencrypted key in the signature instead
						if (!Cryptography.Verify(senderKeyBase64 + timestamp, signature, null, RsaKeyParametersExtension.FromBase64String(senderKeyBase64), false))
							return;
						
						if (_context.CallPopupWindow != null && _context.CallPopupWindow.ForeignKey.ToBase64String() == senderKeyBase64)
							_context.CallPopupWindow.OnOtherPersonJoined();
					},
					["callStart"] = message => {
						Sounds.Ringtone.Repeat();

						string senderKeyBase64 = message["sender"]!["key"]!.GetValue<string>();
						string displayName = message["sender"]!["displayName"]!.GetValue<string>();
						long timestamp = message["timestamp"]!.GetValue<long>();
						string signature = message["signature"]!.GetValue<string>();

						if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timestamp > 10000) // Unable to verify authenticity, because of possible replay attack
							return;
						
						// TODO: check whether caller is a friend and sign the unencrypted key in the signature instead
						if (!Cryptography.Verify(senderKeyBase64 + timestamp, signature, null, RsaKeyParametersExtension.FromBase64String(senderKeyBase64), false))
							return;
						
						ShowCallPrompt(RsaKeyParametersExtension.FromBase64String(senderKeyBase64), displayName, timestamp);
					},
					["callClose"] = message => {
						if (CallWindow == null)
							return;
						
						if (message["sender"]!.GetValue<string>() == CallWindow.ForeignKey.ToBase64String())
							CallWindow.Close();
					},
					["privateKeyRequest"] = message => {
						string senderKeyBase64 = message["sender"]!.GetValue<string>();
						RsaKeyParameters foreignPublicKey = RsaKeyParametersExtension.FromBase64String(senderKeyBase64);
						long timestamp = message["timestamp"]!.GetValue<long>();
						string signature = message["signature"]!.GetValue<string>();

						if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timestamp > 10000) // Unable to verify authenticity, because of possible replay attack
							return;

						if (!Cryptography.Verify(timestamp.ToString(), signature, null, foreignPublicKey, false))
							return;

						byte[] privateKeyBytes = PrivateKeyInfoFactory.CreatePrivateKeyInfo(_privateKey).GetEncoded();
						byte[] aesKey = Cryptography.GenerateAesKey();
						byte[] cipherBytes = Cryptography.EncryptWithAes(privateKeyBytes, privateKeyBytes.Length, aesKey);
						byte[] encryptedKey = Cryptography.EncryptAesKey(aesKey, foreignPublicKey);

						byte[] qrData = new byte[Cryptography.AesKeyLength + cipherBytes.Length];
						Buffer.BlockCopy(encryptedKey, 0, qrData, 0, Cryptography.AesKeyLength);
						Buffer.BlockCopy(cipherBytes, 0, qrData, Cryptography.AesKeyLength, cipherBytes.Length);

						_context.UserInfoPanel.ShowQrCodeConfirmationWindow(senderKeyBase64, qrData);
					},
					["addFriendRequest"] = friendRequest => {
						FriendRequest request = new () {
							Accepted = false,
							SenderKey = friendRequest["sender"]!["key"]!.GetValue<string>(),
							SenderName = friendRequest["sender"]!["displayName"]!.GetValue<string>(),
							ReceiverKey = PublicKey.ToBase64String()
						};
						
						_context.AddFriendRequest(request);
						
						Sounds.Notification.Play();
						if (!_isWindowActivated) {
							try {
								Notifications.ShowNotification("Friend request", $"{request.SenderName} has sent you a friend request");
							} catch (PlatformNotSupportedException e) {
								Console.WriteLine(e);
							}
						}
					},
					["removeFriendRequest"] = friendRequest => { // TODO: possibly add verification here, so that person B can't remove person A's friend request. The problem is that the server can spoof this anyway at all times even with verifications
						_context.RemoveFriendRequest(friendRequest["sender"]!.GetValue<string>());
					}
				};

				JsonObject messageJson = JsonNode.Parse(Encoding.UTF8.GetString(bytes.ToArray()))!.AsObject();
				try {
					actions[messageJson["action"]!.GetValue<string>()](messageJson);
				} catch (Exception e) {
					Console.WriteLine(e);
				}
			}
		} catch (Exception e) {
			Console.WriteLine(e);
		}
	}

	private bool AddMessage(Message message) {
		_context.AddUser(message.ChatId, message.Sender, message.SenderDisplayName, false);
		_model.UpdateName(message.ChatId, message.SenderDisplayName);

		RsaKeyParameters? currentChatForeignPublicKey = _context.GetCurrentChatIdentity();
		if (currentChatForeignPublicKey == null || !currentChatForeignPublicKey.Equals(message.Sender)) {
			if (!message.IsRead)
				_model.SetChatReadStatus(message.ChatId, false);
			
			return false;
		}

		_context.ChatPanel.DecryptAndAddMessage(message);
		return true;
	}

	private void DeleteMessage(JsonObject messageJson) {
		RsaKeyParameters sender = RsaKeyParametersExtension.FromBase64String(messageJson["sender"]!["key"]!.GetValue<string>());
		
		RsaKeyParameters? currentChatForeignPublicKey = _context.GetCurrentChatIdentity();
		if (currentChatForeignPublicKey == null || !currentChatForeignPublicKey.Equals(sender))
			return;

		_context.ChatPanel.RemoveMessage(messageJson["id"]!.GetValue<long>());
	}

	private void ShowCallPrompt(RsaKeyParameters foreignPublicKey, string foreignDisplayName, long timestamp) {
		new CallRequestPopupWindow(PublicKey, foreignPublicKey, foreignDisplayName, timestamp).Show(_context);
	}

	public void SendFriendRequest(RsaKeyParameters publicKey) {
		JsonObject body = new () {
			["sender"] = new JsonObject {
				["key"] = PublicKey.ToBase64String(),
				["displayName"] = _model.DisplayName
			},
			["receiver"] = new JsonObject {
				["key"] = publicKey.ToBase64String(),
			},
			["accepted"] = false,
			["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};
		string json = JsonSerializer.Serialize(body);
		Response response = Http.Post($"https://{Settings.GetInstance().Hostname}:5000/friends", json, [new Header { Name = "Signature", Value = Cryptography.Sign(json, _privateKey) }]);
		if (!response.IsSuccessful) {
			Console.WriteLine(response.Body);
			_context.ShowPopupWindowOnTop(new ErrorPopupWindow($"Could not add friend on server ({Settings.GetInstance().Hostname})"));
			return;
		}
	}

	public void OnWindowActivated(object? sender, EventArgs e) => _isWindowActivated = true;

	public void OnWindowDeactivated(object? sender, EventArgs e) => _isWindowActivated = false;

	public bool TryDecryptMessage(Message message, out DecryptedMessage? decryptedMessage) {
		DecryptedMessage uncheckedMessage = Cryptography.Decrypt(message, _privateKey, false);
		if (Cryptography.Verify(uncheckedMessage.Body + uncheckedMessage.Timestamp, message.Signature, PublicKey, message.Sender, false)) {
			decryptedMessage = uncheckedMessage;
			return true;
		}

		decryptedMessage = null;
		return false;
	}
}
