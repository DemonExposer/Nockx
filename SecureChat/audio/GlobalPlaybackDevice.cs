using System.Collections.Generic;
using System.Linq;
using OpenTK.Audio.OpenAL;

namespace SecureChat.audio;

public static class GlobalPlaybackDevice {
	private static ALDevice _device;
	
	public static void Initialize() {
		IEnumerable<string> devices = ALC.GetString(AlcGetStringList.DeviceSpecifier);
		_device = ALC.OpenDevice(devices.FirstOrDefault());

		ALContext context = ALC.CreateContext(_device, new ALContextAttributes(44100, 16, 16, 200, false));
		ALC.MakeContextCurrent(context);
	}

	public static void Close() => ALC.CloseDevice(_device);
}