using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using FlaUI.Core.Definitions;

namespace FlaUInspect.Core.Converters;

public class ControlTypeToIconConverter : MarkupExtension, IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		if (value is not ControlType controlType)
			return null;

		var iconName = controlType switch {
			ControlType.Button => "Button",
			ControlType.Text => "Text",
			ControlType.CheckBox => "CheckBox",
			ControlType.ComboBox => "ComboBox",
			ControlType.Image => "Image",
			ControlType.Window => "Window",
			ControlType.Tree => "Tree",
			ControlType.List => "List",
			ControlType.Tab => "Tab",
			ControlType.Edit => "Edit",
			ControlType.RadioButton => "RadioButton",
			ControlType.ProgressBar => "ProgressBar",
			ControlType.Header => "Header",
			ControlType.HeaderItem => "HeaderItem",
			ControlType.Menu => "Menu",
			ControlType.MenuItem => "MenuItem",
			ControlType.Document => "Document",
			ControlType.Group => "Group",
			ControlType.Pane => "Pane",
			ControlType.ScrollBar => "ScrollBar",
			ControlType.Slider => "Slider",
			ControlType.Spinner => "Spinner",
			ControlType.SplitButton => "SplitButton",
			ControlType.StatusBar => "StatusBar",
			ControlType.ToolBar => "ToolBar",
			ControlType.ToolTip => "ToolTip",
			ControlType.Thumb => "Thumb",
			ControlType.TitleBar => "TitleBar",
			ControlType.DataGrid => "DataGrid",
			ControlType.Custom => "Custom",
			_ => "Custom"
		};

		return FindResourceInMergedDictionaries(iconName) as Canvas;
	}

	private static object FindResourceInMergedDictionaries(object key) {
		var dictionaries = new List<ResourceDictionary>();

		var currentApp = Application.Current;
		if (currentApp?.Resources != null)
			dictionaries.Add(currentApp.Resources);

		if (currentApp?.Windows != null) {
			foreach (Window window in currentApp.Windows) {
				if (window?.Resources == null)
					continue;

				foreach (ResourceDictionary dict in window.Resources.MergedDictionaries) {
					if (dict == null)
						continue;

					dictionaries.Add(dict);

					foreach (ResourceDictionary merged in dict.MergedDictionaries) {
						if (merged != null)
							dictionaries.Add(merged);
					}
				}
			}
		}

		foreach (ResourceDictionary dict in dictionaries) {
			if (dict == null)
				continue;

			if (dict.Contains(key))
				return dict[key];
		}

		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		=> throw new NotImplementedException();

	public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
