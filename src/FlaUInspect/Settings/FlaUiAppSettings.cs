using FlaUInspect.Core;

namespace FlaUInspect.Settings;

public class FlaUiAppSettings : ObservableObject, ICloneable {
	public string Theme {
		get;
		set => SetProperty(ref field, value);
	} = "Light";

	public OverlaySettings? HoverOverlay {
		get;
		set => SetProperty(ref field, value);
	} = new();

	public OverlaySettings? SelectionOverlay {
		get;
		set => SetProperty(ref field, value);
	} = new();

	public OverlaySettings? PickOverlay {
		get;
		set => SetProperty(ref field, value);
	} = new();

	public object Clone() => new FlaUiAppSettings {
		Theme = Theme,
		HoverOverlay = HoverOverlay?.Clone() as OverlaySettings,
		SelectionOverlay = SelectionOverlay?.Clone() as OverlaySettings,
		PickOverlay = PickOverlay?.Clone() as OverlaySettings
	};

	public void CopyTo(FlaUiAppSettings to) {
		to.Theme = Theme;
		to.PickOverlay?.CopyTo(PickOverlay);
		to.SelectionOverlay?.CopyTo(SelectionOverlay);
		to.HoverOverlay?.CopyTo(HoverOverlay);
	}
}