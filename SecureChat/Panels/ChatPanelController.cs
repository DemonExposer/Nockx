using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using LessAnnoyingHttp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using SecureChat.ClassExtensions;
using SecureChat.ExtendedControls;
using SecureChat.Model;
using SecureChat.Util;
using SecureChat.Windows;

namespace SecureChat.Panels;

public class ChatPanelController {
	public RsaKeyParameters ForeignPublicKey;
	public string ForeignDisplayName = "";

	public readonly RsaKeyParameters PersonalPublicKey;
	private readonly RsaKeyParameters _privateKey;

	private readonly Settings _settings = Settings.GetInstance();
	
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

	public void AddListenersToContextMenu(MessageTextBlock messageTextBlock, IBrush originalBackground, ContextMenu contextMenu) {
		int start = 0, end = 0;

		MenuItem? copyMenuItem = null, deleteMenuItem = null;
		foreach (MenuItem? contextMenuItem in contextMenu.Items) {
			switch (contextMenuItem!.Name) {
				case "CopyMenuItem":
					copyMenuItem = contextMenuItem;
					break;
				case "DeleteMenuItem":
					deleteMenuItem = contextMenuItem;
					break;
			}
		}
		
		// For some reason this solves automatic unselecting of text, not only for copyMenuItem, but for all of them
		copyMenuItem!.PointerMoved += (_, _) => {
			messageTextBlock.TextBlock.SelectionStart = start;
			messageTextBlock.TextBlock.SelectionEnd = end;
		};

		messageTextBlock.TextBlock.ContextFlyout = null;
		messageTextBlock.TextBlock.PointerReleased += (_, args) => {
			if (args.GetCurrentPoint(_context).Properties.PointerUpdateKind != PointerUpdateKind.RightButtonReleased)
				return;

			start = messageTextBlock.TextBlock.SelectionStart;
			end = messageTextBlock.TextBlock.SelectionEnd;
			
			copyMenuItem.IsVisible = messageTextBlock.TextBlock.SelectedText != "";
			deleteMenuItem!.IsVisible = messageTextBlock.Sender == PersonalPublicKey.ToBase64String();
			contextMenu.Open((Control) args.Source!);
		};
		
		messageTextBlock.Border.PointerEntered += (_, _) => messageTextBlock.Border.Background = new SolidColorBrush(Color.Parse("#252525")); 
		messageTextBlock.Border.PointerExited += (_, _) => messageTextBlock.Border.Background = originalBackground;
		messageTextBlock.Border.PointerPressed += (_, args) => {
			if (args.GetCurrentPoint(_context).Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed)
				return;

			copyMenuItem.IsVisible = false;
			deleteMenuItem!.IsVisible = messageTextBlock.Sender == PersonalPublicKey.ToBase64String();
			contextMenu.Open((Control) args.Source!);
		};
	}

	public bool Decrypt(Message message, bool isOwnMessage, out DecryptedMessage? decryptedMessage) {
		DecryptedMessage uncheckedMessage = Cryptography.Decrypt(message, _privateKey, isOwnMessage);
		if (Cryptography.Verify(uncheckedMessage.Body, message.Signature, PersonalPublicKey, ForeignPublicKey, isOwnMessage)) {
			decryptedMessage = uncheckedMessage;
			return true;
		}

		if (!isOwnMessage && ForeignDisplayName != message.SenderDisplayName) {
			ForeignDisplayName = message.SenderDisplayName;
			_context.ChangeDisplayName();
		} else if (isOwnMessage && ForeignDisplayName != message.ReceiverDisplayName) {
			ForeignDisplayName = message.ReceiverDisplayName;
			_context.ChangeDisplayName();
		}

		decryptedMessage = null;
		return false;
	}

	public DecryptedMessage[] GetPastMessages() { // TODO: Add signature to this request
		List<DecryptedMessage> res = [];
		
		string getVariables = $"requestingUser={HttpUtility.UrlEncode(PersonalPublicKey.ToBase64String())}&requestedUser={HttpUtility.UrlEncode(ForeignPublicKey.ToBase64String())}";
		JsonArray messages = JsonNode.Parse(Http.Get($"http://{_settings.IpAddress}:5000/messages?" + getVariables).Body)!.AsArray();
		foreach (JsonNode? messageNode in messages) {
			Message message = Message.Parse(messageNode!.AsObject());
			bool isOwnMessage = Equals(message.Sender, PersonalPublicKey);
			if (!Decrypt(message, isOwnMessage, out DecryptedMessage? decryptedMessage))
				continue; // Just don't add the message if it is not legitimate
			if (!isOwnMessage && ForeignDisplayName != message.SenderDisplayName) {
				ForeignDisplayName = message.SenderDisplayName;
				_context.ChangeDisplayName();
			} else if (isOwnMessage && ForeignDisplayName != message.ReceiverDisplayName) {
				ForeignDisplayName = message.ReceiverDisplayName;
				_context.ChangeDisplayName();
			}
			res.Add(new DecryptedMessage { Id = message.Id, Body = decryptedMessage!.Body, DateTime = DateTime.MinValue, Sender = message.Sender.ToBase64String(), DisplayName = message.SenderDisplayName});
		}

		return res.ToArray();
	}
	
	public long SendMessage(string message) {
		if (_mainWindowModel == null)
			throw new InvalidOperationException("SendMessage may not be called before _mainWindowModel is set, using SetMainWindowModel");
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
			["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};
		
		string bodyString = JsonSerializer.Serialize(body);
		string response = Http.Post($"http://{_settings.IpAddress}:5000/messages", bodyString, [new Header {Name = "Signature", Value = Cryptography.Sign(bodyString, _privateKey)}]).Body;
		return long.Parse(response);
	}

	public void DeleteMessage(long id) {
		JsonObject body = new () {
			["id"] = id,
			["signature"] = Cryptography.Sign(id.ToString(), _privateKey)
		};
		_context.RemoveMessage(id);
		Http.Delete($"http://{_settings.IpAddress}:5000/messages", JsonSerializer.Serialize(body));
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
		Http.Post($"http://{_settings.IpAddress}:5000/makeChatRead", bodyString, [new Header {Name = "Signature", Value = Cryptography.Sign(bodyString, _privateKey)}]);
	}

	public void OnCallButtonClicked(object? sender, EventArgs e) => StartCall();

	private void StartCall() {
		new CallPopupWindow(PersonalPublicKey, ForeignPublicKey).Show(MainWindow.Instance);
	}
}