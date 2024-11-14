using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SecureChat.audio;

public class Network(Network.DataReceivedCallback dataReceivedCallback, IPEndPoint endpoint) {
	public delegate void DataReceivedCallback(IPEndPoint endPoint, byte[] data);

	private readonly UdpClient _socket = new ();

	public void Run() {
		Task.Run(() => {
			while (true) try {
				IPEndPoint endPoint = new (IPAddress.Any, 0);
				byte[] data = _socket.Receive(ref endPoint);
				
				dataReceivedCallback(endPoint, data);
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		});
	}

	public void Send(byte[] data, int length) => _socket.Send(data, length, endpoint);
}