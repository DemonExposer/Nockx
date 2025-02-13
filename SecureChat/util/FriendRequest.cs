using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SecureChat.util;

public class FriendRequest {
	public string SenderModulus, SenderExponent, ReceiverModulus, ReceiverExponent, SenderName, ReceiverName;
	public bool Accepted;

	public static FriendRequest Parse(JsonObject jsonFriendRequest) {
		FriendRequest friendRequest;
		try {
			friendRequest = new FriendRequest() {
				SenderModulus = jsonFriendRequest["sender"]!["modulus"]!.GetValue<string>(),
				SenderExponent = jsonFriendRequest["sender"]!["exponent"]!.GetValue<string>(),
				SenderName = jsonFriendRequest["sender"]!["displayName"]!.GetValue<string>(),
				ReceiverModulus = jsonFriendRequest["receiver"]!["modulus"]!.GetValue<string>(),
				ReceiverExponent = jsonFriendRequest["receiver"]!["exponent"]!.GetValue<string>(),
				ReceiverName = jsonFriendRequest["receiver"]!["displayName"]!.GetValue<string>(),
				Accepted = jsonFriendRequest["accepted"]?.GetValue<bool>() ?? false
			};
		} catch (Exception e) {
			Console.WriteLine(e);
			friendRequest = new FriendRequest();
		}
		return friendRequest;
	}

	public static string Serialize(FriendRequest friendRequest) {
		JsonObject friendRequestObj = new () {
			["sender"] = new JsonObject {
				["modulus"] = friendRequest.SenderModulus,
				["exponent"] = friendRequest.SenderExponent,
				["displayName"] = friendRequest.SenderName
			},
			["receiver"] = new JsonObject {
				["modulus"] = friendRequest.ReceiverModulus,
				["exponent"] = friendRequest.ReceiverExponent,
				["displayName"] = friendRequest.ReceiverName
			},
			["accepted"] = friendRequest.Accepted
		};
		return JsonSerializer.Serialize(friendRequestObj);
	}
}