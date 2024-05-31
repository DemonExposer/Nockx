using System.Text.Json.Nodes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace SecureChat.util;

public class Message {
	public string Body, ReceiverEncryptedKey, SenderEncryptedKey, Signature;
	public RsaKeyParameters Sender, Receiver;
	
	public static Message Parse(JsonObject jsonMessage) {
		return new Message {
			Body = jsonMessage["text"]!.GetValue<string>(),
			ReceiverEncryptedKey = jsonMessage["receiverEncryptedKey"]!.GetValue<string>(),
			SenderEncryptedKey = jsonMessage["senderEncryptedKey"]!.GetValue<string>(),
			Signature = jsonMessage["signature"]!.GetValue<string>(),
			Sender = new RsaKeyParameters(false, new BigInteger(jsonMessage["sender"]!["modulus"]!.GetValue<string>(), 16), new BigInteger(jsonMessage["sender"]!["exponent"]!.GetValue<string>(), 16)),
			Receiver = new RsaKeyParameters(false, new BigInteger(jsonMessage["receiver"]!["modulus"]!.GetValue<string>(), 16), new BigInteger(jsonMessage["receiver"]!["exponent"]!.GetValue<string>(), 16))
		};
	}
}