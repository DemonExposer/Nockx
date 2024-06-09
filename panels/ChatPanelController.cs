using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
using SecureChat.util;

namespace SecureChat;

public class ChatPanelController {
	public RsaKeyParameters ForeignPublicKey;

	private readonly RsaKeyParameters _personalPublicKey;
	private readonly RsaKeyParameters _privateKey;
	
	public ChatPanelController() {
		using (StreamReader reader = File.OpenText(Constants.PublicKeyFile)) {
			PemReader pemReader = new (reader);
			_personalPublicKey = (RsaKeyParameters) pemReader.ReadObject();
		}
		
		using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
			PemReader pemReader = new (reader);
			_privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
		}
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
		verifier.Init(false, isOwnMessage ? _personalPublicKey : ForeignPublicKey);
		
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
		rsaEngine.Init(true, _personalPublicKey);
		byte[] personalEncryptedKey = rsaEngine.ProcessBlock(aesKey, 0, aesKey.Length);
		
		rsaEngine.Init(true, ForeignPublicKey);
		byte[] foreignEncryptedKey = rsaEngine.ProcessBlock(aesKey, 0, aesKey.Length);

		return new Message {
			Body = Convert.ToBase64String(cipherBytes),
			SenderEncryptedKey = Convert.ToBase64String(personalEncryptedKey),
			ReceiverEncryptedKey = Convert.ToBase64String(foreignEncryptedKey),
			Signature = Sign(inputText),
			Receiver = ForeignPublicKey,
			Sender = _personalPublicKey
		};
	}

	private string Decrypt(Message message, bool isOwnMessage) {
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

		return Encoding.UTF8.GetString(plainBytes, 0, length);
	}

	public string[] GetPastMessages() {
		List<string> res = new ();
		
		string getVariables = $"requestingUserModulus={_personalPublicKey.Modulus.ToString(16)}&requestingUserExponent={_personalPublicKey.Exponent.ToString(16)}&requestedUserModulus={ForeignPublicKey.Modulus.ToString(16)}&requestedUserExponent={ForeignPublicKey.Exponent.ToString(16)}";
		JsonArray messages = JsonNode.Parse(Https.Get("http://localhost:5100/messages?" + getVariables).Body)!.AsArray();
		foreach (JsonNode? messageNode in messages) {
			Message message = Message.Parse(messageNode!.AsObject());
			bool isOwnMessage = Equals(message.Sender, _personalPublicKey);
			string messageBody = Decrypt(message, isOwnMessage);
			if (!Verify(messageBody, message.Signature, isOwnMessage))
				Console.WriteLine("Data has been messed with!"); // TODO: handle this properly
			
			res.Add(messageBody);
		}

		return res.ToArray();
	}
	
	public void Send(string message) {
		Message encryptedMessage = Encrypt(message);
		JsonObject body = new () {
			["sender"] = new JsonObject {
				["modulus"] = _personalPublicKey.Modulus.ToString(16),
				["exponent"] = _personalPublicKey.Exponent.ToString(16)
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
		Console.WriteLine(Https.Post("http://localhost:5100/messages", JsonSerializer.Serialize(body)).Body);
	}
}