using System;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using SecureChat.Util;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using LessAnnoyingHttp;
using Org.BouncyCastle.Crypto;
using SecureChat.ClassExtensions;

namespace SecureChat.Panels;

public class UserInfoPanelController {
	public readonly RsaKeyParameters PublicKey;
	private readonly RsaKeyParameters _privateKey;
	private readonly Settings _settings = Settings.GetInstance();
	public string DisplayName = "";
	private MainWindowModel? _mainWindowModel;

	public UserInfoPanelController() {
		using (StreamReader reader = File.OpenText(Constants.PublicKeyFile)) {
			PemReader pemReader = new(reader);
			PublicKey = (RsaKeyParameters) pemReader.ReadObject();
		}

		using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
			PemReader pemReader = new (reader);
			_privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
		}

		DisplayName = PublicKey.ToBase64String();
	}
	
	public void SetMainWindowModel(MainWindowModel mainWindowModel) {
		if (_mainWindowModel != null)
			return;
		
		_mainWindowModel = mainWindowModel;
		GetDisplayName();
	}

	private void GetDisplayName() {
		if (_mainWindowModel == null)
			throw new InvalidOperationException("GetDisplayName may not be called before _mainWindowModel is set, using SetMainWindowModel");
		string getVariables = $"key={PublicKey.ToBase64String()}";
		DisplayName = Http.Get($"http://{_settings.IpAddress}:5000/displayname?" + getVariables).Body;
		_mainWindowModel.DisplayName = DisplayName;
	}

	public bool SetDisplayName(string displayName) {
		if (_mainWindowModel == null)
			throw new InvalidOperationException("SetDisplayName may not be called before _mainWindowModel is set, using SetMainWindowModel");
		JsonObject body = new() {
			["key"] = PublicKey.ToBase64String(),
			["displayName"] = displayName
		};
		string bodyString = JsonSerializer.Serialize(body);
		Response response = Http.Put($"http://{_settings.IpAddress}:5000/displayname", bodyString, [new Header {Name = "Signature", Value = Cryptography.Sign(bodyString, _privateKey)}]);
		if (response.IsSuccessful) {
			DisplayName = displayName;
			_mainWindowModel.DisplayName = displayName;
		}
		return response.IsSuccessful;
	}
}
