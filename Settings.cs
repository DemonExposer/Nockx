using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

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
		_instance = new Settings {
			_file = filename,
			IpAddress = IPAddress.Parse(settingsObject["ipAddress"]!.GetValue<string>())
		};
	}

	public static void Save() {
		File.WriteAllText((_instance ?? throw new InvalidOperationException("Settings must loaded before they can be saved"))._file, JsonSerializer.Serialize(new JsonObject {
			["ipAddress"] = _instance.IpAddress.ToString()
		}, new JsonSerializerOptions { WriteIndented = true }));
	}

	public static Settings GetInstance() => _instance ?? throw new InvalidOperationException("Settings must loaded before instance can be achieved");
}