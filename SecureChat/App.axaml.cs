using System.IO;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LessAnnoyingHttp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using OsNotifications;
using SecureChat.Audio;
using SecureChat.Util;
using SecureChat.Windows;

namespace SecureChat;

public partial class App : Application {
	public const string Version = "v2.0-beta";
	
	private bool _isKeyLoadedSuccessfully;
	private bool _doUpdate;
	private JsonObject _releaseFileObject;
	
	public override void Initialize() {
		Chats.LoadOrDefault(Constants.ChatsFile);
		
		Settings.LoadOrDefault(Constants.SettingsFile);
		
		Response response = Http.Get("https://api.github.com/repos/DemonExposer/SecureChat/releases/latest", [new Header { Name = "User-Agent", Value = "Nockx-Client" }]);
		if (response.IsSuccessful) {
			JsonObject releaseObj = JsonNode.Parse(response.Body)!.AsObject();
			if (releaseObj["name"]!.GetValue<string>() != Version) {
				foreach (JsonNode? node in releaseObj["assets"]!.AsArray()) {
					// e.g. "SecureChat_linux-x64.zip" in this case it takes everything after the underscore and removes ".zip" (4 characters)
					if (node!["name"]!.GetValue<string>().Split("_")[1][..^4] != System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier)
						continue;
					
					_doUpdate = true;
					_releaseFileObject = node.AsObject();
				}
			}
		}
		
		GlobalPlaybackDevice.Initialize();
		Notifications.BundleIdentifier = "com.apple.finder"; // TODO: change this to the actual bundle identifier
		Notifications.SetGuiApplication(true);
		Notifications.WindowsAudioSource = null;
		
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
			desktop.Exit += (_, _) => GlobalPlaybackDevice.Close();
			
			if (!_isKeyLoadedSuccessfully) {
				// TODO: add option to generate public key from private key if only the private key is available or the option to regenerate both
				desktop.MainWindow = new ErrorPopupWindow("one key file found, zero or two expected");
			} else if (_doUpdate) {
				desktop.MainWindow = new UpdateWindow();
				((UpdateWindow) desktop.MainWindow!).Update(_releaseFileObject);
			} else {
				desktop.MainWindow = new MainWindow();
			}
		}

		base.OnFrameworkInitializationCompleted();
	}
}