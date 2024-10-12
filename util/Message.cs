using System;
using System.Text.Json.Nodes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace SecureChat.util;

public class Message {
	public long Id;
	public string Body, ReceiverEncryptedKey, SenderEncryptedKey, Signature;
	public RsaKeyParameters Sender, Receiver;
	public bool IsRead;
	
	public static Message Parse(JsonObject jsonMessage) {
		Message message;
		try {
			message = new Message {
				Id = jsonMessage["id"]!.GetValue<long>(),
				Body = jsonMessage["text"]!.GetValue<string>(),
				ReceiverEncryptedKey = jsonMessage["receiverEncryptedKey"]!.GetValue<string>(),
				SenderEncryptedKey = jsonMessage["senderEncryptedKey"]?.GetValue<string>(),
				Signature = jsonMessage["signature"]!.GetValue<string>(),
				Sender = new RsaKeyParameters(false, new BigInteger(jsonMessage["sender"]!["modulus"]!.GetValue<string>(), 16), new BigInteger(jsonMessage["sender"]!["exponent"]!.GetValue<string>(), 16)),
				Receiver = jsonMessage["receiver"] == null ? null : new RsaKeyParameters(false, new BigInteger(jsonMessage["receiver"]!["modulus"]!.GetValue<string>(), 16), new BigInteger(jsonMessage["receiver"]!["exponent"]!.GetValue<string>(), 16)),
				IsRead = jsonMessage["isRead"]!.GetValue<bool>()
			};
		} catch (Exception e) { // TODO: make this better. currently this is just easy for identifying issues between client and server
			Console.WriteLine(e.Message);
			
			if (jsonMessage["id"] == null)
				Console.WriteLine("id null");
			if (jsonMessage["text"] == null)
				Console.WriteLine("text null");
			if (jsonMessage["receiverEncryptedKey"] == null)
				Console.WriteLine("receiverEncryptedKey null");
			if (jsonMessage["senderEncryptedKey"] == null)
				Console.WriteLine("senderEncryptedKey null");
			if (jsonMessage["signature"] == null)
				Console.WriteLine("signature null");
			if (jsonMessage["sender"] == null)
				Console.WriteLine("sender null");
			if (jsonMessage["receiver"] == null)
				Console.WriteLine("receiver null");
			if (jsonMessage["isRead"] == null)
				Console.WriteLine("isRead null");
			
			message = new Message();
		}

		return message;
	}
}