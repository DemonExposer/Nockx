using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using QRCoder;
using System.IO;

namespace SecureChat.Windows; 

public partial class ConnectAppWindow : PopupWindow {
	private readonly Image _image;

	public ConnectAppWindow() {
		InitializeComponent();

		_image = this.FindControl<Image>("QrCodeImage")!;
		_image.Focusable = true;
		Opened += (_, _) => _image.Focus();
		KeyDown += ConnectAppWindow_KeyDown;
	}

	private void ConnectAppWindow_KeyDown(object? sender, KeyEventArgs e) {
		if (e.Key != Key.Escape)
			return;

		Close();
	}

	public void SetQrCode(string data) {
		QRCodeGenerator qrGenerator = new ();
		QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
		PngByteQRCode qrCode = new (qrCodeData);
		
		byte[] qrCodeBytes = qrCode.GetGraphic(20);
		
		using MemoryStream stream = new (qrCodeBytes);
		Bitmap bitmap = new (stream);
		_image.Source = bitmap;
	}
}
