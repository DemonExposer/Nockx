using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using SecureChat.util;
using System.IO;

namespace SecureChat.panels;

public class UserInfoPanelController {
	public readonly RsaKeyParameters PublicKey;

	public UserInfoPanelController() {
		using (StreamReader reader = File.OpenText(Constants.PublicKeyFile)) {
			PemReader pemReader = new(reader);
			PublicKey = (RsaKeyParameters) pemReader.ReadObject();
		}
	}
}
