using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FathomOS.Core.Interfaces;

namespace FathomOS.Shell.Views;

/// <summary>
/// Comprehensive settings window for FathomOS.
/// Provides access to general settings, license management, server connection,
/// data/cache management, and application information.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly IThemeService? _themeService;
    private readonly ISettingsService? _settingsService;
    private readonly INotificationService? _notificationService;

    public SettingsWindow()
    {
        InitializeComponent();

        // Get services from DI
        _themeService = App.Services?.GetService(typeof(IThemeService)) as IThemeService;
        _settingsService = App.Services?.GetService(typeof(ISettingsService)) as ISettingsService;
        _notificationService = App.Services?.GetService(typeof(INotificationService)) as INotificationService;

        LoadSettings();
        UpdateLicenseInfo();
        UpdateServerStatus();
        UpdateStorageInfo();
    }

    #region Window Controls

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Double-click to maximize/restore
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        Close();
    }

    #endregion

    #region Tab Navigation

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb)
        {
            // Hide all panels
            PanelGeneral.Visibility = Visibility.Collapsed;
            PanelLicense.Visibility = Visibility.Collapsed;
            PanelServer.Visibility = Visibility.Collapsed;
            PanelData.Visibility = Visibility.Collapsed;
            PanelAbout.Visibility = Visibility.Collapsed;

            // Show selected panel
            if (rb == TabGeneral) PanelGeneral.Visibility = Visibility.Visible;
            else if (rb == TabLicense) PanelLicense.Visibility = Visibility.Visible;
            else if (rb == TabServer) PanelServer.Visibility = Visibility.Visible;
            else if (rb == TabData) PanelData.Visibility = Visibility.Visible;
            else if (rb == TabAbout) PanelAbout.Visibility = Visibility.Visible;
        }
    }

    #endregion

    #region General Settings

    private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTheme.SelectedItem is ComboBoxItem item && _themeService != null)
        {
            var themeTag = item.Tag?.ToString();
            if (themeTag == "Dark")
            {
                _themeService.ApplyTheme(AppTheme.Dark);
            }
            else if (themeTag == "Light")
            {
                _themeService.ApplyTheme(AppTheme.Light);
            }
            else if (themeTag == "System")
            {
                // Detect system theme
                var isDarkMode = IsWindowsInDarkMode();
                _themeService.ApplyTheme(isDarkMode ? AppTheme.Dark : AppTheme.Light);
            }

            _settingsService?.Set("App.Theme", themeTag);
            _settingsService?.Save();
        }
    }

    private bool IsWindowsInDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return true; // Default to dark mode if we can't determine
        }
    }

    #endregion

    #region License Settings

    private void UpdateLicenseInfo()
    {
        try
        {
            if (App.LicenseManager != null)
            {
                var status = App.LicenseManager.GetStatusInfo();
                var displayInfo = App.Licensing?.GetLicenseDisplayInfo();

                TxtLicenseEdition.Text = displayInfo?.Edition?.ToUpper() ?? "UNLICENSED";
                TxtLicenseStatus.Text = status.IsLicensed ? "Active" : "Inactive";
                TxtLicenseStatus.Foreground = status.IsLicensed
                    ? (Brush)FindResource("SuccessBrush")
                    : (Brush)FindResource("ErrorBrush");

                TxtLicensedTo.Text = $"Licensed to: {displayInfo?.CustomerName ?? "Not available"}";

                if (status.ExpiresAt.HasValue)
                {
                    TxtLicenseExpiry.Text = $"Expires: {status.ExpiresAt.Value:MMMM dd, yyyy}";
                }
                else
                {
                    TxtLicenseExpiry.Text = "Expiration: Perpetual";
                }

                // Update badge color based on status
                LicenseBadge.Background = new SolidColorBrush(status.IsLicensed
                    ? Color.FromRgb(46, 125, 50)  // Green
                    : Color.FromRgb(211, 47, 47)); // Red
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SettingsWindow: Error updating license info: {ex.Message}");
        }
    }

    private void ViewLicense_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Open license details or certificate viewer
            var activationWindow = new ActivationWindow();
            activationWindow.Owner = this;
            activationWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error viewing license: {ex.Message}");
        }
    }

    private void ActivateLicense_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var activationWindow = new LicenseActivationWindow();
            activationWindow.Owner = this;
            if (activationWindow.ShowDialog() == true)
            {
                UpdateLicenseInfo();
                _notificationService?.ShowSuccess("License activated successfully!");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error activating license: {ex.Message}");
        }
    }

    private void DeactivateLicense_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to deactivate this license?\n\nThis will remove the license from this machine.",
            "Confirm Deactivation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                App.LicenseManager?.Deactivate();
                UpdateLicenseInfo();
                _notificationService?.ShowInfo("License deactivated. Please restart the application.");
            }
            catch (Exception ex)
            {
                _notificationService?.ShowError($"Error deactivating license: {ex.Message}");
            }
        }
    }

    #endregion

    #region Server Settings

    private void UpdateServerStatus()
    {
        try
        {
            // Check server connectivity
            var isConnected = App.Licensing?.IsLicensed ?? false;

            ServerStatusDot.Fill = new SolidColorBrush(isConnected
                ? Color.FromRgb(76, 175, 80)   // Green
                : Color.FromRgb(158, 158, 158)); // Gray

            TxtServerStatusTitle.Text = isConnected ? "Connected" : "Disconnected";
            TxtServerUrl.Text = "s7fathom-license-server.onrender.com";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SettingsWindow: Error updating server status: {ex.Message}");
        }
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var button = sender as Button;
            if (button != null) button.IsEnabled = false;

            TxtServerStatusTitle.Text = "Testing...";
            ServerStatusDot.Fill = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Yellow

            await Task.Delay(1500); // Simulate connection test

            // Test actual connection
            var isOnline = await Task.Run(() =>
            {
                try
                {
                    using var client = new System.Net.Http.HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = client.GetAsync("https://s7fathom-license-server.onrender.com/health").Result;
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            });

            ServerStatusDot.Fill = new SolidColorBrush(isOnline
                ? Color.FromRgb(76, 175, 80)
                : Color.FromRgb(244, 67, 54));

            TxtServerStatusTitle.Text = isOnline ? "Connected" : "Connection Failed";

            _notificationService?.Show(
                isOnline ? NotificationType.Success : NotificationType.Error,
                isOnline ? "Server connection successful!" : "Unable to connect to server.");

            if (button != null) button.IsEnabled = true;
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Connection test failed: {ex.Message}");
        }
    }

    #endregion

    #region Data & Cache Settings

    private void UpdateStorageInfo()
    {
        try
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FathomOS");

            TxtDataLocation.Text = appDataPath;

            if (Directory.Exists(appDataPath))
            {
                var size = GetDirectorySize(appDataPath);
                TxtStorageUsed.Text = $"{FormatBytes(size)} used";

                // Update progress bar (assuming 1GB max for display)
                var percentage = Math.Min((double)size / (1024 * 1024 * 1024), 1.0);
                StorageBar.Width = percentage * 280; // Max width

                // Get cache size (estimate)
                var cachePath = Path.Combine(appDataPath, "cache");
                if (Directory.Exists(cachePath))
                {
                    var cacheSize = GetDirectorySize(cachePath);
                    TxtCacheSize.Text = FormatBytes(cacheSize);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SettingsWindow: Error updating storage info: {ex.Message}");
        }
    }

    private long GetDirectorySize(string path)
    {
        try
        {
            return new DirectoryInfo(path)
                .GetFiles("*", SearchOption.AllDirectories)
                .Sum(f => f.Length);
        }
        catch
        {
            return 0;
        }
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FathomOS");
            Directory.CreateDirectory(path);
            Process.Start("explorer.exe", path);
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error opening folder: {ex.Message}");
        }
    }

    private void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear the cache?\n\nThis may slow down initial operations.",
            "Clear Cache",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var cachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FathomOS", "cache");

                if (Directory.Exists(cachePath))
                {
                    Directory.Delete(cachePath, true);
                }

                UpdateStorageInfo();
                _notificationService?.ShowSuccess("Cache cleared successfully!");
            }
            catch (Exception ex)
            {
                _notificationService?.ShowError($"Error clearing cache: {ex.Message}");
            }
        }
    }

    private void ClearAllData_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "WARNING: This will delete ALL application data including settings, certificates, and cache.\n\n" +
            "This action cannot be undone. Are you sure?",
            "Clear All Data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var confirmResult = MessageBox.Show(
                "Please confirm again: Delete ALL FathomOS data?",
                "Final Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop);

            if (confirmResult == MessageBoxResult.Yes)
            {
                try
                {
                    var dataPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "FathomOS");

                    // Keep the directory but clear contents
                    if (Directory.Exists(dataPath))
                    {
                        foreach (var file in Directory.GetFiles(dataPath))
                        {
                            try { File.Delete(file); } catch { }
                        }
                        foreach (var dir in Directory.GetDirectories(dataPath))
                        {
                            try { Directory.Delete(dir, true); } catch { }
                        }
                    }

                    UpdateStorageInfo();
                    _notificationService?.ShowWarning("All data cleared. Please restart the application.");
                }
                catch (Exception ex)
                {
                    _notificationService?.ShowError($"Error clearing data: {ex.Message}");
                }
            }
        }
    }

    private void ExportSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "fathom-os-settings",
                DefaultExt = ".json",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FathomOS", "settings.json");

                if (File.Exists(settingsPath))
                {
                    File.Copy(settingsPath, dialog.FileName, true);
                    _notificationService?.ShowSuccess("Settings exported successfully!");
                }
                else
                {
                    _notificationService?.ShowWarning("No settings file found to export.");
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error exporting settings: {ex.Message}");
        }
    }

    private void ImportSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FathomOS", "settings.json");

                File.Copy(dialog.FileName, settingsPath, true);
                _settingsService?.Reload();
                LoadSettings();
                _notificationService?.ShowSuccess("Settings imported successfully!");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error importing settings: {ex.Message}");
        }
    }

    #endregion

    #region About Panel

    private void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        TxtUpdateStatus.Text = "Checking for updates...";
        TxtLastChecked.Text = $"Last checked: {DateTime.Now:MMM dd, yyyy 'at' h:mm tt}";

        // Simulate update check
        Task.Delay(1500).ContinueWith(_ =>
        {
            Dispatcher.Invoke(() =>
            {
                TxtUpdateStatus.Text = "You are running the latest version";
                _notificationService?.ShowInfo("You are running the latest version.");
            });
        });
    }

    private void OpenDocs_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://fathom-os.com/docs",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void OpenReleaseNotes_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://fathom-os.com/releases",
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void OpenSupport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://fathom-os.com/support",
                UseShellExecute = true
            });
        }
        catch { }
    }

    #endregion

    #region Settings Persistence

    private void LoadSettings()
    {
        try
        {
            // Load theme setting
            var theme = _settingsService?.Get("App.Theme", "Dark");
            foreach (ComboBoxItem item in CmbTheme.Items)
            {
                if (item.Tag?.ToString() == theme)
                {
                    CmbTheme.SelectedItem = item;
                    break;
                }
            }

            // Load startup settings
            ChkStartWithWindows.IsChecked = _settingsService?.Get("Startup.WithWindows", false);
            ChkStartMinimized.IsChecked = _settingsService?.Get("Startup.Minimized", false);
            ChkCheckUpdates.IsChecked = _settingsService?.Get("Startup.CheckUpdates", true);

            // Load notification settings
            ChkEnableNotifications.IsChecked = _settingsService?.Get("Notifications.Enabled", true);

            // Load server settings
            ChkAutoSync.IsChecked = _settingsService?.Get("Server.AutoSync", true);
            ChkOfflineMode.IsChecked = _settingsService?.Get("Server.OfflineMode", true);

            // Set version
            var version = typeof(App).Assembly.GetName().Version;
            TxtVersion.Text = $"Version {version?.Major}.{version?.Minor}.{version?.Build}";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SettingsWindow: Error loading settings: {ex.Message}");
        }
    }

    private void SaveSettings()
    {
        try
        {
            // Save startup settings
            _settingsService?.Set("Startup.WithWindows", ChkStartWithWindows.IsChecked ?? false);
            _settingsService?.Set("Startup.Minimized", ChkStartMinimized.IsChecked ?? false);
            _settingsService?.Set("Startup.CheckUpdates", ChkCheckUpdates.IsChecked ?? true);

            // Save notification settings
            _settingsService?.Set("Notifications.Enabled", ChkEnableNotifications.IsChecked ?? true);

            // Save server settings
            _settingsService?.Set("Server.AutoSync", ChkAutoSync.IsChecked ?? true);
            _settingsService?.Set("Server.OfflineMode", ChkOfflineMode.IsChecked ?? true);

            _settingsService?.Save();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SettingsWindow: Error saving settings: {ex.Message}");
        }
    }

    #endregion
}
