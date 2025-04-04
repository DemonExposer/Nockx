using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Org.BouncyCastle.Crypto.Parameters;
using SecureChat.ClassExtensions;

namespace SecureChat;

public class Chats {
	private string _file;
	private readonly Dictionary<RsaKeyParameters, string> _keyNameBindings = [];

	private static Chats? _instance = null;

	private Chats() {
		
	}
	
	private Chats(string filename, Dictionary<RsaKeyParameters, string> keyNameBindings) {
		_file = filename;
		_keyNameBindings = keyNameBindings;
	}

	public static void Add(RsaKeyParameters publicKey, string name) {
		(_instance ?? throw new InvalidOperationException("Chats must be loaded before modifications can be made"))._keyNameBindings[publicKey] = name;
		Save();
	}

	public static bool ChatExists(RsaKeyParameters publicKey) {
		if (_instance == null)
			throw new InvalidOperationException("Chats must be loaded before checks can be performed");

		return _instance._keyNameBindings.ContainsKey(publicKey);
	}

	public static void LoadOrDefault(string filename) {
		if (File.Exists(filename)) {
			Load(filename);
			return;
		}

		_instance = new Chats {
			_file = filename
		};
		
		Save();
	}
	
	public static void Load(string filename) {
		JsonArray chatsArray = JsonNode.Parse(File.ReadAllText(filename))!.AsArray();
		Dictionary<RsaKeyParameters, string> keyNameBindings = [];
		foreach (JsonNode? chat in chatsArray)
			keyNameBindings[RsaKeyParametersExtension.FromBase64String(chat!["publicKey"]!.GetValue<string>())] = chat["name"]!.GetValue<string>();
		_instance = new Chats(filename, keyNameBindings);
	}

	private static void Save() {
		JsonArray chats = [];
		foreach (RsaKeyParameters pubKey in (_instance ?? throw new InvalidOperationException("Chats must loaded before they can be saved"))._keyNameBindings.Keys)
			chats.Add(new JsonObject {
				["publicKey"] = pubKey.ToBase64String(),
				["name"] = _instance._keyNameBindings[pubKey]
			});
		
		File.WriteAllText(_instance._file, JsonSerializer.Serialize(chats, new JsonSerializerOptions { WriteIndented = true }));
	}

	public static void Show(MainWindow window) {
		foreach (RsaKeyParameters key in (_instance ?? throw new InvalidOperationException("Chats must loaded before they can be shown"))._keyNameBindings.Keys)
			window.AddUser(key, _instance._keyNameBindings[key], false);
	}
}