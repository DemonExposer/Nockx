using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LessAnnoyingHttp;
using Nockx.Base;
using Nockx.Base.ClassExtensions;
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
	private readonly List<Timer> _timers = [];
	
	public override void Initialize() {
		Chats.LoadOrDefault(Constants.ChatsFile);
		
		Settings.LoadOrDefault(Constants.SettingsFile);
		
		Response response = Http.Get("https://api.github.com/repos/DemonExposer/Nockx/releases/latest", [new Header { Name = "User-Agent", Value = "Nockx-Client" }]);
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
		Notifications.SetGuiApplication(true);
		Notifications.WindowsAudioSource = null;
		
		CheckOrGenerateKeys();
		AvaloniaXamlLoader.Load(this);
	}

	public void AddTimer(Timer timer) => _timers.Add(timer);

	private void CheckOrGenerateKeys() {
		if (!File.Exists(Constants.PrivateKeyFile) && !File.Exists(Constants.PublicKeyFile)) {
			// Generate RSA key
			RsaKeyPairGenerator rsaGenerator = new ();
			rsaGenerator.Init(new RsaKeyGenerationParameters(Constants.KEY_EXPONENT, new SecureRandom(), Constants.KEY_SIZE_BITS, 80));
			
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
		} else if (!File.Exists(Constants.PublicKeyFile)) {
			RsaKeyParameters privateKey;
			using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
				PemReader pemReader = new (reader);
				privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
			}
			
			// Create a public key from the private key and write it to a file
			using (TextWriter textWriter = new StreamWriter(Constants.PublicKeyFile)) {
				PemWriter pemWriter = new (textWriter);
				pemWriter.WriteObject(new RsaKeyParameters(false, privateKey.Modulus, Constants.KEY_EXPONENT));
				pemWriter.Writer.Flush();
			}
			
			_isKeyLoadedSuccessfully = true;
		} else if (!File.Exists(Constants.PrivateKeyFile)) {
			_isKeyLoadedSuccessfully = false;
		} else {
			_isKeyLoadedSuccessfully = true;
		}

		if (_isKeyLoadedSuccessfully)
			RegisterUser();
	}

	public override void OnFrameworkInitializationCompleted() {
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			desktop.Exit += (_, _) => {
				GlobalPlaybackDevice.Close();
				_timers.ForEach(t => t.Dispose());
			};
			
			if (!_isKeyLoadedSuccessfully) {
				desktop.MainWindow = new ErrorPopupWindow("Private key was not found, whereas public key was found");
			} else if (_doUpdate) {
				desktop.MainWindow = new UpdateWindow();
				((UpdateWindow) desktop.MainWindow!).Update(_releaseFileObject);
			} else {
				desktop.MainWindow = new MainWindow();
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void RegisterUser() {
		RsaKeyParameters publicKey, privateKey;
		using (StreamReader reader = File.OpenText(Constants.PublicKeyFile)) {
			PemReader pemReader = new (reader);
			publicKey = (RsaKeyParameters) pemReader.ReadObject();
		}

		using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
			PemReader pemReader = new (reader);
			privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
		}

		JsonObject jsonObject = new () {
			["key"] = publicKey.ToBase64String(),
			["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};
		string body = JsonSerializer.Serialize(jsonObject);

		Http.Post($"https://{Settings.GetInstance().Hostname}:5000/registerUser", body, [new Header { Name = "Signature", Value = Cryptography.Sign(body, privateKey) }]);
	}
}