﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using SecureChat.ClassExtensions;
using Avalonia.Interactivity;
using SecureChat.Windows;
using System;
using Avalonia.Media.Imaging;
using System.IO;
using SkiaSharp;
using Avalonia.Media;
using System.Collections.Generic;
using OpenTK.Mathematics;
using System.Diagnostics;
using Avalonia.Threading;

namespace SecureChat.Panels;

public partial class UserInfoPanel : StackPanel {
	public readonly TextBox DisplayNameTextBox;

	private readonly UserInfoPanelController _controller;
	private ConnectAppWindow? _connectAppWindow;
	private WriteableBitmap _bitmap;
	private Image _pixelImage;

	private Bitmap _pngBitmap;
	private RenderTargetBitmap _rotatedBitmap;

	public UserInfoPanel() {
		_controller = new UserInfoPanelController(this);

		InitializeComponent();

		TextBox keyBox = this.FindControl<TextBox>("KeyBox")!;
		DisplayNameTextBox = this.FindControl<TextBox>("DisplayNameBox")!;
		Button setDisplayNameButton = this.FindControl<Button>("SetDisplayNameButton")!;
		Button connectAppButton = this.FindControl<Button>("ConnectAppButton")!;
		keyBox.PointerReleased += (_, _) => keyBox.SelectAll();

		DisplayNameTextBox.KeyDown += (_, args) => {
			if (args.Key == Key.Enter)
				setDisplayNameButton?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
		};

		setDisplayNameButton.Click += (_, _) => {
			if (DisplayNameTextBox?.Text == null)
				return;

			_controller.SetDisplayName(DisplayNameTextBox.Text);
		};

		connectAppButton.Click += ConnectAppButton_OnClick;

		keyBox!.Text = _controller.PublicKey.ToBase64String();

		_pixelImage = this.FindControl<Image>("PixelImage")!;
		
		_bitmap = new WriteableBitmap(new PixelSize(128, 128), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);

		byte[] bitmap = File.ReadAllBytes("in.bmp")[138..];
		int[] pixels = new int[bitmap.Length / sizeof(int)];
		Debug.WriteLine("{0} {1}", bitmap.Length, pixels.Length);
		Buffer.BlockCopy(bitmap, 0, pixels, 0, bitmap.Length);
		using (ILockedFramebuffer fb = _bitmap.Lock()) {
			unsafe {
				int *buffer = (int *) fb.Address;

				for (int i = 0; i < pixels.Length; i++)
					buffer[i] = pixels[i];
			}
		}

		_pixelImage.Source = _bitmap;
	}

	public List<Vector4> GetPixelMap() {
		List<Vector4> res = [];
		
		using (var fb = _bitmap.Lock()) {
			unsafe {
				int *buffer = (int *) fb.Address;
				for (int i = 0; i < 128 * 128; i++) {
				//	Debug.WriteLine("{0} {1}", i % 128, i / 128);
					res.Add(new Vector4(i % 128, i / 128, buffer[i] & 0xFF, buffer[i] >> 24));
				}
			}
		}

		return res;
	}

	public void DrawPixel(int x, int y, byte r, byte g, byte b, byte a) {
		if (x >= 128 || y >= 128 || x < 0 || y < 0)
			return;
		using (var fb = _bitmap.Lock()) {
			unsafe {
				int *buffer = (int *) fb.Address;
				int index = y * fb.RowBytes / 4 + x;

				// BGRA
				buffer[index] = (a << 24) | (r << 16) | (g << 8) | b;
			}
		}

		// Invalidate the bitmap to show the changes
		_pixelImage.InvalidateVisual();
	}

	public void Clear() {
		for (int i = 0; i < 128; i++) {
			for (int j = 0; j < 128; j++) {
				DrawPixel(i, j, 0, 0, 0, 0);
			}
		}
	}

	public void Save() {
		using FileStream fs = File.Create("out.png");
		_bitmap.Save(fs);
	}
	
	public void SetMainWindowModel(MainWindowModel mainWindowModel) {
		_controller.SetMainWindowModel(mainWindowModel);
	}

	private void ConnectAppButton_OnClick(object? sender, RoutedEventArgs e) {
		_connectAppWindow = new ConnectAppWindow();
		_connectAppWindow.Closed += (_, _) => _connectAppWindow = null;
		// TODO: also put timestamp with signature in the QR code so that the scanner can be verified as being a real scanner
		_connectAppWindow.SetQrCode(_controller.PublicKey.ToBase64String());
		_connectAppWindow.Show();
	}

	public void ShowQrCodeConfirmationWindow(string key, byte[] onConfirmData) {
		if (_connectAppWindow == null)
			return;

		new DialogWindow(
			$"{key} is trying to retrieve your private key. If this key does not correspond to the one on the device you scanned your QR code with, DO NOT ACCEPT THIS REQUEST, BECAUSE YOU CAN LOSE YOUR ACCOUNT!!!",
			accepted => {
				if (accepted)
					_connectAppWindow?.SetQrCode(Convert.ToBase64String(onConfirmData));
			}
		).Show(_connectAppWindow);
	}
}
