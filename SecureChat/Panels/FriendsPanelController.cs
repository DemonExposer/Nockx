﻿using System;
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
using SecureChat.Util;

namespace SecureChat.Panels;

public class FriendsPanelController {
	public RsaKeyParameters PersonalPublicKey {
		get {
			if (_personalPublicKey == null)
				throw new InvalidOperationException("FriendsPanelController PersonalPublicKey was not set before usage");

			return _personalPublicKey;
		}
		set => _personalPublicKey = value;
	}

	private RsaKeyParameters? _personalPublicKey;
	private readonly RsaKeyParameters _privateKey;

	public FriendsPanelController() {
		using (StreamReader reader = File.OpenText(Constants.PrivateKeyFile)) {
			PemReader pemReader = new (reader);
			_privateKey = (RsaKeyParameters) ((AsymmetricCipherKeyPair) pemReader.ReadObject()).Private;
		}
	}

	public List<FriendRequest> GetFriends() {
		string getVariables = $"key={HttpUtility.UrlEncode(PersonalPublicKey.ToBase64String())}&timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
		Response response = Http.Get($"https://{Settings.GetInstance().Hostname}:5000/friends?{getVariables}", [new Header { Name = "Signature", Value = Cryptography.Sign(getVariables, _privateKey) }]);
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
		friendRequest.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		string body = FriendRequest.Serialize(friendRequest);
		Response response = Http.Put($"https://{Settings.GetInstance().Hostname}:5000/friends", body, [new Header { Name = "Signature", Value = Cryptography.Sign(body, _privateKey) }]);
		if (!response.IsSuccessful) {
			Console.WriteLine(response.StatusCode);
		}
	}

	public void DeleteFriendRequest(string personalKey, string foreignKey) {
		JsonObject jsonObject = new () {
			["personalKey"] = personalKey,
			["foreignKey"] = foreignKey,
			["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};
		string body = JsonSerializer.Serialize(jsonObject);

		Response response = Http.Delete($"https://{Settings.GetInstance().Hostname}:5000/friends", body, [new Header { Name = "Signature", Value = Cryptography.Sign(body, _privateKey) }]);
		if (!response.IsSuccessful) {
			Console.WriteLine(response.StatusCode);
			Console.WriteLine(response.Body);
		}
	}
}