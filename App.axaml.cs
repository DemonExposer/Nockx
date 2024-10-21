using System;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
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
	public const string Version = "v1.0-beta";
	
	private bool _isKeyLoadedSuccessfully; 
	
	public override void Initialize() {
		Chats.LoadOrDefault(Constants.ChatsFile);
		
		Settings.LoadOrDefault(Constants.SettingsFile);
		
		// TODO: Test this untested code and add code to unpack the zip file
		Https.Response response = Https.Get("https://api.github.com/repos/DemonExposer/SecureChat/releases/latest");
		if (response.IsSuccessful) {
			JsonObject releaseObj = JsonNode.Parse(response.Body)!.AsObject();
			if (releaseObj["name"]!.GetValue<string>() != Version) {
				foreach (JsonNode? node in releaseObj["assets"]!.AsArray()) {
					// e.g. "SecureChat_debian-x64.zip" in this case it takes everything after the underscore and removes ".zip" (4 characters)
					if (node!["name"]!.GetValue<string>().Split("_")[1][..^4] != System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier)
						continue;

					using HttpClient httpClient = new ();
					using Task<Stream> streamTask = httpClient.GetStreamAsync(node["browser_download_url"]!.GetValue<string>());
					using FileStream fileStream = new (node["name"]!.GetValue<string>(), FileMode.Create);
					streamTask.Result.CopyTo(fileStream);
				}
			}
		}
		
		
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
				desktop.MainWindow = new ErrorPopupWindow("one key file found, zero or two expected");
		}

		base.OnFrameworkInitializationCompleted();
	}
}