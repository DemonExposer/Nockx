using Org.BouncyCastle.Crypto.Parameters;

namespace SecureChat.Panels; 

public interface IContentPanel {
	public void Show(RsaKeyParameters publicKey, MainWindow context);
}
