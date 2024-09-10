using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace SecureChat;

public class Chats {
	private string _file;
	private readonly ISet<RsaKeyParameters> _pubKeys = new HashSet<RsaKeyParameters>();

	private static Chats? _instance = null;

	private Chats() {
		
	}
	
	private Chats(string filename, IEnumerable<RsaKeyParameters> publicKeys) {
		_file = filename;
		_pubKeys.UnionWith(publicKeys);
	}

	public static void Add(RsaKeyParameters publicKey) {
		(_instance ?? throw new InvalidOperationException("Chats must be loaded before modifications can be made"))._pubKeys.Add(publicKey);
		Save();
	}

	public static bool ChatExists(RsaKeyParameters publicKey) {
		if (_instance == null)
			throw new InvalidOperationException("Chats must be loaded before checks can be performed");

		return Enumerable.Contains(_instance._pubKeys, publicKey);
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
		_instance = new Chats(filename, chatsArray.ToArray().Select(node => 
			new RsaKeyParameters(
				false,
				new BigInteger(node!["publicKey"]!["modulus"]!.GetValue<string>(), 16),
				new BigInteger(node["publicKey"]!["exponent"]!.GetValue<string>(), 16)
			)
		));
	}

	private static void Save() {
		JsonArray chats = new ();
		foreach (RsaKeyParameters pubKey in (_instance ?? throw new InvalidOperationException("Chats must loaded before they can be saved"))._pubKeys)
			chats.Add(new JsonObject {
				["publicKey"] = new JsonObject {
					["modulus"] = pubKey.Modulus.ToString(16),
					["exponent"] = pubKey.Exponent.ToString(16)
				},
				["name"] = pubKey.Modulus.ToString(16)
			});
		
		File.WriteAllText(_instance._file, JsonSerializer.Serialize(chats, new JsonSerializerOptions { WriteIndented = true }));
	}

	public static void Show(MainWindow window) {
		foreach (RsaKeyParameters key in (_instance ?? throw new InvalidOperationException("Chats must loaded before they can be shown"))._pubKeys) {
			string name = key.Modulus.ToString(16);
			window.AddUser(key, name, false);
		}
	}
}