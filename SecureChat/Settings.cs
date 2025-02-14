using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using SecureChat.Windows;

namespace SecureChat;

public class Settings {
	private string _file;
	public IPAddress IpAddress { get; set; }
	
	private static Settings? _instance = null;

	private Settings() {
		
	}

	public static void LoadOrDefault(string filename) {
		if (File.Exists(filename)) {
			Load(filename);
			return;
		}
		
		_instance = new Settings {
			_file = filename,
			IpAddress = IPAddress.Parse("127.0.0.1")
		};
		
		Save();
	}
	
	public static void Load(string filename) {
		JsonObject settingsObject = JsonNode.Parse(File.ReadAllText(filename))!.AsObject();

		IPAddress ipAddress;
		try {
			ipAddress = IPAddress.Parse(settingsObject["ipAddress"]!.GetValue<string>());
		} catch (Exception) {
			try {
				IPHostEntry hostEntry = Dns.GetHostEntry(settingsObject["ipAddress"]!.GetValue<string>());
				ipAddress = hostEntry.AddressList[0];
			} catch (SocketException) {
				new ErrorPopupWindow("Server domain name not found").Show(MainWindow.Instance);
				return;
			} catch (Exception) {
				new ErrorPopupWindow("Server IP address is not correct").Show(MainWindow.Instance);
				return;
			}
		}
		
		_instance = new Settings {
			_file = filename,
			IpAddress = ipAddress
		};
	}

	public static void Save() {
		File.WriteAllText((_instance ?? throw new InvalidOperationException("Settings must loaded before they can be saved"))._file, JsonSerializer.Serialize(new JsonObject {
			["ipAddress"] = _instance.IpAddress.ToString()
		}, new JsonSerializerOptions { WriteIndented = true }));
	}

	public static Settings GetInstance() => _instance ?? throw new InvalidOperationException("Settings must loaded before instance can be achieved");
}