namespace SecureChat;

public class MainWindowController {
	private readonly string _senderPublicKey, _receiverPublicKey;
	
	public MainWindowController(string senderPublicKey, string receiverPublicKey) {
		_senderPublicKey = senderPublicKey;
		_receiverPublicKey = receiverPublicKey;
	}
	
	public void Send(string message) {
		
	}
}