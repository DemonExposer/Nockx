using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SecureChat.util;

public class FriendRequest {
	public string SenderKey, ReceiverKey, SenderName, ReceiverName;
	public bool Accepted;

	public static FriendRequest Parse(JsonObject jsonFriendRequest) {
		FriendRequest friendRequest;
		try {
			friendRequest = new FriendRequest() {
				SenderKey = jsonFriendRequest["sender"]!["key"]!.GetValue<string>(),
				SenderName = jsonFriendRequest["sender"]!["displayName"]!.GetValue<string>(),
				ReceiverKey = jsonFriendRequest["receiver"]!["key"]!.GetValue<string>(),
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
				["key"] = friendRequest.SenderKey,
				["displayName"] = friendRequest.SenderName
			},
			["receiver"] = new JsonObject {
				["key"] = friendRequest.ReceiverKey,
				["displayName"] = friendRequest.ReceiverName
			},
			["accepted"] = friendRequest.Accepted
		};
		return JsonSerializer.Serialize(friendRequestObj);
	}
}