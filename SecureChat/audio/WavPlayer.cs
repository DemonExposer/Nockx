using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SecureChat.audio;
public static class WavPlayer {
	public static unsafe void PlayFile(string file) {
		Task.Run(() => {
			using BinaryReader reader = new (File.Open(file, FileMode.Open));
			reader.ReadBytes(4);
			reader.ReadInt32();
			reader.ReadBytes(4);
			reader.ReadBytes(4);
			reader.ReadInt32();
			int audioFormat = reader.ReadInt16();
			int numChannels = reader.ReadInt16();
			int sampleRate = reader.ReadInt32();
			reader.ReadInt32();
			reader.ReadInt16();
			int bitsPerSample = reader.ReadInt16();

			reader.ReadBytes(4);
			int dataSize = reader.ReadInt32();
			byte[] data = reader.ReadBytes(dataSize);

			IEnumerable<string> devices = ALC.GetString(AlcGetStringList.DeviceSpecifier);
			ALDevice device = ALC.OpenDevice(devices.FirstOrDefault());

			ALFormat format = (numChannels, bitsPerSample) switch {
				(1, 8) => ALFormat.Mono8,
				(1, 16) => ALFormat.Mono16,
				(2, 8) => ALFormat.Stereo8,
				(2, 16) => ALFormat.Stereo16,
				_ => throw new NotSupportedException("Unsupported WAV format")
			};

			ALContext context = ALC.CreateContext(device, new ALContextAttributes(44100, numChannels == 1 ? 1 : 0, numChannels == 2 ? 1 : 0, 200, false));
			ALC.MakeContextCurrent(context);

			int buffer = AL.GenBuffer();
			int source = AL.GenSource();

			fixed (void* dataPointer = data)
				AL.BufferData(buffer, format, dataPointer, data.Length, sampleRate);

			AL.Source(source, ALSourcei.Buffer, buffer);

			AL.SourcePlay(source);

			Thread.Sleep((int) ((long) data.Length * 1010 / sampleRate));

			AL.DeleteSource(source);
			AL.DeleteBuffer(buffer);
		});
	}
}
