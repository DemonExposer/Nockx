using System;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using SecureChat.util;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using LessAnnoyingHttp;
using Org.BouncyCastle.Crypto;

namespace SecureChat.panels;

public class UserInfoPanelController {
	public readonly RsaKeyParameters PublicKey;
	private readonly RsaKeyParameters _privateKey;
	private readonly Settings _settings = Settings.GetInstance();
	public string Nickname = "";
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
		
		GetNickname();
	}
	
	public void SetMainWindowModel(MainWindowModel mainWindowModel) {
		if (_mainWindowModel != null)
			return;
		
		_mainWindowModel = mainWindowModel;
	}

	private void GetNickname() {
		string getVariables = $"modulus={PublicKey.Modulus.ToString(16)}&exponent={PublicKey.Exponent.ToString(16)}";
		Nickname = Http.Get($"http://{_settings.IpAddress}:5000/nickname?" + getVariables).Body;
	}

	public bool SetNickname(string nickname) {
		if (_mainWindowModel == null)
			throw new InvalidOperationException("SetNickname may not be called before _mainWindowModel is set, using SetMainWindowModel");
		JsonObject body = new() {
			["modulus"] = PublicKey.Modulus.ToString(16),
			["exponent"] = PublicKey.Exponent.ToString(16),
			["nickname"] = nickname
		};
		string bodyString = JsonSerializer.Serialize(body);
		Response response = Http.Put($"http://{_settings.IpAddress}:5000/nickname", bodyString, [new Header {Name = "Signature", Value = Cryptography.Sign(bodyString, _privateKey)}]);
		if (response.IsSuccessful) {
			Nickname = nickname;
			_mainWindowModel.Nickname = nickname;
		}
		return response.IsSuccessful;
	}
}
