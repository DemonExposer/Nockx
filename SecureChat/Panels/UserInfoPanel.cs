using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using System.Collections.Generic;
using System.IO;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using QRCoder;
using SecureChat.ClassExtensions;

namespace SecureChat.Panels;

public class UserInfoPanel : StackPanel {
	private UserInfoPanelController _controller = new ();

	public UserInfoPanel() {
		Dispatcher.UIThread.InvokeAsync(() => {
			TextBox? keyBox = null;

			using IEnumerator<ILogical> enumerator = this.GetLogicalChildren().GetEnumerator();
			while (enumerator.MoveNext()) {
				if (enumerator.Current.GetType() == typeof(TextBox)) {
					TextBox textBox = (TextBox) enumerator.Current;
					switch (textBox.Name) {
						case "KeyBox":
							keyBox = textBox;
							break;
						case "DisplayNameBox":
							textBox.Text = _controller.DisplayName;
							textBox.KeyDown += (_, args) => {
								if (args.Key == Key.Enter) {
									if (textBox.Text == null)
										return;
									if(!_controller.SetDisplayName(textBox.Text))
										textBox.Text = _controller.DisplayName;
								}
							};
							break;
					}
				} else if (enumerator.Current.GetType() == typeof(Image)) {
					Image image = (Image) enumerator.Current;
					switch (image.Name) {
						case "QrCodeImage":
							QRCodeGenerator qrGenerator = new ();
							QRCodeData qrCodeData = qrGenerator.CreateQrCode(_controller.PublicKey.ToBase64String(), QRCodeGenerator.ECCLevel.M);
							PngByteQRCode qrCode = new (qrCodeData);
							
							byte[] qrCodeBytes = qrCode.GetGraphic(20);
							
							using (MemoryStream stream = new (qrCodeBytes)) {
								Bitmap bitmap = new (stream);
								image.Source = bitmap;
							}

							break;
					}
				}
			}
			keyBox!.Text = _controller.PublicKey.ToBase64String();
		});
	}
	
	public void SetMainWindowModel(MainWindowModel mainWindowModel) {
		_controller.SetMainWindowModel(mainWindowModel);
	}
}
