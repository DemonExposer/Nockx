using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SecureChat.Util;

public class FriendRequest {
	public required string SenderKey, ReceiverKey, SenderName, ReceiverName;
	public bool Accepted;
	public required long Timestamp;

	public static FriendRequest? Parse(JsonObject jsonFriendRequest) {
		FriendRequest? friendRequest;
		try {
			friendRequest = new FriendRequest() {
				SenderKey = jsonFriendRequest["sender"]!["key"]!.GetValue<string>(),
				SenderName = jsonFriendRequest["sender"]!["displayName"]!.GetValue<string>(),
				ReceiverKey = jsonFriendRequest["receiver"]!["key"]!.GetValue<string>(),
				ReceiverName = jsonFriendRequest["receiver"]!["displayName"]!.GetValue<string>(),
				Accepted = jsonFriendRequest["accepted"]?.GetValue<bool>() ?? false,
				Timestamp = jsonFriendRequest["timestamp"]!.GetValue<long>()
			};
		} catch (Exception e) {
			Console.WriteLine(e);
			friendRequest = null;
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
			["accepted"] = friendRequest.Accepted,
			["timestamp"] = friendRequest.Timestamp
		};
		return JsonSerializer.Serialize(friendRequestObj);
	}
}