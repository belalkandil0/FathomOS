using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FathomOS.Core.Interfaces;
using FathomOS.Shell.Services;
using LicensingSystem.Client;
using LicensingSystem.Shared;

namespace FathomOS.Shell.Views;

/// <summary>
/// FathomOS Dashboard - Main application window with module tiles and group navigation
/// </summary>
public partial class DashboardWindow : Window
{
    private readonly ModuleManager _moduleManager;
    private readonly List<RecentProject> _recentProjects = new();
    private bool _isDarkTheme = true;
    private ModuleGroupMetadata? _currentGroup = null; // null = main dashboard
    private DispatcherTimer? _licenseCheckTimer;
    
    public DashboardWindow()
    {
        InitializeComponent();
        
        _moduleManager = App.Current.ModuleManager;
        
        Loaded += DashboardWindow_Loaded;
        Closing += DashboardWindow_Closing;
        Closed += DashboardWindow_Closed;
    }
    
    private void DashboardWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("DEBUG: DashboardWindow_Loaded starting...");
            ShowMainDashboard();
            System.Diagnostics.Debug.WriteLine("DEBUG: ShowMainDashboard completed");
            LoadRecentProjects();
            System.Diagnostics.Debug.WriteLine("DEBUG: LoadRecentProjects completed");
            UpdateModuleCount();
            System.Diagnostics.Debug.WriteLine("DEBUG: UpdateModuleCount completed");
            UpdateThemeToggleIcon();
            System.Diagnostics.Debug.WriteLine("DEBUG: UpdateThemeToggleIcon completed");
            UpdateLicenseStatus();
            System.Diagnostics.Debug.WriteLine("DEBUG: UpdateLicenseStatus completed");
            UpdateWindowTitle();
            System.Diagnostics.Debug.WriteLine("DEBUG: UpdateWindowTitle completed");
            _ = CheckServerConnectionAsync();
            System.Diagnostics.Debug.WriteLine("DEBUG: CheckServerConnectionAsync started");
            StartLicenseMonitoring();
            System.Diagnostics.Debug.WriteLine("DEBUG: StartLicenseMonitoring completed");
            System.Diagnostics.Debug.WriteLine("DEBUG: DashboardWindow_Loaded FINISHED - window should stay open!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: EXCEPTION in DashboardWindow_Loaded: {ex}");
            throw;
        }
    }
    
    /// <summary>
    /// Handle window closing - stop the license timer
    /// </summary>
    private void DashboardWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("DEBUG: DashboardWindow_Closing called!");
        System.Diagnostics.Debug.WriteLine($"DEBUG: Stack trace: {Environment.StackTrace}");
        _licenseCheckTimer?.Stop();
    }
    
    /// <summary>
    /// Handle window closed - cleanup
    /// Note: App shutdown is handled by ShutdownMode.OnMainWindowClose
    /// </summary>
    private void DashboardWindow_Closed(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("DEBUG: DashboardWindow_Closed called - app will shutdown via ShutdownMode");
        // ShutdownMode.OnMainWindowClose will handle shutdown automatically
        // No need to call Application.Current.Shutdown() here
    }
    
    /// <summary>
    /// Update window title with client name and edition
    /// Format: "FathomOS - Client Name - Edition"
    /// </summary>
    private void UpdateWindowTitle()
    {
        try
        {
            var clientName = App.ClientName;
            var edition = App.LicenseEdition;

            if (!string.IsNullOrEmpty(clientName) && !string.IsNullOrEmpty(edition))
            {
                Title = $"FathomOS - {clientName} - {edition}";
            }
            else if (!string.IsNullOrEmpty(edition))
            {
                Title = $"FathomOS - {edition}";
            }
            else
            {
                Title = "FathomOS";
            }
        }
        catch
        {
            Title = "FathomOS";
        }
    }

    /// <summary>
    /// Check server connection status and update indicator
    /// </summary>
    private async Task CheckServerConnectionAsync()
    {
        try
        {
            // Set initial state
            await Dispatcher.InvokeAsync(() =>
            {
                ServerStatusDot.Fill = new SolidColorBrush(Color.FromRgb(110, 118, 129)); // Gray
                TxtServerStatus.Text = "Checking...";
            });

            // Try to check server connectivity
            bool isConnected = false;
            try
            {
                using var httpClient = new System.Net.Http.HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                var response = await httpClient.GetAsync("https://s7fathom-license-server.onrender.com/api/health");
                isConnected = response.IsSuccessStatusCode;
            }
            catch
            {
                isConnected = false;
            }

            // Update UI
            await Dispatcher.InvokeAsync(() =>
            {
                if (isConnected)
                {
                    ServerStatusDot.Fill = new SolidColorBrush(Color.FromRgb(63, 185, 80)); // Green
                    TxtServerStatus.Text = "Online";
                }
                else
                {
                    ServerStatusDot.Fill = new SolidColorBrush(Color.FromRgb(248, 81, 73)); // Red
                    TxtServerStatus.Text = "Offline";
                }
            });
        }
        catch
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ServerStatusDot.Fill = new SolidColorBrush(Color.FromRgb(248, 81, 73)); // Red
                TxtServerStatus.Text = "Offline";
            });
        }
    }

    /// <summary>
    /// Start background license monitoring
    /// </summary>
    private void StartLicenseMonitoring()
    {
        _licenseCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(5)
        };
        _licenseCheckTimer.Tick += async (s, e) => await CheckLicenseStatusAsync();
        _licenseCheckTimer.Start();
    }
    
    /// <summary>
    /// Background license check - silent unless revoked/expired
    /// </summary>
    private async Task CheckLicenseStatusAsync()
    {
        try
        {
            if (App.LicenseManager == null) return;

            var result = await App.LicenseManager.ForceServerCheckAsync();

            // Update UI on dispatcher thread
            await Dispatcher.InvokeAsync(() =>
            {
                UpdateLicenseStatus();
                UpdateWindowTitle();
            });

            // Also check server connectivity in the background
            _ = CheckServerConnectionAsync();
            
            // Only take action if server CONFIRMED revocation or expiration
            if (result.Status == LicenseStatus.Revoked)
            {
                _licenseCheckTimer?.Stop();
                
                // CRITICAL: Clear stored license to prevent reuse
                App.LicenseManager.Deactivate();
                
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        "Your license has been revoked.\n\n" +
                        "The stored license has been removed. Please contact support.",
                        "License Revoked",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Application.Current.Shutdown();
                });
            }
            else if (result.Status == LicenseStatus.Expired && result.GraceDaysRemaining <= 0)
            {
                _licenseCheckTimer?.Stop();
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        "Your license has expired. Please renew to continue using Fathom OS.",
                        "License Expired",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Application.Current.Shutdown();
                });
            }
            // If offline or server unreachable, do NOTHING - let user continue
        }
        catch
        {
            // Silently fail - don't interrupt user if check fails
        }
    }
    
    /// <summary>
    /// Update license status display in footer
    /// </summary>
    private void UpdateLicenseStatus()
    {
        // Get license info from App
        var edition = App.LicenseEdition;
        var statusText = App.LicenseStatusText;
        var isLicensed = App.IsLicensed;
        var tier = App.CurrentTier;
        
        // Update badge
        TxtLicenseEdition.Text = edition;
        
        // Set badge color based on tier
        if (!isLicensed)
        {
            LicenseBadge.Background = new SolidColorBrush(Color.FromRgb(198, 40, 40)); // Red
            TxtLicenseStatus.Text = "Unlicensed";
        }
        else if (tier == "Enterprise")
        {
            LicenseBadge.Background = new SolidColorBrush(Color.FromRgb(156, 39, 176)); // Purple for Enterprise
            TxtLicenseStatus.Text = statusText;
        }
        else if (tier == "Professional")
        {
            LicenseBadge.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)); // Green for Professional
            TxtLicenseStatus.Text = statusText;
        }
        else
        {
            LicenseBadge.Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)); // Blue for Basic/Custom
            TxtLicenseStatus.Text = statusText;
        }
        
        // Refresh module tiles to update lock icons
        if (_currentGroup != null)
        {
            ShowGroupModules(_currentGroup);
        }
        else
        {
            LoadModuleTiles();
        }
    }

    /// <summary>
    /// Get group metadata by ID
    /// </summary>
    private ModuleGroupMetadata? GetGroupMetadata(string groupId)
    {
        return _moduleManager.GetGroup(groupId);
    }
    
    /// <summary>
    /// Handle license badge click to show license details
    /// </summary>
    private void LicenseBadge_Click(object sender, MouseButtonEventArgs e)
    {
        var edition = App.LicenseEdition;
        var status = App.LicenseStatusText;
        var isLicensed = App.IsLicensed;
        
        var message = isLicensed
            ? $"License Status: {status}\nEdition: {edition}\n\nThank you for using Fathom OS!"
            : $"License Status: Not Licensed\n\nPlease activate your copy of Fathom OS to unlock all features.";
        
        var result = MessageBox.Show(
            message + "\n\nWould you like to manage your license?",
            "License Information",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);
        
        if (result == MessageBoxResult.Yes)
        {
            // Show activation window
            var activationWindow = new ActivationWindow();
            if (activationWindow.ShowDialog() == true)
            {
                // Refresh license status
                UpdateLicenseStatus();
            }
        }
    }
    
    /// <summary>
    /// Handle certificates button click to show certificate manager
    /// </summary>
    private void CertificatesButton_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            // Open the Certificate List Window with LicenseManager
            var certWindow = new CertificateListWindow(App.LicenseManager);
            certWindow.Owner = this;
            certWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error opening Certificate Manager:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Update total module count in footer
    /// </summary>
    private void UpdateModuleCount()
    {
        // Use DiscoveredModuleCount which includes all modules (root + grouped)
        TxtModuleCount.Text = _moduleManager.DiscoveredModuleCount.ToString();
    }
    
    #region Navigation
    
    /// <summary>
    /// Show the main dashboard with groups and root modules
    /// </summary>
    private void ShowMainDashboard()
    {
        _currentGroup = null;
        
        // Hide group navigation header
        GroupNavigationHeader.Visibility = Visibility.Collapsed;
        
        // Show recent projects section
        RecentProjectsSection.Visibility = Visibility.Visible;
        RecentProjectsSeparator.Visibility = Visibility.Visible;
        
        // Update header text
        TxtModulesHeader.Text = "MODULES";
        
        // Load groups
        LoadGroupTiles();
        
        // Load root modules
        LoadModuleTiles();
    }
    
    /// <summary>
    /// Navigate into a module group
    /// </summary>
    private void ShowGroupModules(ModuleGroupMetadata group)
    {
        _currentGroup = group;

        // Show group navigation header with Home button
        GroupNavigationHeader.Visibility = Visibility.Visible;
        TxtCurrentGroupName.Text = group.DisplayName;
        TxtCurrentGroupDescription.Text = group.Description;

        // Hide groups section and recent projects
        GroupsSection.Visibility = Visibility.Collapsed;
        RecentProjectsSection.Visibility = Visibility.Collapsed;
        RecentProjectsSeparator.Visibility = Visibility.Collapsed;

        // Update header
        TxtModulesHeader.Text = $"{group.DisplayName.ToUpper()} MODULES";

        // Load group's modules (using metadata)
        ModuleTiles.Items.Clear();

        var groupModules = _moduleManager.GetModulesInGroup(group.GroupId);

        if (groupModules.Count == 0)
        {
            NoModulesPanel.Visibility = Visibility.Visible;
        }
        else
        {
            NoModulesPanel.Visibility = Visibility.Collapsed;

            foreach (var module in groupModules)
            {
                var tile = CreateModuleTile(module, group.GroupId);
                ModuleTiles.Items.Add(tile);
            }
        }
    }
    
    /// <summary>
    /// Handle Home button click - return to main dashboard
    /// </summary>
    private void Home_Click(object sender, RoutedEventArgs e)
    {
        ShowMainDashboard();
    }
    
    #endregion
    
    #region Window Chrome Controls
    
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            Maximize_Click(sender, e);
        }
        else
        {
            if (WindowState == WindowState.Maximized)
            {
                var point = PointToScreen(e.GetPosition(this));
                WindowState = WindowState.Normal;
                Left = point.X - (ActualWidth / 2);
                Top = point.Y - 15;
            }
            DragMove();
        }
    }
    
    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    
    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            BtnMaximize.Content = "\uE739";
            MainBorder.CornerRadius = new CornerRadius(10);
        }
        else
        {
            WindowState = WindowState.Maximized;
            BtnMaximize.Content = "\uE923";
            MainBorder.CornerRadius = new CornerRadius(0);
        }
    }
    
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    #endregion
    
    #region Theme Toggle
    
    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        var themeName = _isDarkTheme ? "Dark" : "Light";
        App.Current.ApplyTheme(themeName);
        UpdateThemeToggleIcon();
        
        // Refresh current view
        if (_currentGroup != null)
            ShowGroupModules(_currentGroup);
        else
            ShowMainDashboard();
    }
    
    private void UpdateThemeToggleIcon()
    {
        BtnThemeToggle.Content = _isDarkTheme ? "🌙" : "☀️";
        BtnThemeToggle.ToolTip = _isDarkTheme ? "Switch to Light Theme" : "Switch to Dark Theme";
    }
    
    #endregion
    
    #region Group Tiles
    
    /// <summary>
    /// Load module group tiles
    /// </summary>
    private void LoadGroupTiles()
    {
        GroupTiles.Items.Clear();

        if (_moduleManager.Groups.Count == 0)
        {
            GroupsSection.Visibility = Visibility.Collapsed;
        }
        else
        {
            GroupsSection.Visibility = Visibility.Visible;

            foreach (var group in _moduleManager.Groups)
            {
                var tile = CreateGroupTile(group);
                GroupTiles.Items.Add(tile);
            }
        }
    }

    /// <summary>
    /// Create a tile button for a module group
    /// </summary>
    private Button CreateGroupTile(IModuleGroupMetadata group)
    {
        var button = new Button
        {
            Style = (Style)FindResource("ModuleTileStyle"),
            Tag = group.GroupId,
            ToolTip = $"{group.Description}\n({group.ModuleIds.Count} modules)"
        };

        var content = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(15)
        };

        // Icon container
        var iconContainer = new Border
        {
            Width = 72,
            Height = 72,
            Margin = new Thickness(0, 0, 0, 12),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Try to load group icon
        if (!string.IsNullOrEmpty(group.IconPath) && File.Exists(group.IconPath))
        {
            try
            {
                var image = new Image
                {
                    Source = new BitmapImage(new Uri(group.IconPath)),
                    Width = 64,
                    Height = 64
                };
                iconContainer.Child = image;
            }
            catch
            {
                iconContainer.Child = CreateGroupDefaultIcon(group.DisplayName);
            }
        }
        else
        {
            iconContainer.Child = CreateGroupDefaultIcon(group.DisplayName);
        }

        content.Children.Add(iconContainer);

        // Group name
        var nameBlock = new TextBlock
        {
            Text = group.DisplayName,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 160,
            Foreground = (Brush)FindResource("TextBrush")
        };
        content.Children.Add(nameBlock);

        // Module count
        var countBlock = new TextBlock
        {
            Text = $"{group.ModuleIds.Count} modules",
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            Foreground = (Brush)FindResource("SecondaryTextBrush"),
            Margin = new Thickness(0, 4, 0, 0),
            Opacity = 0.7
        };
        content.Children.Add(countBlock);

        // "Click to open" hint
        var hintBlock = new TextBlock
        {
            Text = "▶ Open",
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            Foreground = (Brush)FindResource("AccentBrush"),
            Margin = new Thickness(0, 2, 0, 0)
        };
        content.Children.Add(hintBlock);

        button.Content = content;
        button.Click += GroupTile_Click;

        return button;
    }
    
    /// <summary>
    /// Create a default icon for a group with folder symbol
    /// </summary>
    private Border CreateGroupDefaultIcon(string name)
    {
        var border = new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(12),
            Background = new LinearGradientBrush(
                Color.FromRgb(0, 150, 136),  // Teal
                Color.FromRgb(0, 121, 107),  // Darker teal
                45)
        };
        
        var text = new TextBlock
        {
            Text = "📁",
            FontSize = 32,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        border.Child = text;
        return border;
    }
    
    /// <summary>
    /// Handle group tile click - navigate into group
    /// </summary>
    private void GroupTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string groupId)
        {
            var group = GetGroupMetadata(groupId);
            if (group != null)
            {
                ShowGroupModules(group);
            }
        }
    }
    
    #endregion
    
    #region Module Tiles
    
    /// <summary>
    /// Create module tiles from discovered modules (metadata only, no DLL loading)
    /// </summary>
    private void LoadModuleTiles()
    {
        ModuleTiles.Items.Clear();

        if (_moduleManager.Modules.Count == 0)
        {
            NoModulesPanel.Visibility = _moduleManager.Groups.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        else
        {
            NoModulesPanel.Visibility = Visibility.Collapsed;

            foreach (var module in _moduleManager.Modules)
            {
                var tile = CreateModuleTile(module, null);
                ModuleTiles.Items.Add(tile);
            }
        }
    }

    /// <summary>
    /// Create a tile button for a module using metadata (no DLL loading)
    /// </summary>
    private Button CreateModuleTile(IModuleMetadata module, string? groupId)
    {
        // Check if module is licensed
        bool isModuleLicensed = App.IsModuleLicensed(module.ModuleId);

        var button = new Button
        {
            Style = (Style)FindResource("ModuleTileStyle"),
            Tag = module.ModuleId,
            ToolTip = isModuleLicensed
                ? module.Description
                : $"{module.Description}\n\n🔒 This module is not included in your license.\nClick to learn more about upgrading.",
            Opacity = isModuleLicensed ? 1.0 : 0.6
        };

        // Main content grid to allow overlay
        var mainGrid = new Grid();

        var content = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(15)
        };

        // Icon container with lock overlay capability
        var iconGrid = new Grid
        {
            Width = 72,
            Height = 72,
            Margin = new Thickness(0, 0, 0, 12),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var iconContainer = new Border
        {
            Width = 72,
            Height = 72
        };

        try
        {
            // Use IconPath from metadata directly
            var iconPath = module.IconPath;
            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                var image = new Image
                {
                    Source = new BitmapImage(new Uri(iconPath)),
                    Width = 64,
                    Height = 64,
                    Opacity = isModuleLicensed ? 1.0 : 0.5
                };
                iconContainer.Child = image;
            }
            else
            {
                var defaultIcon = CreateDefaultIcon(module.DisplayName);
                if (!isModuleLicensed && defaultIcon is Border border)
                {
                    border.Opacity = 0.5;
                }
                iconContainer.Child = defaultIcon;
            }
        }
        catch
        {
            iconContainer.Child = CreateDefaultIcon(module.DisplayName);
        }

        iconGrid.Children.Add(iconContainer);

        // Add lock icon overlay for unlicensed modules
        if (!isModuleLicensed)
        {
            var lockOverlay = new Border
            {
                Width = 28,
                Height = 28,
                Background = new SolidColorBrush(Color.FromArgb(230, 198, 40, 40)), // Semi-transparent red
                CornerRadius = new CornerRadius(14),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, -4, -4)
            };

            var lockIcon = new TextBlock
            {
                Text = "🔒",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            lockOverlay.Child = lockIcon;
            iconGrid.Children.Add(lockOverlay);
        }

        content.Children.Add(iconGrid);

        // Module name
        var nameBlock = new TextBlock
        {
            Text = module.DisplayName,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 160,
            Foreground = (Brush)FindResource("TextBrush"),
            Opacity = isModuleLicensed ? 1.0 : 0.7
        };
        content.Children.Add(nameBlock);

        // Category label
        var categoryBlock = new TextBlock
        {
            Text = module.Category,
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            Foreground = (Brush)FindResource("SecondaryTextBrush"),
            Margin = new Thickness(0, 4, 0, 0),
            Opacity = isModuleLicensed ? 0.7 : 0.5
        };
        content.Children.Add(categoryBlock);

        // Version or "Upgrade" label
        var versionBlock = new TextBlock
        {
            Text = isModuleLicensed ? $"v{module.Version}" : "Upgrade to unlock",
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            Foreground = isModuleLicensed
                ? (Brush)FindResource("AccentBrush")
                : new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange for upgrade
            Margin = new Thickness(0, 2, 0, 0),
            FontWeight = isModuleLicensed ? FontWeights.Normal : FontWeights.SemiBold
        };
        content.Children.Add(versionBlock);

        mainGrid.Children.Add(content);
        button.Content = mainGrid;
        button.Click += ModuleTile_Click;

        return button;
    }
    
    /// <summary>
    /// Create a default icon with initials
    /// </summary>
    private Border CreateDefaultIcon(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var initials = string.Join("", words.Take(2).Select(w => w[0])).ToUpper();
        
        var border = new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(12),
            Background = (Brush)FindResource("AccentBrush")
        };
        
        var text = new TextBlock
        {
            Text = initials,
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        border.Child = text;
        return border;
    }
    
    /// <summary>
    /// Handle module tile click
    /// </summary>
    private void ModuleTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string moduleId)
        {
            _moduleManager.LaunchModule(moduleId, this);
        }
    }
    
    #endregion
    
    #region Recent Projects
    
    private void LoadRecentProjects()
    {
        _recentProjects.Clear();
        
        if (_recentProjects.Count == 0)
        {
            TxtNoRecentProjects.Visibility = Visibility.Visible;
            RecentProjects.Visibility = Visibility.Collapsed;
        }
        else
        {
            TxtNoRecentProjects.Visibility = Visibility.Collapsed;
            RecentProjects.Visibility = Visibility.Visible;
            RecentProjects.ItemsSource = _recentProjects;
        }
    }
    
    private void RecentProject_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is RecentProject project)
        {
            OpenProject(project.Path);
        }
    }
    
    private void OpenRecentProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string path)
        {
            OpenProject(path);
        }
    }
    
    private void OpenProject(string path)
    {
        if (File.Exists(path))
        {
            _moduleManager.OpenFileWithModule(path, this);
        }
        else
        {
            MessageBox.Show($"Project file not found: {path}", "File Not Found", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    
    #endregion
    
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();

            // Refresh UI after settings change
            UpdateThemeToggleIcon();
            UpdateLicenseStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error opening settings: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}

/// <summary>
/// Recent project entry
/// </summary>
public class RecentProject
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public DateTime LastOpened { get; set; }
}
