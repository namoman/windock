using System.Windows;
using System.Windows.Input;
using WindowDock.UI.ViewModels;

namespace WindowDock.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            return;
        }

        DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();
}
