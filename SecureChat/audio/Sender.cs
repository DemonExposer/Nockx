using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;

namespace SecureChat.audio;

public class Sender {
	public IEnumerable<string> Devices;
	public ALCaptureDevice _captureDevice;
	
	private const int Frequency = 44100;
	private const ALFormat Format = ALFormat.Mono16;
	private const int BufferSize = 44100; // 1 second of audio

	private bool _doChangeDevice;
	private int _newDeviceIndex = -1;
	
	public Sender() {
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

				while (true) {
					Console.WriteLine("Recording...");

					// Wait for the capture buffer to be filled
					int samplesAvailable = 0;
					ALC.GetInteger(new ALDevice(_captureDevice), AlcGetInteger.CaptureSamples, out samplesAvailable);

					while (samplesAvailable * sizeof(short) < 2048) {
						ALC.GetInteger(new ALDevice(_captureDevice), AlcGetInteger.CaptureSamples, out samplesAvailable);
						Thread.Sleep(10);
					}

					// Capture samples from the microphone
					ALC.CaptureSamples(_captureDevice, buffer, samplesAvailable);
					Console.WriteLine($"Captured {samplesAvailable * sizeof(short)} samples.");

					byte[] byteArray = new byte[buffer.Length * sizeof(short)];
					Buffer.BlockCopy(buffer, 0, byteArray, 0, byteArray.Length);

					// Save the captured audio to a WAV file
					//	SaveToWav(buffer, samplesAvailable, 44100);
					network.Send(byteArray, samplesAvailable * sizeof(short));
					//	PlayAudio(buffer);
					//	Console.WriteLine("Recording saved as recordedAudio.wav");

					if (_doChangeDevice) {
						_doChangeDevice = false;
						ALC.CaptureStop(_captureDevice);
						ALC.CaptureCloseDevice(_captureDevice);

						_captureDevice = ALC.CaptureOpenDevice(Devices.ToArray()[_newDeviceIndex], Frequency, Format, BufferSize);
						_newDeviceIndex = -1;
						break;
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
}