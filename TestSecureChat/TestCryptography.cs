using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using SecureChat.Model;
using SecureChat.Util;

namespace TestSecureChat;

public class TestCryptography {
	private RsaKeyParameters _publicKey;
	private RsaKeyParameters _privateKey;
	private byte[] _aesKey;
	private const string M = "test message";
	
	[SetUp]
	public void Setup() {
		RsaKeyPairGenerator rsaGenerator = new ();
		rsaGenerator.Init(new KeyGenerationParameters(new SecureRandom(), Constants.KEY_SIZE_BITS));
			
		AsymmetricCipherKeyPair keyPair = rsaGenerator.GenerateKeyPair();

		_privateKey = (RsaKeyParameters) keyPair.Private;
		_publicKey = (RsaKeyParameters) keyPair.Public;
		_aesKey = Cryptography.GenerateAesKey();
	}

	[Test]
	public void TestAes() {
		byte[] message = Encoding.UTF8.GetBytes(M);

		byte[] encrypted = Cryptography.EncryptWithAes(message, message.Length, _aesKey);
		(byte[] decrypted, int length) = Cryptography.DecryptWithAes(encrypted, _aesKey);
		string d = Encoding.UTF8.GetString(decrypted, 0, length);
		
		Assert.That(d, Is.EqualTo(M));
	}

	[Test]
	public void TestAesKeyEncrypt() {
		Assert.That(_aesKey, Is.EqualTo(Cryptography.DecryptAesKey(Cryptography.EncryptAesKey(_aesKey, _publicKey), _privateKey)));
	}

	[Test]
	public void TestSign() {
		Assert.That(Cryptography.Verify(M, Cryptography.Sign(M, _privateKey), _publicKey, null, true), Is.True);
	}

	[Test]
	public void TestEncryptMessage() {
		RsaKeyPairGenerator rsaGenerator = new ();
		rsaGenerator.Init(new KeyGenerationParameters(new SecureRandom(), Constants.KEY_SIZE_BITS));
			
		AsymmetricCipherKeyPair keyPair = rsaGenerator.GenerateKeyPair();

		RsaKeyParameters foreignPrivateKey = (RsaKeyParameters) keyPair.Private;
		RsaKeyParameters foreignPublicKey = (RsaKeyParameters) keyPair.Public;
		
		Message a = Cryptography.Encrypt(M, _publicKey, foreignPublicKey, _privateKey);
		DecryptedMessage d = Cryptography.Decrypt(a, foreignPrivateKey, false);
		
		Assert.That(d.Body, Is.EqualTo(M));
	}
}