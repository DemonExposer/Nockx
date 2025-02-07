using OpenTK.Audio.OpenAL;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SecureChat.audio;

public class Sound {
	private readonly int _audioFormat;
	private readonly int _numChannels;
	private readonly int _sampleRate;
	private readonly ALFormat _format;
	private readonly byte[] _data;

	private bool _doStopRepeat;
	private int _source;

	public Sound(string filename) {
		using BinaryReader reader = new (File.Open("sounds/" + filename, FileMode.Open));
		reader.ReadBytes(4);
		reader.ReadInt32();
		reader.ReadBytes(4);
		reader.ReadBytes(4);
		reader.ReadInt32();
		_audioFormat = reader.ReadInt16();
		_numChannels = reader.ReadInt16();
		_sampleRate = reader.ReadInt32();
		reader.ReadInt32();
		reader.ReadInt16();
		int bitsPerSample = reader.ReadInt16();

		_format = (_numChannels, bitsPerSample) switch {
			(1, 8) => ALFormat.Mono8,
			(1, 16) => ALFormat.Mono16,
			(2, 8) => ALFormat.Stereo8,
			(2, 16) => ALFormat.Stereo16,
			_ => throw new NotSupportedException("Unsupported WAV format")
		};

		reader.ReadBytes(4);
		int dataSize = reader.ReadInt32();
		_data = reader.ReadBytes(dataSize);
	}

	public unsafe void Play() {
		Task.Run(() => {
			try {
				IEnumerable<string> devices = ALC.GetString(AlcGetStringList.DeviceSpecifier);
				ALDevice device = ALC.OpenDevice(devices.FirstOrDefault());

				ALContext context = ALC.CreateContext(device, new ALContextAttributes(44100, _numChannels == 1 ? 1 : 0, _numChannels == 2 ? 1 : 0, 200, false));
				ALC.MakeContextCurrent(context);

				int buffer = AL.GenBuffer();
				int source = AL.GenSource();

				fixed (void* dataPointer = _data)
					AL.BufferData(buffer, _format, dataPointer, _data.Length, _sampleRate);

				AL.Source(source, ALSourcei.Buffer, buffer);

				AL.SourcePlay(source);

				Thread.Sleep((int) ((long) _data.Length * 1010 / _sampleRate));

				AL.DeleteSource(source);
				AL.DeleteBuffer(buffer);
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		});
	}

	public unsafe void Repeat() {
		_doStopRepeat = false;
		Task.Run(() => {
			try {
				IEnumerable<string> devices = ALC.GetString(AlcGetStringList.DeviceSpecifier);
				ALDevice device = ALC.OpenDevice(devices.FirstOrDefault());

				ALContext context = ALC.CreateContext(device, new ALContextAttributes(44100, _numChannels == 1 ? 1 : 0, _numChannels == 2 ? 1 : 0, 200, false));
				ALC.MakeContextCurrent(context);

				int buffer = AL.GenBuffer();
				_source = AL.GenSource();

				fixed (void* dataPointer = _data)
					AL.BufferData(buffer, _format, dataPointer, _data.Length, _sampleRate);

				AL.Source(_source, ALSourcei.Buffer, buffer);

				while (!_doStopRepeat) {
					AL.SourceRewind(_source);
					AL.SourcePlay(_source);

					Thread.Sleep((int) ((long) _data.Length * 1000 / _sampleRate));
				}

				AL.DeleteSource(_source);
				AL.DeleteBuffer(buffer);
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		});
	}

	public void StopRepeat() {
		_doStopRepeat = true;
		AL.SourceStop(_source);
	}
}
