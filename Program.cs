using Avalonia;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using Org.BouncyCastle.OpenSsl;

namespace SecureChat;

class Program {
	private const string PrivateKeyFile = "private_key.pem";
	private const string PublicKeyFile = "public_key.pem";

	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args) {
		if (!File.Exists(PrivateKeyFile) && !File.Exists(PublicKeyFile)) {
			ECKeyPairGenerator keyGenerator = new ();
			SecureRandom secureRandom = new ();
			KeyGenerationParameters keyGenParams = new (secureRandom, 256);

			keyGenerator.Init(keyGenParams);
			AsymmetricCipherKeyPair keyPair = keyGenerator.GenerateKeyPair();

			ECPrivateKeyParameters privateKey = (ECPrivateKeyParameters) keyPair.Private;
			ECPublicKeyParameters publicKey = (ECPublicKeyParameters) keyPair.Public;

			using (TextWriter textWriter = new StreamWriter(PrivateKeyFile)) {
				PemWriter pemWriter = new (textWriter);
				pemWriter.WriteObject(privateKey);
				pemWriter.Writer.Flush();
			}

			using (TextWriter textWriter = new StreamWriter(PublicKeyFile)) {
				PemWriter pemWriter = new (textWriter);
				pemWriter.WriteObject(publicKey);
				pemWriter.Writer.Flush();
			}
		} else if (!File.Exists(PrivateKeyFile) || !File.Exists(PublicKeyFile)) {
			throw new Exception("one key file found, zero or two expected"); // TODO: change this into a pop-up
		}

		BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace();
}
