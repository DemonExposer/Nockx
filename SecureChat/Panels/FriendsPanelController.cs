using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using LessAnnoyingHttp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using SecureChat.ClassExtensions;
using SecureChat.util;
using SecureChat.Util;

namespace SecureChat.Panels;

public class FriendsPanelController {
	public readonly RsaKeyParameters PersonalPublicKey;
	private readonly RsaKeyParameters _privateKey;
	private readonly Settings _settings = Settings.GetInstance();

	public FriendsPanelController() {
		
		using (StreamReader reader = File.OpenText(Constants.PublicKeyFile)) {
			PemReader pemReader = new (reader);
			PersonalPublicKey = (RsaKeyParameters) pemReader.ReadObject();
		}
		
		using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
			PemReader pemReader = new (reader);
			_privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
		}
	}

	public List<FriendRequest> GetFriends() {
		string getVariables =
			$"key={HttpUtility.UrlEncode(PersonalPublicKey.ToBase64String())}";
		Console.WriteLine(getVariables);
		Console.WriteLine(Cryptography.Sign(getVariables, _privateKey));
		Response response = Http.Get($"http://{_settings.IpAddress}:5000/friends?{getVariables}",
			[new Header { Name = "Signature", Value = Cryptography.Sign(getVariables, _privateKey) }]);
		//TODO: error popup when friends could not be retrieved
		if (!response.IsSuccessful) {
			Console.WriteLine(response.StatusCode);
			return [];
		}

		List<FriendRequest> res = [];
		JsonArray friends = JsonNode.Parse(response.Body)!.AsArray();
		foreach (JsonNode? jsonNode in friends) {
			JsonObject friendObject = jsonNode!.AsObject();
			res.Add(FriendRequest.Parse(friendObject));
		}
		return res;
	}

	public void AcceptFriendRequest(FriendRequest friendRequest) {
		string body = FriendRequest.Serialize(friendRequest);
		Response response = Http.Put($"http://{_settings.IpAddress}:5000/friends", body,
			[new Header { Name = "Signature", Value = Cryptography.Sign(body, _privateKey) }]);
		if (!response.IsSuccessful) {
			Console.WriteLine(response.StatusCode);
		}
	}

	public void DeleteFriendRequest(FriendRequest friendRequest) {
		string body = FriendRequest.Serialize(friendRequest);
		Response response =
			Http.Delete(
				$"http://{_settings.IpAddress}:5000/friends?key={HttpUtility.UrlEncode(PersonalPublicKey.ToBase64String())}",
				body, [new Header { Name = "Signature", Value = Cryptography.Sign(body, _privateKey) }]);
		if (!response.IsSuccessful) {
			Console.WriteLine(response.StatusCode);
			Console.WriteLine(response.Body);
		}
	}
}