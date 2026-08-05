using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using WindowDock.Core.Models;
using WindowDock.Core.Native;
using WindowDock.Core.Services;

namespace WindowDock.UI.ViewModels;

public sealed class ActiveWindowItemViewModel : INotifyPropertyChanged
{
    private bool _isVisible = true;

    public required IntPtr Handle { get; init; }
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
    public ImageSource? Icon { get; init; }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class ShortcutItemViewModel
{
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string IconGlyph { get; init; }
    public required string[] Keys { get; init; }
    public bool HighlightLastKey { get; init; }
}

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly WindowParker _parker;
    private readonly WindowEnumerator _enumerator;
    private readonly AppSettingsStore _settingsStore;
    private AppPage _currentPage = AppPage.Dashboard;
    private bool _hideAllEnabled;
    private int _animationSpeed = 1;
    private int _dockOpacity = 60;
    private bool _startWithWindows;
    private bool _hideTaskbarWhileActive;

    public MainViewModel(
        WindowParker parker,
        WindowEnumerator enumerator,
        AppSettingsStore settingsStore)
    {
        _parker = parker;
        _enumerator = enumerator;
        _settingsStore = settingsStore;

        var settings = _settingsStore.Load();
        _startWithWindows = settings.StartWithWindows;
        _hideTaskbarWhileActive = settings.HideTaskbarWhileActive;
        _animationSpeed = settings.AnimationSpeed;
        _dockOpacity = settings.DockOpacity;

        ParkedWindows = new ObservableCollection<ParkedWindowItemViewModel>();
        ActiveWindows = new ObservableCollection<ActiveWindowItemViewModel>();
        Shortcuts = CreateShortcuts();

        NavigateCommand = new RelayCommand<AppPage>(Navigate);
        ParkForegroundCommand = new RelayCommand(ParkForeground);
        RestoreParkedCommand = new RelayCommand<ParkedWindowItemViewModel>(RestoreParked);
        ToggleActiveWindowCommand = new RelayCommand<ActiveWindowItemViewModel>(ToggleActiveWindow);
        ToggleHideAllCommand = new RelayCommand(ToggleHideAll);
        OpenGitHubCommand = new RelayCommand(OpenGitHub);
        SaveSettingsCommand = new RelayCommand(SaveSettings);

        _parker.ParkedWindowsChanged += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<ParkedWindowItemViewModel> ParkedWindows { get; }
    public ObservableCollection<ActiveWindowItemViewModel> ActiveWindows { get; }
    public ObservableCollection<ShortcutItemViewModel> Shortcuts { get; }

    public ICommand NavigateCommand { get; }
    public ICommand ParkForegroundCommand { get; }
    public ICommand RestoreParkedCommand { get; }
    public ICommand ToggleActiveWindowCommand { get; }
    public ICommand ToggleHideAllCommand { get; }
    public ICommand OpenGitHubCommand { get; }
    public ICommand SaveSettingsCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AppPage CurrentPage
    {
        get => _currentPage;
        set
        {
            if (_currentPage == value)
            {
                return;
            }

            _currentPage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDashboard));
            OnPropertyChanged(nameof(IsWindowsPage));
            OnPropertyChanged(nameof(IsShortcutsPage));
            OnPropertyChanged(nameof(IsSettingsPage));
            OnPropertyChanged(nameof(PageTitle));
        }
    }

    public string PageTitle => CurrentPage switch
    {
        AppPage.Dashboard => "Dashboard",
        AppPage.Windows => "Windows",
        AppPage.Shortcuts => "Shortcuts",
        AppPage.Settings => "Settings",
        _ => "Windock"
    };

    public bool IsDashboard => CurrentPage == AppPage.Dashboard;
    public bool IsWindowsPage => CurrentPage == AppPage.Windows;
    public bool IsShortcutsPage => CurrentPage == AppPage.Shortcuts;
    public bool IsSettingsPage => CurrentPage == AppPage.Settings;

    public int HiddenCount => ParkedWindows.Count;
    public int ActiveCount => ActiveWindows.Count;
    public string SystemStatus => "활성";

    public bool HideAllEnabled
    {
        get => _hideAllEnabled;
        set
        {
            if (_hideAllEnabled == value)
            {
                return;
            }

            _hideAllEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (_startWithWindows == value)
            {
                return;
            }

            _startWithWindows = value;
            OnPropertyChanged();
            ApplyStartupSetting();
            SaveSettings();
        }
    }

    public bool HideTaskbarWhileActive
    {
        get => _hideTaskbarWhileActive;
        set
        {
            if (_hideTaskbarWhileActive == value)
            {
                return;
            }

            _hideTaskbarWhileActive = value;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public int AnimationSpeed
    {
        get => _animationSpeed;
        set
        {
            if (_animationSpeed == value)
            {
                return;
            }

            _animationSpeed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AnimationSpeedLabel));
            SaveSettings();
        }
    }

    public string AnimationSpeedLabel => AnimationSpeed switch
    {
        0 => "느림",
        2 => "빠름",
        _ => "보통"
    };

    public int DockOpacity
    {
        get => _dockOpacity;
        set
        {
            if (_dockOpacity == value)
            {
                return;
            }

            _dockOpacity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DockOpacityLabel));
            SaveSettings();
        }
    }

    public string DockOpacityLabel => $"{DockOpacity}%";

    public string AppVersion => "1.0.0";

    public ImageSource? AppIcon { get; set; }

    public void Refresh()
    {
        _parker.RefreshAliveStatus();
        RefreshParkedWindows();
        RefreshActiveWindows();
        OnPropertyChanged(nameof(HiddenCount));
        OnPropertyChanged(nameof(ActiveCount));
        HideAllEnabled = ParkedWindows.Count > 0 && ActiveWindows.Count == 0;
    }

    private void RefreshParkedWindows()
    {
        var items = _parker.GetParkedWindows().Select(CreateParkedItem).ToList();
        ParkedWindows.Clear();
        foreach (var item in items)
        {
            ParkedWindows.Add(item);
        }
    }

    private void RefreshActiveWindows()
    {
        var parkedHandles = _parker.GetParkedWindows().Select(w => w.Handle).ToHashSet();
        var items = _enumerator.GetOpenWindows()
            .Where(w => !parkedHandles.Contains(w.Handle))
            .Select(w => new ActiveWindowItemViewModel
            {
                Handle = w.Handle,
                Title = w.Title,
                ProcessName = w.ProcessName,
                Icon = TryLoadIcon(w.Handle),
                IsVisible = true
            })
            .ToList();

        ActiveWindows.Clear();
        foreach (var item in items)
        {
            ActiveWindows.Add(item);
        }
    }

    private void Navigate(AppPage page)
    {
        CurrentPage = page;
    }

    private void ParkForeground()
    {
        var result = _parker.ParkForegroundWindow(_enumerator);
        ShowMessageIfNeeded(result);
        Refresh();
    }

    private void RestoreParked(ParkedWindowItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        _parker.Restore(item.Handle);
        Refresh();
    }

    private void ToggleActiveWindow(ActiveWindowItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var result = _parker.Park(item.Handle);
        ShowMessageIfNeeded(result);
        Refresh();
    }

    private void ToggleHideAll()
    {
        if (ActiveWindows.Count > 0)
        {
            foreach (var window in ActiveWindows.ToList())
            {
                _parker.Park(window.Handle);
            }
        }
        else
        {
            foreach (var parked in ParkedWindows.ToList())
            {
                _parker.Restore(parked.Handle);
            }
        }

        Refresh();
    }

    private static void ShowMessageIfNeeded(ParkResult result)
    {
        if (!result.IsSuccess && !string.IsNullOrWhiteSpace(result.Message))
        {
            System.Windows.MessageBox.Show(
                result.Message,
                "Windock",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }

    private void OpenGitHub()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/namoman/windock",
            UseShellExecute = true
        });
    }

    private void SaveSettings()
    {
        _settingsStore.Save(new AppSettings
        {
            StartWithWindows = StartWithWindows,
            HideTaskbarWhileActive = HideTaskbarWhileActive,
            AnimationSpeed = AnimationSpeed,
            DockOpacity = DockOpacity
        });
    }

    private void ApplyStartupSetting()
    {
        const string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string valueName = "Windock";

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyName, writable: true);
            if (key is null)
            {
                return;
            }

            if (StartWithWindows)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(exePath))
                {
                    key.SetValue(valueName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(valueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Registry access may fail in restricted environments.
        }
    }

    private static ParkedWindowItemViewModel CreateParkedItem(ParkedWindowInfo info) =>
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

    private static ObservableCollection<ShortcutItemViewModel> CreateShortcuts() =>
    [
        new ShortcutItemViewModel
        {
            Title = "현재 창 보관",
            Subtitle = "Park Active Window",
            IconGlyph = "📥",
            Keys = ["Ctrl", "Shift", "P"],
            HighlightLastKey = true
        }
    ];

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
