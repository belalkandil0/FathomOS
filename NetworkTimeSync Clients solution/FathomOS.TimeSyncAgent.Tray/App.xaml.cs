using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace FathomOS.TimeSyncAgent.Tray;

public partial class App : System.Windows.Application
{
    private TaskbarIcon? _trayIcon;
    private StatusWindow? _statusWindow;
    private TrayViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Create ViewModel
        _viewModel = new TrayViewModel();

        // Create the status window (hidden initially)
        _statusWindow = new StatusWindow
        {
            DataContext = _viewModel
        };

        // Create the tray icon
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Fathom OS Time Sync Agent",
            Icon = CreateIcon(_viewModel.StatusColor),
            ContextMenu = CreateContextMenu()
        };

        // Handle click events
        _trayIcon.TrayLeftMouseUp += TrayIcon_Click;
        _trayIcon.TrayMouseDoubleClick += TrayIcon_DoubleClick;

        // Update icon when status changes
        _viewModel.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(TrayViewModel.StatusColor))
            {
                Dispatcher.Invoke(() =>
                {
                    _trayIcon.Icon = CreateIcon(_viewModel.StatusColor);
                    _trayIcon.ToolTipText = $"Fathom OS Time Sync - {_viewModel.ServiceStatusText}";
                });
            }
        };

        // Start monitoring
        _viewModel.StartMonitoring();
    }

    private void TrayIcon_Click(object sender, RoutedEventArgs e)
    {
        ShowStatusWindow();
    }

    private void TrayIcon_DoubleClick(object sender, RoutedEventArgs e)
    {
        ShowStatusWindow();
    }

    private void ShowStatusWindow()
    {
        if (_statusWindow == null) return;

        if (_statusWindow.IsVisible)
        {
            _statusWindow.Hide();
        }
        else
        {
            // Position near system tray
            var workArea = SystemParameters.WorkArea;
            _statusWindow.Left = workArea.Right - _statusWindow.Width - 10;
            _statusWindow.Top = workArea.Bottom - _statusWindow.Height - 10;
            _statusWindow.Show();
            _statusWindow.Activate();
        }
    }

    private System.Windows.Controls.ContextMenu CreateContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var showItem = new System.Windows.Controls.MenuItem { Header = "Show Status" };
        showItem.Click += (s, e) => ShowStatusWindow();
        menu.Items.Add(showItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var startItem = new System.Windows.Controls.MenuItem { Header = "Start Service" };
        startItem.Click += (s, e) => _viewModel?.StartService();
        menu.Items.Add(startItem);

        var stopItem = new System.Windows.Controls.MenuItem { Header = "Stop Service" };
        stopItem.Click += (s, e) => _viewModel?.StopService();
        menu.Items.Add(stopItem);

        var restartItem = new System.Windows.Controls.MenuItem { Header = "Restart Service" };
        restartItem.Click += (s, e) => _viewModel?.RestartService();
        menu.Items.Add(restartItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (s, e) => ExitApplication();
        menu.Items.Add(exitItem);

        return menu;
    }

    private System.Drawing.Icon CreateIcon(string colorName)
    {
        // Create a simple icon programmatically based on status color
        var bitmap = new System.Drawing.Bitmap(16, 16);
        using (var g = System.Drawing.Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            var color = colorName switch
            {
                "Green" => System.Drawing.Color.FromArgb(108, 203, 95),
                "Yellow" => System.Drawing.Color.FromArgb(252, 225, 0),
                "Red" => System.Drawing.Color.FromArgb(255, 107, 107),
                _ => System.Drawing.Color.FromArgb(160, 160, 160)
            };

            using var brush = new System.Drawing.SolidBrush(color);
            g.FillEllipse(brush, 1, 1, 14, 14);

            // Add border
            using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(60, 0, 0, 0), 1);
            g.DrawEllipse(pen, 1, 1, 14, 14);
        }

        var handle = bitmap.GetHicon();
        return System.Drawing.Icon.FromHandle(handle);
    }

    private void ExitApplication()
    {
        _viewModel?.StopMonitoring();
        _trayIcon?.Dispose();
        _statusWindow?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _viewModel?.StopMonitoring();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
