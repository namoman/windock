using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WindowDock.Core.Models;
using WindowDock.Core.Native;
using WindowDock.Core.Services;

namespace WindowDock.UI.ViewModels;

public sealed class ParkedWindowItemViewModel
{
    public required IntPtr Handle { get; init; }
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
    public required DateTime ParkedAt { get; init; }
    public required bool UsedMinimizeFallback { get; init; }
    public ImageSource? Icon { get; init; }

    public string DisplayText => $"{ProcessName} — {Title}";

    public string SubText =>
        UsedMinimizeFallback
            ? $"최소화 보관 · {ParkedAt:HH:mm}"
            : $"작업표시줄 숨김 · {ParkedAt:HH:mm}";
}

public sealed class DockViewModel : INotifyPropertyChanged
{
    private readonly WindowParker _parker;
    private readonly WindowEnumerator _enumerator;

    public DockViewModel(WindowParker parker, WindowEnumerator enumerator)
    {
        _parker = parker;
        _enumerator = enumerator;
        ParkedWindows = new ObservableCollection<ParkedWindowItemViewModel>();
        RestoreCommand = new RelayCommand<ParkedWindowItemViewModel>(RestoreWindow);
        ParkForegroundCommand = new RelayCommand(ParkForeground);
        ParkFromPickerCommand = new RelayCommand(ShowPicker);

        _parker.ParkedWindowsChanged += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<ParkedWindowItemViewModel> ParkedWindows { get; }

    public ICommand RestoreCommand { get; }
    public ICommand ParkForegroundCommand { get; }
    public ICommand ParkFromPickerCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? RequestPicker;

    public void Refresh()
    {
        _parker.RefreshAliveStatus();
        var items = _parker.GetParkedWindows()
            .Select(CreateItem)
            .ToList();

        ParkedWindows.Clear();
        foreach (var item in items)
        {
            ParkedWindows.Add(item);
        }

        OnPropertyChanged(nameof(HasItems));
    }

    public bool HasItems => ParkedWindows.Count > 0;

    private void RestoreWindow(ParkedWindowItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        _parker.Restore(item.Handle);
        Refresh();
    }

    private void ParkForeground()
    {
        var result = _parker.ParkForegroundWindow(_enumerator);
        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Message))
        {
            System.Windows.MessageBox.Show(
                result.Message,
                "Windock",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        Refresh();
    }

    private void ShowPicker() => RequestPicker?.Invoke(this, EventArgs.Empty);

    private static ParkedWindowItemViewModel CreateItem(ParkedWindowInfo info) =>
        new()
        {
            Handle = info.Handle,
            Title = info.Title,
            ProcessName = info.ProcessName,
            ParkedAt = info.ParkedAt,
            UsedMinimizeFallback = info.UsedMinimizeFallback,
            Icon = TryLoadIcon(info.Handle)
        };

    private static ImageSource? TryLoadIcon(IntPtr hwnd)
    {
        try
        {
            var iconHandle = Win32Helper.GetWindowIcon(hwnd, large: false);

            if (iconHandle == IntPtr.Zero)
            {
                return null;
            }

            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                iconHandle,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        catch
        {
            return null;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}

public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) =>
        _canExecute?.Invoke(parameter is T t ? t : default) ?? true;

    public void Execute(object? parameter) =>
        _execute(parameter is T t ? t : default);
}
