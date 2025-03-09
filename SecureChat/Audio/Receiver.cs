using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;

namespace SecureChat.Audio;

public class Receiver {
	private int _sourceId;
	private float _volumeFactor = 1F;

	private readonly List<int> _buffers = [];

	private bool _doneOnce;

	private short _lastSample = 0;
	
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
		for (int i = 0; i < lengthInBytes; i += 2) {
			int amplified = (int) (buffer[i/2] * _volumeFactor);
			buffer[i/2] = (short) Math.Max(short.MinValue, Math.Min(short.MaxValue, amplified));
		}

		// TODO: possibly do post-buffer smoothing as well by always adding data as an extra buffer to the end, and remove it when a new buffer gets added and the audio is still playing

		if ((ALSourceState) AL.GetSource(_sourceId, ALGetSourcei.SourceState) != ALSourceState.Playing)
			_lastSample = 0;

		// Smooth from last played sample to next sample in case they are too far apart
		short sampleDistance = (short) (buffer[0] - _lastSample);
		short amountSmoothingSamples = (short) (Math.Abs(sampleDistance) / 3276); // if the distance is less than 3276, nothing will be done because the for loop will go from 0 to 0
		short smoothingStep = (short) (sampleDistance / (amountSmoothingSamples + 1));
		short[] smoothedBuffer = new short[amountSmoothingSamples + lengthInBytes/2];
		for (int i = 0; i < amountSmoothingSamples; i++)
			smoothedBuffer[i] = (short) (_lastSample + (i+1) * smoothingStep);

		Buffer.BlockCopy(buffer, 0, smoothedBuffer, amountSmoothingSamples*2, lengthInBytes);
		lengthInBytes += amountSmoothingSamples * 2;

		_lastSample = buffer[lengthInBytes/2 - 1];

		AL.GetSource(_sourceId, ALGetSourcei.BuffersProcessed, out int amount);
		if (amount > 0) {
			AL.SourceUnqueueBuffers(_sourceId, amount, _buffers.ToArray());
			AL.DeleteBuffers(amount, _buffers.ToArray());
			_buffers.RemoveRange(0, amount);
		}
		
		int bufferId = AL.GenBuffer();
		
		fixed (short *bufferPointer = smoothedBuffer)
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

			error = AL.GetError();
			if (error != ALError.NoError)
				Console.WriteLine($"{error}");
		
			// Wait for the playback to finish
			ManualResetEvent manualResetEvent = new (false);
			manualResetEvent.WaitOne();
        
			// Cleanup
			AL.DeleteSource(_sourceId);
			AL.DeleteBuffer(bufferId);
		} catch (Exception e) {
			Console.WriteLine(e);
		}
	}

	public void SetVolume(float volume) {
		AL.Source(_sourceId, ALSourcef.Gain, Math.Min(1F, volume));
		_volumeFactor = Math.Max(1F, volume);
	}
}