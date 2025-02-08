using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Audio.OpenAL;

namespace SecureChat.audio;

public static class GlobalDevice {
	public static void Initialize() {
		IEnumerable<string> devices = ALC.GetString(AlcGetStringList.DeviceSpecifier);
		ALDevice device = ALC.OpenDevice(devices.FirstOrDefault());

		ALContext context = ALC.CreateContext(device, new ALContextAttributes(44100, 16, 16, 200, false));
		ALC.MakeContextCurrent(context);
		Console.WriteLine("context made");
	}
}