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
			try {
				_socket.Send(Encoding.UTF8.GetBytes("PING"), 4, endpoint);
				IPEndPoint endPoint = new (IPAddress.Any, 0);
				byte[] data = _socket.Receive(ref endPoint);
				pingCallback(int.Parse(Encoding.UTF8.GetString(data))); // TODO: this is not reliable, since it's UDP. possibly check it against a hash.
			} catch (Exception e) {
				Console.WriteLine(e);
			}
			
			while (true) try {
				IPEndPoint endPoint = new (IPAddress.Any, 0);
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