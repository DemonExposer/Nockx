using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SecureChat.util;

namespace SecureChat.audio;

public class Network(Network.DataReceivedCallback dataReceivedCallback, IPEndPoint endpoint, Network.PingCallback pingCallback) {
	public delegate void DataReceivedCallback(IPEndPoint endPoint, byte[] data);
	public delegate void PingCallback(int port);

	private readonly UdpClient _socket = new ();
	private byte[]? _key;

	public void Run() {
		Task.Run(() => {
			try {
				_socket.Send(Encoding.UTF8.GetBytes("PING"), 4, endpoint);
				pingCallback(((IPEndPoint) _socket.Client.LocalEndPoint!).Port);
			} catch (Exception e) {
				Console.WriteLine(e);
			}
			
			while (true) try {
				IPEndPoint endPoint = new (IPAddress.Any, 0);
				byte[] data = _socket.Receive(ref endPoint);

				if (data.Length == 4 && data.SequenceEqual(Encoding.UTF8.GetBytes("PONG")))
					continue;

				if (_key == null)
					continue;

				byte[] plainBytes = Cryptography.DecryptWithAes(data, _key);

				dataReceivedCallback(endPoint, plainBytes);
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