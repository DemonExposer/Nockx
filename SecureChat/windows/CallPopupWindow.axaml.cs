using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;

namespace SecureChat.windows;

public partial class CallPopupWindow : PopupWindow {
	private TextBlock _volumeTextBlock = null!;
	
	public CallPopupWindow() {
		InitializeComponent();
	}

	public void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
		_volumeTextBlock = this.FindControl<TextBlock>("VolumeTextBlock")!;
		this.FindControl<Slider>("VolumeSlider")!.Value = 50;
	}

	private void VolumeSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e) {
		_volumeTextBlock.Text = ((int) e.NewValue).ToString();
	}
}