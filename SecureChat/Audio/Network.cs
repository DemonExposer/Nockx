using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SecureChat.Util;

namespace SecureChat.Audio;

public class Network(Network.DataReceivedCallback dataReceivedCallback, IPEndPoint endpoint, Network.PingCallback pingCallback) {
	public delegate void DataReceivedCallback(IPEndPoint endPoint, byte[] data);
	public delegate void PingCallback(int port);

	private readonly UdpClient _socket = new ();
	private byte[]? _key;

	public void Run() {
		Task.Run(() => {
			IPEndPoint endPoint = new (IPAddress.Any, 0);
			
			try {
				string[] portAndHash;
				do {
					_socket.Send(Encoding.UTF8.GetBytes("PING"), 4, endpoint);

					// The port needs to be retrieved from the server, because in certain networks, the socket port here can differ from the socket port at the modem
					byte[] data = _socket.Receive(ref endPoint); // TODO: test what happens when the server doesn't respond
					portAndHash = Encoding.UTF8.GetString(data).Split(':');
				} while (Cryptography.Md5Hash(portAndHash[0]) != portAndHash[1]); // Verify port, because it's sent over UDP
				
				pingCallback(int.Parse(portAndHash[0]));
			} catch (Exception e) {
				Console.WriteLine(e);
			}
			
			while (true) try {
				byte[] data = _socket.Receive(ref endPoint);

				if (_key == null)
					continue;

				(byte[] plainBytes, int length) = Cryptography.DecryptWithAes(data, _key);

				dataReceivedCallback(endPoint, plainBytes.Take(length).ToArray());
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		});
	}

	public void Send(byte[] data, int length) {
		if (_key == null)
			return;

		byte[] cipherData = Cryptography.EncryptWithAes(data, length, _key);
		
		_socket.Send(cipherData, cipherData.Length, endpoint);
	}

	public void SetKey(byte[] key) => _key = key;
}