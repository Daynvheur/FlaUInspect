using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using FlaUI.Core.Overlay;
using FlaUInspect.Settings;

namespace FlaUInspect.Core;

public partial class ElementOverlay(ElementOverlayConfiguration configuration) : IDisposable {
	public ElementOverlay(OverlaySettings HoverOverlay) : this(new ElementOverlayConfiguration(HoverOverlay)) { }

	private static int _instanceCounter = 0;
	private readonly int _instanceId = Interlocked.Increment(ref _instanceCounter);
	private readonly Dictionary<int, Form> _overlayRectangleFormList = new();

	public ElementOverlayConfiguration Configuration { get; } = configuration;

	public void Dispose() {
		Hide();
		GC.SuppressFinalize(this);
	}

	public void Hide() {
		foreach (var overlayRectangleForm in _overlayRectangleFormList) {
			try {
				overlayRectangleForm.Value.Hide();
				overlayRectangleForm.Value.Close();
			}
			catch (InvalidOperationException) { }
			overlayRectangleForm.Value.Dispose();
		}
		_overlayRectangleFormList.Clear();
	}

	public void Hide(Rectangle rectangle) {
		var key = GetRectangleKey(rectangle);
		if (_overlayRectangleFormList.TryGetValue(key, out var overlayRectangleForm)) {
			try {
				overlayRectangleForm.Hide();
				overlayRectangleForm.Close();
			}
			catch (InvalidOperationException) { }
			overlayRectangleForm.Dispose();
			_ = _overlayRectangleFormList.Remove(key);
		}
	}

	public void Show(Rectangle rectangle) {
		var color1 = Color.FromArgb(255, Configuration.Color.R, Configuration.Color.G, Configuration.Color.B);
		var rectangles = Configuration.RectangleFactory?.Invoke(Configuration, rectangle) ?? ElementOverlayConfiguration.BoundRectangleFactory(Configuration, rectangle);

		foreach (var rectangle1 in rectangles) {
			var key = GetRectangleKey(rectangle1);
			if (!_overlayRectangleFormList.TryGetValue(key, out var overlayRectangleForm)) {
				overlayRectangleForm = new Form {
					FormBorderStyle = FormBorderStyle.None,
					BackColor = color1,
					TransparencyKey = color1,
					TopMost = true,
					ShowInTaskbar = false,
					Opacity = Configuration.Color.A / 255d,
					Tag = _instanceId
				};
				_overlayRectangleFormList[key] = overlayRectangleForm;
			}

			_ = SetWindowPos(overlayRectangleForm.Handle, new IntPtr(-1), rectangle1.X, rectangle1.Y, rectangle1.Width, rectangle1.Height, 16 /*0x10*/);
			_ = ShowWindow(overlayRectangleForm.Handle, 8);
		}
	}

	private static int GetRectangleKey(Rectangle rectangle) => HashCode.Combine(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool SetWindowPos(
		IntPtr hWnd,
		IntPtr hwndAfter,
		int x,
		int y,
		int width,
		int height,
		int flags);

	[LibraryImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);
}

public record ElementOverlayConfiguration(int Size, Thickness Margin, Color Color, Func<ElementOverlayConfiguration, Rectangle, Rectangle[]>? RectangleFactory = null) {
	public ElementOverlayConfiguration(OverlaySettings HoverOverlay) : this(HoverOverlay.Size,
												(Thickness)(new ThicknessConverter().ConvertFromString(HoverOverlay.Margin) ?? new()),
												ColorTranslator.FromHtml(HoverOverlay.OverlayColor),
												GetRectangleFactory(HoverOverlay.OverlayMode)) { }

	public static Func<ElementOverlayConfiguration, Rectangle, Rectangle[]> GetRectangleFactory(string? mode) => mode?.ToLower(CultureInfo.InvariantCulture) switch {
		"fill" => FillRectangleFactory,
		"border" => BoundRectangleFactory,
		_ => BoundRectangleFactory
	};

	public static Rectangle[] FillRectangleFactory(ElementOverlayConfiguration config, Rectangle rectangle) => [
			new Rectangle(rectangle.X - (int)config.Margin.Left, rectangle.Y - (int)config.Margin.Top, rectangle.Width + (int)config.Margin.Right, rectangle.Height + (int)config.Margin.Bottom)
		];

	public static Rectangle[] BoundRectangleFactory(ElementOverlayConfiguration config, Rectangle rectangle) => [
			new Rectangle(rectangle.X - (int)config.Margin.Left, rectangle.Y - (int)config.Margin.Top, config.Size, rectangle.Height + (int)config.Margin.Bottom),
			new Rectangle(rectangle.X - (int)config.Margin.Left, rectangle.Y - (int)config.Margin.Top, rectangle.Width + (int)config.Margin.Right, config.Size),
			new Rectangle(rectangle.X + rectangle.Width - config.Size + (int)config.Margin.Left, rectangle.Y - (int)config.Margin.Top, config.Size, rectangle.Height + (int)config.Margin.Bottom),
			new Rectangle(rectangle.X - (int)config.Margin.Left, rectangle.Y + rectangle.Height - config.Size + (int)config.Margin.Right, rectangle.Width + (int)config.Margin.Right, config.Size)
		];
}