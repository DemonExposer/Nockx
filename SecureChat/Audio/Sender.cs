using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using OpenTK.Audio.OpenAL;
using SecureChat.CustomControls;

namespace SecureChat.Audio;

public class Sender {
	public IEnumerable<string> Devices;
	public double NoiseGateThresholdPercent;
	
	private const int Frequency = 44100;
	private const ALFormat Format = ALFormat.Mono16;
	private const int BufferSize = 44100; // 1 second of audio

	private bool _doChangeDevice, _isVoiceChatClosed;
	private int _newDeviceIndex = -1;
	private ALCaptureDevice _captureDevice;

	private readonly List<byte[]> _bufferBuffer = [];

	private readonly NoiseGateSlider _volumeBar;
	
	public Sender(NoiseGateSlider volumeBar) {
		_volumeBar = volumeBar;

		Devices = ALC.GetString(ALDevice.Null, AlcGetStringList.CaptureDeviceSpecifier);
		_captureDevice = ALC.CaptureOpenDevice(Devices.FirstOrDefault(), Frequency, Format, BufferSize);
	}
    
	public void Run(Network network) {
		Task.Run(() => {
			while (true) try {
				short[] buffer = new short[BufferSize];
				
				if (_captureDevice == IntPtr.Zero) {
					Console.WriteLine("Failed to open capture device.");
					return;
				}

				ALC.CaptureStart(_captureDevice);

				int noiseGateDecay = 0;
				while (true) {
					Console.WriteLine("Recording...");

					// Wait for the capture buffer to be filled
					ALC.GetInteger(new ALDevice(_captureDevice), AlcGetInteger.CaptureSamples, out int samplesAvailable);

					while (samplesAvailable * sizeof(short) < 2048) {
						ALC.GetInteger(new ALDevice(_captureDevice), AlcGetInteger.CaptureSamples, out samplesAvailable);
						Thread.Sleep(10);
					}

					// Capture samples from the microphone
					ALC.CaptureSamples(_captureDevice, buffer, samplesAvailable);
					Console.WriteLine($"Captured {samplesAvailable * sizeof(short)} samples.");

					byte[] byteArray = new byte[buffer.Length * sizeof(short)];
					Buffer.BlockCopy(buffer, 0, byteArray, 0, byteArray.Length);
					
					long average = 0;
					for (int i = 0; i < samplesAvailable; i++)
						average += Math.Abs(Math.Max(buffer[i], (short) -32767));

					average /= samplesAvailable;
					average = (long) (average * Math.Sqrt(2)); // Convert from RMS to peak-to-center
					
					byte volumePercentLinear = Math.Min((byte) 100, (byte) (average*100 / 32767)); // Clamp it between 0 and 100 because of possible averaging errors
					byte volumePercentLogarithmic = (byte) (Math.Log10(volumePercentLinear) * 50);
					Dispatcher.UIThread.InvokeAsync(() => _volumeBar.VolumePercent = volumePercentLogarithmic);

					if (volumePercentLogarithmic >= NoiseGateThresholdPercent)
						noiseGateDecay = 20;

					if (noiseGateDecay > 0) {
						_bufferBuffer.ForEach(buf => network.Send(buf, buf.Length));
						_bufferBuffer.Clear();
						
						network.Send(byteArray, samplesAvailable * sizeof(short));
						noiseGateDecay--;
					} else {
						_bufferBuffer.Add(byteArray[..(samplesAvailable * sizeof(short))]);
						if (_bufferBuffer.Count > 10) // Add a delay of 10 buffers, which will be sent retroactively if the noise gate threshold is broken, to provide smoothing
							_bufferBuffer.RemoveAt(0);
					}

					if (_doChangeDevice) {
						_doChangeDevice = false;
						ALC.CaptureStop(_captureDevice);
						ALC.CaptureCloseDevice(_captureDevice);

						_captureDevice = ALC.CaptureOpenDevice(Devices.ToArray()[_newDeviceIndex], Frequency, Format, BufferSize);
						_newDeviceIndex = -1;
						break;
					}
					
					if (_isVoiceChatClosed) {
						ALC.CaptureStop(_captureDevice);
						ALC.CaptureCloseDevice(_captureDevice);
						return;
					}
				}
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		});
	}

	public void SetInputDevice(int index) {
		_newDeviceIndex = index;
		_doChangeDevice = true;
	}

	public void Close() => _isVoiceChatClosed = true;
}