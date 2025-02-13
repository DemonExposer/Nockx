using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using LessAnnoyingHttp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using SecureChat.util;

namespace SecureChat.panels;

public class FriendsPanelController {
	private readonly FriendsPanel _context;
	public readonly RsaKeyParameters PersonalPublicKey;
	private readonly RsaKeyParameters _privateKey;
	private readonly Settings _settings = Settings.GetInstance();

	public FriendsPanelController(FriendsPanel context) {
		_context = context;
		
		using (StreamReader reader = File.OpenText(Constants.PublicKeyFile)) {
			PemReader pemReader = new (reader);
			PersonalPublicKey = (RsaKeyParameters) pemReader.ReadObject();
		}
		
		using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
			PemReader pemReader = new (reader);
			_privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
		}
	}

	public void GetFriends() {
		string getVariables =
			$"modulus={PersonalPublicKey.Modulus.ToString(16)}&exponent={PersonalPublicKey.Exponent.ToString(16)}";
		Response response = Http.Get($"http://{_settings.IpAddress}:5000/friends?{getVariables}",
			[new Header { Name = "Signature", Value = Cryptography.Sign(getVariables, _privateKey) }]);
		//TODO: error popup when friends could not be retrieved
		if (!response.IsSuccessful) {
			Console.WriteLine(response.StatusCode);
			// Console.WriteLine("Failed to get Friends");
			return;
		}
		
		JsonArray friends = JsonNode.Parse(response.Body)!.AsArray();
		foreach (JsonNode? jsonNode in friends) {
			JsonObject friendObject = jsonNode!.AsObject();
			_context.AddFriend(FriendRequest.Parse(friendObject));
		}
	}
}