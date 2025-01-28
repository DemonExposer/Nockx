using System;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using SecureChat.model;

namespace SecureChat.util;

public static class Cryptography {
	public static byte[] GenerateAesKey() {
		CipherKeyGenerator aesKeyGen = new ();
		aesKeyGen.Init(new KeyGenerationParameters(new SecureRandom(), 256));
		return aesKeyGen.GenerateKey();
	}

	public static byte[] EncryptAesKey(byte[] aesKey, RsaKeyParameters rsaPublicKey) {
		OaepEncoding rsaEngine = new (new RsaEngine());
		rsaEngine.Init(true, rsaPublicKey);
		return rsaEngine.ProcessBlock(aesKey, 0, aesKey.Length);
	}

	public static byte[] DecryptAesKey(byte[] encryptedAesKey, RsaKeyParameters rsaPrivateKey) {
		OaepEncoding rsaEngine = new (new RsaEngine());
		rsaEngine.Init(false, rsaPrivateKey);
		return rsaEngine.ProcessBlock(encryptedAesKey, 0, encryptedAesKey.Length);
	}

	public static byte[] EncryptWithAes(byte[] data, int inputLength, byte[] aesKey) {
		AesEngine aesEngine = new ();
		PaddedBufferedBlockCipher cipher = new (new CbcBlockCipher(aesEngine), new Pkcs7Padding());
		cipher.Init(true, new KeyParameter(aesKey));
		
		byte[] cipherBytes = new byte[cipher.GetOutputSize(inputLength)];
		int length = cipher.ProcessBytes(data, 0, inputLength, cipherBytes, 0);
		length += cipher.DoFinal(cipherBytes, length);

		return cipherBytes;
	}

	public static (byte[] plainBytes, int length) DecryptWithAes(byte[] data, byte[] aesKey) {
		AesEngine aesEngine = new ();
		PaddedBufferedBlockCipher cipher = new (new CbcBlockCipher(aesEngine), new Pkcs7Padding());
		cipher.Init(false, new KeyParameter(aesKey));
		
		byte[] plainBytes = new byte[cipher.GetOutputSize(data.Length)];
		int length = cipher.ProcessBytes(data, 0, data.Length, plainBytes, 0);
		length += cipher.DoFinal(plainBytes, length);

		return (plainBytes, length);
	}
	
	public static string Sign(string text, RsaKeyParameters privateKey) {
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		RsaDigestSigner signer = new (new Sha256Digest());
		signer.Init(true, privateKey);
		
		signer.BlockUpdate(bytes, 0, bytes.Length);
		byte[] signature = signer.GenerateSignature();

		return Convert.ToBase64String(signature);
	}

	public static bool Verify(string text, string signature, RsaKeyParameters? personalPublicKey, RsaKeyParameters? foreignPublicKey, bool isOwnMessage) {
		switch (isOwnMessage) {
			case true when personalPublicKey == null:
				throw new ArgumentNullException(nameof(personalPublicKey), "must not be null when verifying own messages");
			case false when foreignPublicKey == null:
				throw new ArgumentNullException(nameof(foreignPublicKey), "must not be null when verifying other's messages");
		}
		
		byte[] textBytes = Encoding.UTF8.GetBytes(text);
		byte[] signatureBytes = Convert.FromBase64String(signature);

		RsaDigestSigner verifier = new (new Sha256Digest());
		verifier.Init(false, isOwnMessage ? personalPublicKey : foreignPublicKey);
		
		verifier.BlockUpdate(textBytes, 0, textBytes.Length);

		return verifier.VerifySignature(signatureBytes);
	}

	public static Message Encrypt(string inputText, RsaKeyParameters personalPublicKey, RsaKeyParameters foreignPublicKey, RsaKeyParameters privateKey) {
		// Encrypt using AES
		byte[] aesKey = GenerateAesKey();

		byte[] plainBytes = Encoding.UTF8.GetBytes(inputText);
		byte[] cipherBytes = EncryptWithAes(plainBytes, plainBytes.Length, aesKey);
		
		// Encrypt the AES key using RSA
		byte[] personalEncryptedKey = EncryptAesKey(aesKey, personalPublicKey);
		
		byte[] foreignEncryptedKey = EncryptAesKey(aesKey, foreignPublicKey);

		return new Message {
			Body = Convert.ToBase64String(cipherBytes),
			SenderEncryptedKey = Convert.ToBase64String(personalEncryptedKey),
			ReceiverEncryptedKey = Convert.ToBase64String(foreignEncryptedKey),
			Signature = Sign(inputText, privateKey),
			Receiver = foreignPublicKey,
			Sender = personalPublicKey
		};
	}

	public static DecryptedMessage Decrypt(Message message, RsaKeyParameters privateKey, bool isOwnMessage) {
		// Decrypt the AES key using RSA
		byte[] aesKeyEncrypted = Convert.FromBase64String(isOwnMessage ? message.SenderEncryptedKey : message.ReceiverEncryptedKey);
		
		byte[] aesKey = DecryptAesKey(aesKeyEncrypted, privateKey);
		
		// Decrypt the message using AES
		(byte[] plainBytes, int length) = DecryptWithAes(Convert.FromBase64String(message.Body), aesKey);

		string body = Encoding.UTF8.GetString(plainBytes, 0, length);
		return new DecryptedMessage { Id = message.Id, Body = body, Sender = message.Sender.Modulus.ToString(16), DateTime = DateTime.MinValue};
	}
}