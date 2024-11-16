using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace SecureChat.windows;

public partial class CallPopupWindow : PopupWindow {
	private TextBlock _volumeTextBlock = null!;
	
	public CallPopupWindow() {
		InitializeComponent();
	}

	private void InitializeComponent() {
		AvaloniaXamlLoader.Load(this);
		_volumeTextBlock = this.FindControl<TextBlock>("VolumeTextBlock")!;
		Slider slider = this.FindControl<Slider>("VolumeSlider")!;
		slider.Value = 50;
		slider.Styles.Add(new Style(x => x.OfType<Slider>().Descendant().OfType<Thumb>()) {
			Setters = {
				new Setter(MaxHeightProperty, 15D),
				new Setter(MaxWidthProperty, 15D)
			}
		});
	}

	private void VolumeSlider_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e) {
		_volumeTextBlock.Text = ((int) e.NewValue).ToString();
	}
}