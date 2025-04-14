using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SecureChat.CustomControls; 

public partial class Spinner : Image {
	private List<Bitmap> _frames = [];
	private bool _isSpinning;

	public Spinner() {
		InitializeComponent();
	}

	private void InitializeComponent() {
		for (int i = 0; File.Exists($"animations/spinner/{i}.png"); i++)
			_frames.Add(new Bitmap($"animations/spinner/{i}.png"));
	}

	public void Spin() {
		if (_frames.Count == 0)
			throw new IndexOutOfRangeException(); // Handle this more properly by checking if the animations/spinner folder exists (and if there are files present) and notifying the user if not

		if (_isSpinning)
			return;

		_isSpinning = true;

		int i = 0;
		Timer timer = new (_ => {
			try {
				lock (this) {
					Dispatcher.UIThread.Invoke(() => {
						Source = _frames[i++];
					});
					i %= _frames.Count;
				}
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		}, null, 0, 1000/60);

		// For disposing and also to cancel garbage collection, the timer needs to be stored
		((App) App.Current!).AddTimer(timer);
	}
}
