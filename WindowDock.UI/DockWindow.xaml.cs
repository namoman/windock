using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WindowDock.UI.ViewModels;

namespace WindowDock.UI;

public partial class DockWindow : Window
{
    public DockWindow(DockViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestPicker += (_, _) => OnRequestPicker();
    }

    public event EventHandler? RequestPicker;

    private void OnRequestPicker() => RequestPicker?.Invoke(this, EventArgs.Empty);

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            return;
        }

        DragMove();
    }

    private void ParkedItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem { DataContext: ParkedWindowItemViewModel item }
            && DataContext is DockViewModel vm
            && vm.RestoreCommand.CanExecute(item))
        {
            vm.RestoreCommand.Execute(item);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();
}
