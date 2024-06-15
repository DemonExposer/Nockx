using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using SecureChat.util;
using SecureChat.windows;

namespace SecureChat;

public partial class App : Application {
	private bool _isKeyLoadedSuccessfully; 
	
	public override void Initialize() {
		if (!File.Exists(Constants.ChatsFile))
			File.WriteAllText(Constants.ChatsFile, "[]");

		CheckOrGenerateKeys();
		AvaloniaXamlLoader.Load(this);
	}

	private void CheckOrGenerateKeys() {
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

			_isKeyLoadedSuccessfully = true;
		} else if (!File.Exists(Constants.PrivateKeyFile) || !File.Exists(Constants.PublicKeyFile)) {
			_isKeyLoadedSuccessfully = false;
		} else {
			_isKeyLoadedSuccessfully = true;
		}
	}

	public override void OnFrameworkInitializationCompleted() {
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			if (_isKeyLoadedSuccessfully)
				desktop.MainWindow = new MainWindow();
			else
				// TODO: add option to generate public key from private key if only the private key is available or the option to regenerate both
				desktop.MainWindow = new PopupWindow("one key file found, zero or two expected");
		}

		base.OnFrameworkInitializationCompleted();
	}
}