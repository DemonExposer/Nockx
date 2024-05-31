using Avalonia;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using Org.BouncyCastle.OpenSsl;
using SecureChat.util;

namespace SecureChat;

class Program {
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args) {
		if (!File.Exists(Constants.PrivateKeyFile) && !File.Exists(Constants.PublicKeyFile)) {
			// Generate RSA key
			RsaKeyPairGenerator rsaGenerator = new ();
			rsaGenerator.Init(new KeyGenerationParameters(new SecureRandom(), Constants.KEY_SIZE_BITS));
			
			AsymmetricCipherKeyPair keyPair = rsaGenerator.GenerateKeyPair();

			RsaKeyParameters privateKey = (RsaKeyParameters) keyPair.Private;
			RsaKeyParameters publicKey = (RsaKeyParameters) keyPair.Public;
			
			// Write private and public keys to files
			using (TextWriter textWriter = new StreamWriter(Constants.PrivateKeyFile)) {
				PemWriter pemWriter = new (textWriter);
				pemWriter.WriteObject(privateKey);
				pemWriter.Writer.Flush();
			}

			using (TextWriter textWriter = new StreamWriter(Constants.PublicKeyFile)) {
				PemWriter pemWriter = new (textWriter);
				pemWriter.WriteObject(publicKey);
				pemWriter.Writer.Flush();
			}
		} else if (!File.Exists(Constants.PrivateKeyFile) || !File.Exists(Constants.PublicKeyFile)) {
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
