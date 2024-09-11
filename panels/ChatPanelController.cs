using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using SecureChat.model;
using SecureChat.util;

namespace SecureChat.panels;

public class ChatPanelController {
	public RsaKeyParameters ForeignPublicKey;

	public readonly RsaKeyParameters PersonalPublicKey;
	private readonly RsaKeyParameters _privateKey;

	private readonly Settings _settings = Settings.GetInstance();
	
	private readonly ChatPanel _context;

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
			deleteMenuItem!.IsVisible = messageTextBlock.Sender == PersonalPublicKey.Modulus.ToString(16);
			contextMenu.Open((Control) args.Source!);
		};
		
		messageTextBlock.Border.PointerEntered += (_, _) => messageTextBlock.Border.Background = new SolidColorBrush(Color.Parse("#252525")); 
		messageTextBlock.Border.PointerExited += (_, _) => messageTextBlock.Border.Background = originalBackground;
		messageTextBlock.Border.PointerPressed += (_, args) => {
			if (args.GetCurrentPoint(_context).Properties.PointerUpdateKind != PointerUpdateKind.RightButtonPressed)
				return;

			copyMenuItem.IsVisible = false;
			deleteMenuItem!.IsVisible = messageTextBlock.Sender == PersonalPublicKey.Modulus.ToString(16);
			contextMenu.Open((Control) args.Source!);
		};
	}
	
	private string Sign(string text) {
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		ISigner signer = new RsaDigestSigner(new Sha256Digest());
		signer.Init(true, _privateKey);
		
		signer.BlockUpdate(bytes, 0, bytes.Length);
		byte[] signature = signer.GenerateSignature();

		return Convert.ToBase64String(signature);
	}

	private bool Verify(string text, string signature, bool isOwnMessage) {
		byte[] textBytes = Encoding.UTF8.GetBytes(text);
		byte[] signatureBytes = Convert.FromBase64String(signature);

		ISigner verifier = new RsaDigestSigner(new Sha256Digest());
		verifier.Init(false, isOwnMessage ? PersonalPublicKey : ForeignPublicKey);
		
		verifier.BlockUpdate(textBytes, 0, textBytes.Length);

		return verifier.VerifySignature(signatureBytes);
	}

	private Message Encrypt(string inputText) {
		// Encrypt using AES
		CipherKeyGenerator aesKeyGen = new ();
		aesKeyGen.Init(new KeyGenerationParameters(new SecureRandom(), 256));
		byte[] aesKey = aesKeyGen.GenerateKey();

		AesEngine aesEngine = new ();
		PaddedBufferedBlockCipher cipher = new (new CbcBlockCipher(aesEngine), new Pkcs7Padding());
		cipher.Init(true, new KeyParameter(aesKey));
		
		byte[] plainBytes = Encoding.UTF8.GetBytes(inputText);
		byte[] cipherBytes = new byte[cipher.GetOutputSize(plainBytes.Length)];
		int length = cipher.ProcessBytes(plainBytes, 0, plainBytes.Length, cipherBytes, 0);
		length += cipher.DoFinal(cipherBytes, length);
		
		// Encrypt the AES key using RSA
		OaepEncoding rsaEngine = new (new RsaEngine());
		rsaEngine.Init(true, PersonalPublicKey);
		byte[] personalEncryptedKey = rsaEngine.ProcessBlock(aesKey, 0, aesKey.Length);
		
		rsaEngine.Init(true, ForeignPublicKey);
		byte[] foreignEncryptedKey = rsaEngine.ProcessBlock(aesKey, 0, aesKey.Length);

		return new Message {
			Body = Convert.ToBase64String(cipherBytes),
			SenderEncryptedKey = Convert.ToBase64String(personalEncryptedKey),
			ReceiverEncryptedKey = Convert.ToBase64String(foreignEncryptedKey),
			Signature = Sign(inputText),
			Receiver = ForeignPublicKey,
			Sender = PersonalPublicKey
		};
	}

	public DecryptedMessage Decrypt(Message message, bool isOwnMessage) {
		// Decrypt the AES key using RSA
		byte[] aesKeyEncrypted = Convert.FromBase64String(isOwnMessage ? message.SenderEncryptedKey : message.ReceiverEncryptedKey);
		OaepEncoding rsaEngine = new (new RsaEngine());
		rsaEngine.Init(false, _privateKey);
		
		byte[] aesKey = rsaEngine.ProcessBlock(aesKeyEncrypted, 0, aesKeyEncrypted.Length);
		
		// Decrypt the message using AES
		AesEngine aesEngine = new ();
		PaddedBufferedBlockCipher cipher = new (new CbcBlockCipher(aesEngine), new Pkcs7Padding());
		cipher.Init(false, new KeyParameter(aesKey));

		byte[] encryptedBodyBytes = Convert.FromBase64String(message.Body);
		byte[] plainBytes = new byte[cipher.GetOutputSize(encryptedBodyBytes.Length)];
		int length = cipher.ProcessBytes(encryptedBodyBytes, 0, encryptedBodyBytes.Length, plainBytes, 0);
		length += cipher.DoFinal(plainBytes, length);

		string body = Encoding.UTF8.GetString(plainBytes, 0, length);
		return new DecryptedMessage { Id = message.Id, Body = body, Sender = message.Sender.Modulus.ToString(16), DateTime = DateTime.MinValue};
	}

	public DecryptedMessage[] GetPastMessages() { // TODO: Add signature to this request
		List<DecryptedMessage> res = new ();
		
		string getVariables = $"requestingUserModulus={PersonalPublicKey.Modulus.ToString(16)}&requestingUserExponent={PersonalPublicKey.Exponent.ToString(16)}&requestedUserModulus={ForeignPublicKey.Modulus.ToString(16)}&requestedUserExponent={ForeignPublicKey.Exponent.ToString(16)}";
		JsonArray messages = JsonNode.Parse(Https.Get($"http://{_settings.IpAddress}:5000/messages?" + getVariables).Body)!.AsArray();
		foreach (JsonNode? messageNode in messages) {
			Message message = Message.Parse(messageNode!.AsObject());
			bool isOwnMessage = Equals(message.Sender, PersonalPublicKey);
			DecryptedMessage decryptedMessage = Decrypt(message, isOwnMessage);
			if (!Verify(decryptedMessage.Body, message.Signature, isOwnMessage))
				continue; // Just don't add the message if it is not legitimate
			
			res.Add(new DecryptedMessage { Id = message.Id, Body = decryptedMessage.Body, DateTime = DateTime.MinValue, Sender = message.Sender.Modulus.ToString(16)});
		}

		return res.ToArray();
	}
	
	public long SendMessage(string message) {
		Message encryptedMessage = Encrypt(message);
		JsonObject body = new () {
			["sender"] = new JsonObject {
				["modulus"] = PersonalPublicKey.Modulus.ToString(16),
				["exponent"] = PersonalPublicKey.Exponent.ToString(16)
			},
			["receiver"] = new JsonObject {
				["modulus"] = ForeignPublicKey.Modulus.ToString(16),
				["exponent"] = ForeignPublicKey.Exponent.ToString(16)
			},
			["text"] = encryptedMessage.Body,
			["senderEncryptedKey"] = encryptedMessage.SenderEncryptedKey,
			["receiverEncryptedKey"] = encryptedMessage.ReceiverEncryptedKey,
			["signature"] = encryptedMessage.Signature
		};
		
		string response = Https.Post($"http://{_settings.IpAddress}:5000/messages", JsonSerializer.Serialize(body)).Body;
		return long.Parse(response);
	}

	public void DeleteMessage(long id) {
		JsonObject body = new () {
			["id"] = id,
			["signature"] = Sign(id.ToString())
		};
		_context.RemoveMessage(id);
		Https.Delete($"http://{_settings.IpAddress}:5000/messages", JsonSerializer.Serialize(body));
	}
}