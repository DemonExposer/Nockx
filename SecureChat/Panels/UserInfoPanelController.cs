using System;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using SecureChat.Util;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using LessAnnoyingHttp;
using Org.BouncyCastle.Crypto;
using SecureChat.ClassExtensions;

namespace SecureChat.Panels;

public class UserInfoPanelController {
	public readonly RsaKeyParameters PublicKey;
	private readonly RsaKeyParameters _privateKey;
	public string DisplayName = "";
	private MainWindowModel? _mainWindowModel;

	public UserInfoPanelController() {
		using (StreamReader reader = File.OpenText(Constants.PublicKeyFile)) {
			PemReader pemReader = new (reader);
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
		string getVariables = $"key={HttpUtility.UrlEncode(PublicKey.ToBase64String())}";
		
		DisplayName = Http.Get($"https://{Settings.GetInstance().Hostname}:5000/displayname?" + getVariables).Body;
		_mainWindowModel.DisplayName = DisplayName;
	}

	public bool SetDisplayName(string displayName) {
		if (_mainWindowModel == null)
			throw new InvalidOperationException("SetDisplayName may not be called before _mainWindowModel is set, using SetMainWindowModel");
		JsonObject body = new () {
			["user"] = new JsonObject {
				["key"] = PublicKey.ToBase64String(),
				["displayName"] = displayName
			},
			["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};
		string bodyString = JsonSerializer.Serialize(body);
		Response response = Http.Put($"https://{Settings.GetInstance().Hostname}:5000/displayname", bodyString, [new Header { Name = "Signature", Value = Cryptography.Sign(bodyString, _privateKey) }]);
		if (response.IsSuccessful) {
			DisplayName = displayName;
			_mainWindowModel.DisplayName = displayName;
		}
		return response.IsSuccessful;
	}
}
