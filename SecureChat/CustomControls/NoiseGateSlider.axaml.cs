using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace SecureChat.CustomControls;

public partial class NoiseGateSlider : UserControl {
	protected readonly ProgressBar ProgressBar;
	protected readonly Track Track;
	
	public double Value { get; protected set; }

	public double VolumePercent {
		get => ProgressBar.Value;
		set => ProgressBar.Value = value;
	}
	
	public event EventHandler<RangeBaseValueChangedEventArgs>? ValueChanged {
		add => AddHandler(RangeBase.ValueChangedEvent, value);
		remove => RemoveHandler(RangeBase.ValueChangedEvent, value);
	}
	
	public NoiseGateSlider() {
		InitializeComponent();
		
		ProgressBar = this.FindControl<ProgressBar>("VolumeBar")!;
		Track = this.FindControl<Track>("PART_Track")!;
		
		Value = Track.Value;
		Track.Thumb!.DragDelta += (_, _) => {
			RaiseEvent(new RangeBaseValueChangedEventArgs(Value, Track.Value, RangeBase.ValueChangedEvent, this));
			Value = Track.Value;
		};
	}
}
