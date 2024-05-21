using System;
using System.IO;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using SecureChat.util;

namespace SecureChat;

public class MainWindowController {
	private readonly RsaKeyParameters _senderPublicKey, _receiverPublicKey;
	private readonly RsaKeyParameters _privateKey;
	
	public MainWindowController() {
		using (StreamReader reader = File.OpenText(Constants.PublicKeyFile)) {
			PemReader pemReader = new (reader);
			_senderPublicKey = (RsaKeyParameters) pemReader.ReadObject();
		}
		
		using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
			PemReader pemReader = new (reader);
			_privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
		}
		
		_receiverPublicKey = null!;
	}

	private string Encrypt(string inputText) {
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

		// TODO: add the AES key encrypted with the receiver's key
		// Encrypt the AES key using RSA
		OaepEncoding rsaEngine = new (new RsaEngine());
		rsaEngine.Init(true, _senderPublicKey);
		
		byte[] encryptedKey = rsaEngine.ProcessBlock(aesKey, 0, aesKey.Length);
		
		// Merge the encrypted key with the encrypted message
		byte[] combinedOutput = new byte[encryptedKey.Length + length];
		Array.Copy(encryptedKey, 0, combinedOutput, 0, encryptedKey.Length);
		Array.Copy(cipherBytes, 0, combinedOutput, encryptedKey.Length, length);

		return Convert.ToBase64String(combinedOutput);
	}

	private string Decrypt(string cipherText) {
		byte[] cipherBytes = Convert.FromBase64String(cipherText);

		// Decrypt the AES key using RSA
		byte[] aesKeyEncrypted = cipherBytes[..Constants.KEY_SIZE_BYTES]; // Separate the key from the message
		OaepEncoding rsaEngine = new (new RsaEngine());
		rsaEngine.Init(false, _privateKey);
		
		byte[] aesKey = rsaEngine.ProcessBlock(aesKeyEncrypted, 0, aesKeyEncrypted.Length);
		
		// Decrypt the message using AES
		AesEngine aesEngine = new ();
		PaddedBufferedBlockCipher cipher = new (new CbcBlockCipher(aesEngine), new Pkcs7Padding());
		cipher.Init(false, new KeyParameter(aesKey));

		int trueCipherLength = cipherBytes.Length - Constants.KEY_SIZE_BYTES; // Separate the message from the key
		byte[] plainBytes = new byte[cipher.GetOutputSize(trueCipherLength)];
		int length = cipher.ProcessBytes(cipherBytes, Constants.KEY_SIZE_BYTES, trueCipherLength, plainBytes, 0);
		length += cipher.DoFinal(plainBytes, length);

		return Encoding.UTF8.GetString(plainBytes, 0, length);
	}
	
	public void Send(string message) {
		Console.WriteLine(Decrypt(Encrypt(message)));
	}
}