using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using FlaUInspect.Settings;

namespace FlaUInspect.Core;

public partial class ElementOverlay(ElementOverlayConfiguration configuration) : IDisposable {
	public ElementOverlay(OverlaySettings HoverOverlay) : this(new ElementOverlayConfiguration(HoverOverlay)) { }

	private readonly Action<Graphics, Color, int, Rectangle> _paintAction = ElementOverlayConfiguration.GetPaintAction(configuration.Mode);
	private readonly Color _color = Color.FromArgb(255, configuration.Color.R, configuration.Color.G, configuration.Color.B);
	private readonly double _opacity = configuration.Color.A / 255d;
	private Form? _overlayRectangleForm;

	public ElementOverlayConfiguration Configuration { get; } = configuration;

	public void Dispose() {
		_overlayRectangleForm?.Dispose();
		GC.SuppressFinalize(this);
	}

	public void Hide() {
		if (_overlayRectangleForm is null)
			return;

		try {
			_overlayRectangleForm.Hide();
			_overlayRectangleForm.Close();
		}
		catch (InvalidOperationException) { }
		_overlayRectangleForm.Dispose();
		_overlayRectangleForm = null;
	}

	public void Show(Rectangle rectangle, int? blinkCount = null, int blinkIntervalMs = 500) {
		var rectangles = Configuration.RectangleFactory?.Invoke(Configuration, rectangle);
		if (rectangles is null || rectangles.Length == 0)
			return;

		var rectangle1 = rectangles[0];
		var form = new Form {
			FormBorderStyle = FormBorderStyle.None,
			BackColor = Color.Magenta,
			TransparencyKey = Color.Magenta,
			TopMost = true,
			ShowInTaskbar = false,
			Opacity = _opacity,
			Bounds = rectangle1
		};
		form.Paint += (s, e) => _paintAction(e.Graphics, _color, Configuration.Size, form.ClientRectangle);
		form.Invalidate();
		_ = SetWindowPos(form.Handle, new IntPtr(-1), rectangle1.X, rectangle1.Y, rectangle1.Width, rectangle1.Height, 16);
		_ = ShowWindow(form.Handle, 8);

		_overlayRectangleForm = form;

		if (blinkCount.HasValue) {
			var phase = 0;
			var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(blinkIntervalMs) };
			timer.Tick += (_, _) => {
				if (++phase >= blinkCount.Value * 2) {
					timer.Stop();
					try {
						form.Close();
					}
					catch (InvalidOperationException) { }
					form.Dispose();
					_overlayRectangleForm = null;
				}
				else if (phase % 2 == 0) {
					form.Hide();
				}
				else {
					form.Show();
				}
			};
			timer.Start();
		}
	}

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

public record ElementOverlayConfiguration(int Size, Thickness Margin, Color Color, Func<ElementOverlayConfiguration, Rectangle, Rectangle[]>? RectangleFactory = null, string? Mode = null) {
	public ElementOverlayConfiguration(OverlaySettings HoverOverlay) : this(HoverOverlay.Size,
												(Thickness)(new ThicknessConverter().ConvertFromString(HoverOverlay.Margin) ?? new()),
												ColorTranslator.FromHtml(HoverOverlay.OverlayColor),
												GetRectangleFactory(HoverOverlay.OverlayMode),
												HoverOverlay.OverlayMode) { }

	private static Func<ElementOverlayConfiguration, Rectangle, Rectangle[]> GetRectangleFactory(string? mode) => mode?.ToLower(CultureInfo.InvariantCulture) switch {
		"fill" => FillRectangleFactory,
		"border" or _ => BoundRectangleFactory
	};

	public static Action<Graphics, Color, int, Rectangle> GetPaintAction(string? mode) => mode?.ToLower(CultureInfo.InvariantCulture) switch {
		"fill" => (g, c, _, bounds) => g.FillRectangle(new SolidBrush(c), bounds),
		"border" or _ => (g, c, s, bounds) => g.DrawRectangle(new Pen(c, s), bounds.X, bounds.Y, bounds.Width - s - 1, bounds.Height - s - 1)
	};

	public static Rectangle[] FillRectangleFactory(ElementOverlayConfiguration config, Rectangle rectangle) => [
			new Rectangle(rectangle.X - (int)config.Margin.Left, rectangle.Y - (int)config.Margin.Top, rectangle.Width + (int)config.Margin.Right, rectangle.Height + (int)config.Margin.Bottom)
		];

	public static Rectangle[] BoundRectangleFactory(ElementOverlayConfiguration config, Rectangle rectangle) => [
			new Rectangle(rectangle.X - (int)config.Margin.Left, rectangle.Y - (int)config.Margin.Top,
				rectangle.Width + (int)config.Margin.Left + (int)config.Margin.Right,
				rectangle.Height + (int)config.Margin.Top + (int)config.Margin.Bottom)
		];
}