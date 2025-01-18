using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SecureChat.audio;

public class Network(Network.DataReceivedCallback dataReceivedCallback, IPEndPoint endpoint, Network.PingCallback pingCallback) {
	public delegate void DataReceivedCallback(IPEndPoint endPoint, byte[] data);
	public delegate void PingCallback(int port);

	private readonly UdpClient _socket = new ();

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

				if (data.Length == 4 && data.SequenceEqual(Encoding.UTF8.GetBytes("PONG"))) {
					continue;
				}

				dataReceivedCallback(endPoint, data);
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		});
	}

	public void Send(byte[] data, int length) => _socket.Send(data, length, endpoint);
}