using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using FlaUInspect.Core;
using FlaUInspect.Core.Logger;
using FlaUInspect.Settings;
using FlaUInspect.ViewModels;
using FlaUInspect.Views;
using Microsoft.Extensions.DependencyInjection;
using Color = System.Drawing.Color;
using wColor = System.Windows.Media.Color;

namespace FlaUInspect;

public partial class App {

	public static IServiceProvider Services { get; private set; } = default!;
	public static FlaUiAppOptions FlaUiAppOptions { get; } = new();

	public static InternalLogger Logger { get; } = new();

	protected override async void OnStartup(StartupEventArgs e) {
		base.OnStartup(e);

		ServiceCollection services = new();
		_ = services.AddSingleton<ISettingsService<FlaUiAppSettings>>(_ => new JsonSettingsService<FlaUiAppSettings>(Path.Combine(AppContext.BaseDirectory, $"appsettings.json")));
		Services = services.BuildServiceProvider();

		var settingsService = Services.GetRequiredService<ISettingsService<FlaUiAppSettings>>();
		var flaUiAppSettings = settingsService.Load();
		ApplyAppOption(flaUiAppSettings);

		//InternalLogger logger = new ();
		Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
		StartupViewModel startupViewModel = new();
		StartupWindow startupWindow = new(Logger) { DataContext = startupViewModel };
		Current.MainWindow = startupWindow;
		startupWindow.Show();

		//Preload light theme
		SetTheme(flaUiAppSettings);

		await Task.Run(startupViewModel.Init);
	}

	public static void ApplyAppOption(FlaUiAppSettings settings) {
		// Apply theme
		Current.Dispatcher.Invoke(() => SetTheme(settings));

		SetOverlayOption(settings.HoverOverlay, o => FlaUiAppOptions.HoverOverlay = o);
		SetOverlayOption(settings.SelectionOverlay, o => FlaUiAppOptions.SelectionOverlay = o);
		SetOverlayOption(settings.PickOverlay, o => FlaUiAppOptions.PickOverlay = o);

		// Add overlay colors as application resources
		Current.Dispatcher.Invoke(() => SetOverlayColors(settings));
	}

	private static void SetOverlayOption(OverlaySettings? overlay, Action<Func<ElementOverlay?>> setter)
		=> setter(overlay is not null ? (() => new(overlay)) : FlaUiAppOptions.DefaultOverlay);

	private static void SetOverlayColors(FlaUiAppSettings settings) {
		var pickColor = settings.PickOverlay is not null
			? ColorTranslator.FromHtml(settings.PickOverlay.OverlayColor)
			: Color.Blue;
		var selectionColor = settings.SelectionOverlay is not null
			? ColorTranslator.FromHtml(settings.SelectionOverlay.OverlayColor)
			: Color.Blue;

		Current.Resources["PickOverlayBrush"] = new SolidColorBrush(wColor.FromArgb(pickColor.A, pickColor.R, pickColor.G, pickColor.B));
		Current.Resources["SelectionOverlayBrush"] = new SolidColorBrush(wColor.FromArgb(selectionColor.A, selectionColor.R, selectionColor.G, selectionColor.B));
	}

	private static void SetTheme(FlaUiAppSettings settings) {
		ResourceDictionary newTheme = new() {
			Source = settings.Theme switch {
				"Dark" => new Uri("/FlaUInspect;component/Themes/DarkTheme.xaml", UriKind.Relative),
				_ => new Uri("/FlaUInspect;component/Themes/LightTheme.xaml", UriKind.Relative),
			}
		};

		// Remove existing theme dictionaries
		for (var i = Current.Resources.MergedDictionaries.Count - 1; i >= 0; i--) {
			var dict = Current.Resources.MergedDictionaries[i];

			if (dict.Source is not null && (dict.Source.OriginalString.Contains("Themes/DarkTheme.xaml") || dict.Source.OriginalString.Contains("Themes/LightTheme.xaml")))
				Current.Resources.MergedDictionaries.RemoveAt(i);
		}

		// Add the new theme dictionary
		Current.Resources.MergedDictionaries.Add(newTheme);
	}
}