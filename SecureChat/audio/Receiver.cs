using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;

namespace SecureChat.audio;

public class Receiver {
	private int _sourceId;

	public IEnumerable<string> Devices;
	public ALDevice _device;
	private readonly List<int> _buffers = [];

	private bool _doneOnce;
	
	public Receiver() {
		Devices = ALC.GetString(AlcGetStringList.DeviceSpecifier);
		_device = ALC.OpenDevice(Devices.FirstOrDefault());
	}
	
	public void OnReceive(IPEndPoint endPoint, byte[] data) {
		try {
			const int frequency = 44100; // Sampling frequency
			const ALFormat format = ALFormat.Mono16; // 16-bit mono
			const int bufferSize = 44100; // 1 second of audio
			short[] buffer = new short[bufferSize];

			Buffer.BlockCopy(data, 0, buffer, 0, data.Length);
			if (!_doneOnce) {
				Task.Run(() => PlayAudio(buffer, data.Length));
				_doneOnce = true;
			} else {
				AddBuffer(buffer, data.Length);
			}
		} catch (Exception e) {
			Console.WriteLine(e);
		}
	}

	private unsafe void AddBuffer(short[] buffer, int lengthInBytes) {
		AL.GetSource(_sourceId, ALGetSourcei.BuffersProcessed, out int amount);
		if (amount > 0) {
			AL.SourceUnqueueBuffers(_sourceId, amount, _buffers.ToArray());
			AL.DeleteBuffers(amount, _buffers.ToArray());
			_buffers.RemoveRange(0, amount);
		}
		
		int bufferId = AL.GenBuffer();
		
		fixed (short *bufferPointer = buffer)
			AL.BufferData(bufferId, ALFormat.Mono16, bufferPointer, lengthInBytes, 44100);
		
		AL.SourceQueueBuffer(_sourceId, bufferId);
		ALError error = AL.GetError();
		if (error != ALError.NoError)
			Console.WriteLine($"AddBuffer {error}");
		else
			_buffers.Add(bufferId);

		if ((ALSourceState) AL.GetSource(_sourceId, ALGetSourcei.SourceState) != ALSourceState.Playing)
			AL.SourcePlay(_sourceId);
	}

	private unsafe void PlayAudio(short[] buffer, int lengthInBytes) {
		try {
			ALContext context = ALC.CreateContext(_device, new ALContextAttributes(44100,1,0,200,false));
			ALC.MakeContextCurrent(context);
		
			int bufferId = AL.GenBuffer();
			_sourceId = AL.GenSource();
		
			ALError error = AL.GetError();
			if (error != ALError.NoError)
				Console.WriteLine($"{error}");
		
			// Set buffer format and frequency (Mono 16-bit)
			fixed (short *bufferPointer = buffer)
				AL.BufferData(bufferId, ALFormat.Mono16, bufferPointer, lengthInBytes, 44100);
		
			error = AL.GetError();
			if (error != ALError.NoError)
				Console.WriteLine($"{error}");
        
			// Bind the buffer to the source
			AL.SourceQueueBuffer(_sourceId, bufferId);
		
			error = AL.GetError();
			if (error != ALError.NoError)
				Console.WriteLine($"{error}");
        
			// Start playback
			AL.SourcePlay(_sourceId);
			Console.WriteLine("Playing recorded audio...");

			error = AL.GetError();
			if (error != ALError.NoError)
				Console.WriteLine($"{error}");
		
			// Wait for the playback to finish
			ALSourceState state;
			ManualResetEvent manualResetEvent = new (false);
			manualResetEvent.WaitOne();
			//	do {
			//		state = (ALSourceState) AL.GetSource(sourceId, ALGetSourcei.SourceState);
			//	} while (true);
        
			Console.WriteLine("Playback finished.");
        
			// Cleanup
			AL.DeleteSource(_sourceId);
			AL.DeleteBuffer(bufferId);

			ALC.CloseDevice(_device);
		} catch (Exception e) {
			Console.WriteLine(e);
		}
	}
}