using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Audio.OpenAL;

namespace SecureChat.audio;

public class Sender {
	public static void Run(Network network) {
		Task.Run(() => {
			while (true) try {
				const int frequency = 44100; // Sampling frequency
				const ALFormat format = ALFormat.Mono16; // 16-bit mono
				const int bufferSize = 44100; // 1 second of audio
				short[] buffer = new short[bufferSize];

				UdpClient client = new ();

				string deviceName = ALC.GetString(ALDevice.Null, AlcGetString.CaptureDeviceSpecifier);
				Console.WriteLine(deviceName);
				ALCaptureDevice captureDevice = ALC.CaptureOpenDevice(deviceName, frequency, format, bufferSize);

				if (captureDevice == IntPtr.Zero) {
					Console.WriteLine("Failed to open capture device.");
					return;
				}

				ALC.CaptureStart(captureDevice);

				while (true) {
					Console.WriteLine("Recording...");

					// Wait for the capture buffer to be filled
					int samplesAvailable = 0;
					ALC.GetInteger(new ALDevice(captureDevice), AlcGetInteger.CaptureSamples, out samplesAvailable);

					while (samplesAvailable * sizeof(short) < 2048) {
						ALC.GetInteger(new ALDevice(captureDevice), AlcGetInteger.CaptureSamples, out samplesAvailable);
						Thread.Sleep(10);
					}

					// Capture samples from the microphone
					ALC.CaptureSamples(captureDevice, buffer, samplesAvailable);
					Console.WriteLine($"Captured {samplesAvailable * sizeof(short)} samples.");

					byte[] byteArray = new byte[buffer.Length * sizeof(short)];
					Buffer.BlockCopy(buffer, 0, byteArray, 0, byteArray.Length);

					// Save the captured audio to a WAV file
					//	SaveToWav(buffer, samplesAvailable, 44100);
					network.Send(byteArray, samplesAvailable * sizeof(short));
					//	PlayAudio(buffer);
					//	Console.WriteLine("Recording saved as recordedAudio.wav");
				}

				client.Close();
				ALC.CaptureStop(captureDevice);

				ALC.CaptureCloseDevice(captureDevice);
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		});
	}
}