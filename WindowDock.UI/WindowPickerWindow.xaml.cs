using System.Windows;
using WindowDock.Core.Models;
using WindowDock.Core.Services;

namespace WindowDock.UI;

public partial class WindowPickerWindow : Window
{
    private readonly WindowParker _parker;

    public WindowPickerWindow(WindowEnumerator enumerator, WindowParker parker)
    {
        InitializeComponent();
        _parker = parker;
        WindowsList.ItemsSource = enumerator.GetOpenWindows();
    }

    private void ParkButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowsList.SelectedItem is not WindowInfo selected)
        {
            MessageBox.Show(
                "보관할 창을 선택하세요.",
                "Windock",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var result = _parker.Park(selected.Handle);
        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Message))
        {
            MessageBox.Show(
                result.Message,
                "Windock",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
