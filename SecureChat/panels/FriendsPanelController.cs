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
			return;
		}
		
		JsonArray friends = JsonNode.Parse(response.Body)!.AsArray();
		foreach (JsonNode? jsonNode in friends) {
			JsonObject friendObject = jsonNode!.AsObject();
			_context.AddFriend(FriendRequest.Parse(friendObject));
		}
	}

	public void AcceptFriendRequest(FriendRequest friendRequest) {
		string body = FriendRequest.Serialize(friendRequest);
		Console.WriteLine(PersonalPublicKey.Modulus.ToString(16));
		Console.WriteLine(PersonalPublicKey.Exponent.ToString(16));
		Console.WriteLine(Cryptography.Verify(body, Cryptography.Sign(body, _privateKey), PersonalPublicKey, null, true));
		Response response = Http.Put($"http://{_settings.IpAddress}:5000/friends", body, [new Header { Name = "Signature", Value = Cryptography.Sign(body, _privateKey) }]);
		if (!response.IsSuccessful) {
			Console.WriteLine(response.StatusCode);
		}
	}

	public void DeleteFriendRequest(FriendRequest friendRequest) {
		string body = FriendRequest.Serialize(friendRequest);
		Response response = Http.Delete($"http://{_settings.IpAddress}:5000/friends?modulus={_privateKey.Modulus.ToString(16)}", body, [new Header { Name = "Signature", Value = Cryptography.Sign(body, _privateKey) }]);
		if (!response.IsSuccessful) {
			Console.WriteLine(response.StatusCode);
			Console.WriteLine(response.Body);
		}
	}
}