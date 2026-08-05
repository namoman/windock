using System.Windows.Interop;
using System.Windows.Threading;
using WindowDock.Core.Native;
using WindowDock.Core.Services;
using WindowDock.UI;
using WindowDock.UI.ViewModels;
using Forms = System.Windows.Forms;

namespace WindowDock.App;

public partial class App : System.Windows.Application
{
    private const int HotkeyId = 0x5744;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint VkP = 0x50;

    private System.Windows.Window? _ownerWindow;
    private HwndSource? _hwndSource;
    private MainWindow? _mainWindow;
    private Forms.NotifyIcon? _notifyIcon;
    private DispatcherTimer? _refreshTimer;

    private readonly WindowEnumerator _enumerator = new();
    private readonly WindowParker _parker = new();
    private readonly ParkedWindowStore _store = new();
    private readonly AppSettingsStore _settingsStore = new();
    private MainViewModel? _mainViewModel;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _ownerWindow = new System.Windows.Window
        {
            Width = 0,
            Height = 0,
            WindowStyle = System.Windows.WindowStyle.None,
            ShowInTaskbar = false,
            Visibility = System.Windows.Visibility.Hidden
        };
        _ownerWindow.Show();

        var ownerHwnd = new WindowInteropHelper(_ownerWindow).EnsureHandle();
        _parker.SetOwnerWindow(ownerHwnd);

        _mainViewModel = new MainViewModel(_parker, _enumerator, _settingsStore)
        {
            AppIcon = AppIconLoader.LoadImageSource()
        };
        _mainWindow = new MainWindow(_mainViewModel)
        {
            Owner = _ownerWindow
        };

        _hwndSource = HwndSource.FromHwnd(ownerHwnd);
        _hwndSource.AddHook(WndProc);
        RegisterGlobalHotkey(ownerHwnd);

        SetupTrayIcon();
        SetupRefreshTimer();

        _parker.ParkedWindowsChanged += (_, _) =>
            _store.SaveMetadata(_parker.GetParkedWindows());

        ApplyWindowOpacity();
        _mainWindow.Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        if (_hwndSource != null)
        {
            UnregisterGlobalHotkey();
            _hwndSource.RemoveHook(WndProc);
        }

        _refreshTimer?.Stop();
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }

    private void ApplyWindowOpacity()
    {
        if (_mainWindow is null || _mainViewModel is null)
        {
            return;
        }

        _mainWindow.Opacity = Math.Clamp(_mainViewModel.DockOpacity / 100.0, 0.1, 1.0);
        _mainViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.DockOpacity) && _mainWindow is not null)
            {
                _mainWindow.Opacity = Math.Clamp(_mainViewModel.DockOpacity / 100.0, 0.1, 1.0);
            }
        };
    }

    private void SetupTrayIcon()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Windock",
            Visible = true,
            Icon = AppIconLoader.LoadTrayIcon() ?? System.Drawing.SystemIcons.Application
        };

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Windock 표시/숨김", null, (_, _) => ToggleMainWindow());
        menu.Items.Add("현재 창 보관", null, (_, _) => ParkForeground());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("종료", null, (_, _) => Shutdown());

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (_, _) => ToggleMainWindow();
    }

    private void SetupRefreshTimer()
    {
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _refreshTimer.Tick += (_, _) => _mainViewModel?.Refresh();
        _refreshTimer.Start();
    }

    private void ToggleMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (_mainWindow.IsVisible)
        {
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Show();
            _mainWindow.Activate();
        }
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

        _mainViewModel?.Refresh();
    }

    private static void RegisterGlobalHotkey(IntPtr hwnd)
    {
        if (!Win32Helper.RegisterHotKey(hwnd, HotkeyId, ModControl | ModShift, VkP))
        {
            System.Windows.MessageBox.Show(
                "글로벌 핫키(Ctrl+Shift+P) 등록에 실패했습니다.",
                "Windock",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    private void UnregisterGlobalHotkey()
    {
        if (_ownerWindow is null)
        {
            return;
        }

        var hwnd = new WindowInteropHelper(_ownerWindow).Handle;
        Win32Helper.UnregisterHotKey(hwnd, HotkeyId);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int wmHotkey = 0x0312;
        if (msg == wmHotkey && wParam.ToInt32() == HotkeyId)
        {
            ParkForeground();
            handled = true;
        }

        return IntPtr.Zero;
    }
}
