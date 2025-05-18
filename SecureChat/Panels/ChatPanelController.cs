using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using LessAnnoyingHttp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using SecureChat.ClassExtensions;
using SecureChat.Model;
using SecureChat.Util;
using SecureChat.Windows;

namespace SecureChat.Panels;

public class ChatPanelController {
	public RsaKeyParameters ForeignPublicKey;
	public string ForeignDisplayName = "";

	public readonly RsaKeyParameters PersonalPublicKey;
	private readonly RsaKeyParameters _privateKey;
	
	private readonly ChatPanel _context;
	
	private MainWindowModel? _mainWindowModel;

	public ChatPanelController(ChatPanel context) {
		_context = context;
		
		using (StreamReader reader = File.OpenText(Constants.PublicKeyFile)) {
			PemReader pemReader = new (reader);
			PersonalPublicKey = (RsaKeyParameters) pemReader.ReadObject();
		}
		
		using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
			PemReader pemReader = new (reader);
			_privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
		}
	}
	
	public void SetMainWindowModel(MainWindowModel mainWindowModel) {
		if (_mainWindowModel != null)
			return;
		
		_mainWindowModel = mainWindowModel;
	}

	public bool Decrypt(Message message, bool isOwnMessage, out DecryptedMessage? decryptedMessage) {
		DecryptedMessage uncheckedMessage = Cryptography.Decrypt(message, _privateKey, isOwnMessage);
		if (!Cryptography.Verify(uncheckedMessage.Body + uncheckedMessage.Timestamp, message.Signature, PersonalPublicKey, ForeignPublicKey, isOwnMessage)) {
			decryptedMessage = null;
			return false;
		}

		decryptedMessage = uncheckedMessage;
		return true;
	}

	public DecryptedMessage[] GetPastMessages(long lastMessageId = -1) {
		List<DecryptedMessage> res = [];
		
		long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		string getVariables = $"requestingUser={HttpUtility.UrlEncode(PersonalPublicKey.ToBase64String())}&requestedUser={HttpUtility.UrlEncode(ForeignPublicKey.ToBase64String())}&timestamp={timestamp}";
		if (lastMessageId != -1)
			getVariables += $"&lastMessageId={lastMessageId}";
		
		JsonArray messages = JsonNode.Parse(Http.Get($"https://{Settings.GetInstance().Hostname}:5000/messages?" + getVariables, [new Header { Name = "Signature", Value = Cryptography.Sign(timestamp.ToString(), _privateKey) }]).Body)!.AsArray();
		foreach (JsonNode? messageNode in messages) {
			Message message = Message.Parse(messageNode!.AsObject());
			bool isOwnMessage = Equals(message.Sender, PersonalPublicKey);
			if (!Decrypt(message, isOwnMessage, out DecryptedMessage? decryptedMessage))
				continue; // Just don't add the message if it is not legitimate
			
			res.Add(decryptedMessage!);
		}

		return res.ToArray();
	}
	
	public long SendMessage(string message) {
		if (_mainWindowModel == null)
			throw new InvalidOperationException("SendMessage may not be called before _mainWindowModel is set using SetMainWindowModel");
		Message encryptedMessage = Cryptography.Encrypt(message, PersonalPublicKey, ForeignPublicKey, _privateKey);
		JsonObject body = new () {
			["sender"] = new JsonObject {
				["key"] = PersonalPublicKey.ToBase64String(),
				["displayName"] = _mainWindowModel.DisplayName
			},
			["receiver"] = new JsonObject {
				["key"] = ForeignPublicKey.ToBase64String(),
				["displayName"] = ForeignDisplayName
			},
			["text"] = encryptedMessage.Body,
			["senderEncryptedKey"] = encryptedMessage.SenderEncryptedKey,
			["receiverEncryptedKey"] = encryptedMessage.ReceiverEncryptedKey,
			["signature"] = encryptedMessage.Signature,
			["timestamp"] = encryptedMessage.Timestamp
		};
		
		string bodyString = JsonSerializer.Serialize(body);
		string response = Http.Post($"https://{Settings.GetInstance().Hostname}:5000/messages", bodyString, [new Header { Name = "Signature", Value = Cryptography.Sign(bodyString, _privateKey) }]).Body;
		return long.Parse(response);
	}

	public void DeleteMessage(long id) {
		JsonObject body = new () {
			["id"] = id,
			["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};
		_context.RemoveMessage(id);
		string bodyString = JsonSerializer.Serialize(body);
		Http.Delete($"https://{Settings.GetInstance().Hostname}:5000/messages", bodyString, [new Header { Name = "Signature", Value = Cryptography.Sign(bodyString, _privateKey) }]);
	}

	public void SetChatRead() {
		JsonObject body = new () {
			["receiver"] = new JsonObject {
				["key"] = PersonalPublicKey.ToBase64String(),
				["displayName"] = ""
			},
			["sender"] = new JsonObject {
				["key"] = ForeignPublicKey.ToBase64String(),
				["displayName"] = ""
			},
			["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};
		
		string bodyString = JsonSerializer.Serialize(body);
		Http.Post($"https://{Settings.GetInstance().Hostname}:5000/makeChatRead", bodyString, [new Header { Name = "Signature", Value = Cryptography.Sign(bodyString, _privateKey) }]);
	}

	public void OnCallButtonClicked(object? sender, EventArgs e) => StartCall();

	private void StartCall() {
		MainWindow.Instance.CallPopupWindow = new CallPopupWindow(PersonalPublicKey, ForeignPublicKey);
		MainWindow.Instance.CallPopupWindow.Show(MainWindow.Instance);
	}
}