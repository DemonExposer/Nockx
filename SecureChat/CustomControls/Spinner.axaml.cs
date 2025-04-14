using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SecureChat.CustomControls; 

public partial class Spinner : Border {
	private List<Bitmap> _frames = [];
	private Bitmap _checkmark;
	private bool _isSpinning;
	private Timer? _timer;

	private Image _innerImage;

	public Spinner() {
		AvaloniaXamlLoader.Load(this);
		InitializeComponent();

		_innerImage = this.FindControl<Image>("PART_Image")!;
	}

	private void InitializeComponent() {
		for (int i = 0; File.Exists($"animations/spinner/{i}.png"); i++)
			_frames.Add(new Bitmap($"animations/spinner/{i}.png"));

		_checkmark = new Bitmap("animations/spinner/checkmark.png");
	}

	public void Spin() {
		if (_frames.Count == 0)
			throw new IndexOutOfRangeException(); // Handle this more properly by checking if the animations/spinner folder exists (and if there are files present) and notifying the user if not

		if (_isSpinning)
			return;

		_isSpinning = true;

		int i = 0;
		_timer = new (_ => {
			Dispatcher.UIThread.InvokeAsync(() => {
				lock (this) {
					_innerImage.Source = _frames[i++];
					i %= _frames.Count;
				}
			});
		}, null, 0, 1000/60);

		// For disposing and also to cancel garbage collection, the timer needs to be stored
		((App) App.Current!).AddTimer(_timer);
	}

	public void Success() {
		if (_isSpinning) {
			_timer.Dispose();
			_isSpinning = false;
		}

		Dispatcher.UIThread.InvokeAsync(() => _innerImage.Source = _checkmark);
	}
}
