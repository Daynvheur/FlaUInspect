using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FlaUInspect.ViewModels;
using Button = System.Windows.Controls.Button;
using ToggleButton = System.Windows.Controls.Primitives.ToggleButton;
using Window = System.Windows.Window;

namespace FlaUInspect.Views;

public partial class ProcessWindow : Window {
	public ProcessWindow() {
		InitializeComponent();
		Loaded += MainWindow_Loaded;
	}

	private void MainWindow_Loaded(object sender, EventArgs e) {
		if (DataContext is ProcessViewModel processViewModel) {
			processViewModel.CopiedNotificationRequested += ShowCopiedNotification;
			processViewModel.CopiedNotificationCurrentElementSaveStateRequested += ShowCopiedNotificationCurrentElementSaveState;
		}
	}

	private void ShowCopiedNotification() => ShowCopiedNotification(CopiedNotificationGrid);

	private void ShowCopiedNotificationCurrentElementSaveState() => ShowCopiedNotification(CopiedNotificationCurrentElementSaveStateGrid);

	private static async void ShowCopiedNotification(Grid ShowCopiedNotification) {
		ShowCopiedNotification.Visibility = Visibility.Visible;
		DoubleAnimation animation = new(1, 0, TimeSpan.FromSeconds(1));
		ShowCopiedNotification.BeginAnimation(OpacityProperty, animation);
		await Task.Delay(1000);
		ShowCopiedNotification.Visibility = Visibility.Collapsed;
	}

	private void ProcessWindow_Closed(object? sender, EventArgs e) => ExecuteClosingCommand();

	private void SelectWindowClick(object sender, RoutedEventArgs e) => (Application.Current.MainWindow as StartupWindow)?.Show();

	private void TreeViewControl_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
		if (DataContext is ProcessViewModel processViewModel)
			processViewModel.SelectedItem = e.NewValue as ElementViewModel;

		var container = TreeViewControl.ItemContainerGenerator.ContainerFromItem(TreeViewControl.SelectedItem) as TreeViewItem;
		container?.BringIntoView();
	}

	private async void InvokePatternActionHandler(object sender, RoutedEventArgs e) {
		var vm = (sender as Button)?.DataContext as PatternItem;

		if (vm?.Action is not null)
			await Task.Run(vm.Action);
	}

	private void ExecuteClosingCommand() {
		if (DataContext is ProcessViewModel processViewModel && processViewModel.ClosingCommand.CanExecute(DataContext))
			processViewModel.ClosingCommand.Execute(DataContext);
	}

	private void TreeViewControl_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
		if (DataContext is not ProcessViewModel processViewModel || TreeViewControl.SelectedItem is not ElementViewModel selectedElement)
			return;

		processViewModel.SetFocus(selectedElement, 3);
	}

	private void TreeViewControl_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
		var treeView = sender as TreeView;
		if (treeView is null || DataContext is not ProcessViewModel processViewModel)
			return;

		// Find the TreeViewItem under the mouse
		var source = e.OriginalSource as DependencyObject;
		while (source is not null && source is not TreeViewItem)
			source = VisualTreeHelper.GetParent(source);

		if (source is TreeViewItem itemContainer) {
			itemContainer.IsSelected = true;
			processViewModel.SelectedItem = itemContainer.DataContext as ElementViewModel;
		}
	}

	private void HighlightFocusClick(object sender, RoutedEventArgs e) {
		if (DataContext is not ProcessViewModel processViewModel || TreeViewControl.SelectedItem is not ElementViewModel selectedElement)
			return;

		processViewModel.SetFocus(selectedElement, 3);
	}
}