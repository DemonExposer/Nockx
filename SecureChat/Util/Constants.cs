using Org.BouncyCastle.Math;

namespace SecureChat.Util;

public static class Constants {
	public const string PrivateKeyFile = "private_key.pem";
	public const string PublicKeyFile = "public_key.pem";
	public const string ChatsFile = "chats.json";
	public const string SettingsFile = "settings.json";

	public const int KEY_SIZE_BITS = 2048;
	public const int KEY_SIZE_BYTES = KEY_SIZE_BITS / 8;
	public static readonly BigInteger KEY_EXPONENT = new ("10001", 16);
}