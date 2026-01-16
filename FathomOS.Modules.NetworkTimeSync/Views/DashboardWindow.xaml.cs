namespace FathomOS.Modules.NetworkTimeSync.Views;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FathomOS.Modules.NetworkTimeSync.ViewModels;

/// <summary>
/// Interaction logic for DashboardWindow.xaml
/// </summary>
public partial class DashboardWindow : Window
{
    private readonly DashboardViewModel _viewModel;
    private bool _isDarkTheme = true;

    public DashboardWindow()
    {
        InitializeComponent();
        _viewModel = new DashboardViewModel();
        DataContext = _viewModel;

        Closing += DashboardWindow_Closing;
        
        // Set initial theme icon
        UpdateThemeIcon();
    }

    private void DashboardWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TITLE BAR & WINDOW CONTROLS
    // ═══════════════════════════════════════════════════════════════════════

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
        }
        else
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized 
            ? WindowState.Normal 
            : WindowState.Maximized;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // THEME TOGGLE
    // ═══════════════════════════════════════════════════════════════════════

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        ApplyTheme();
        UpdateThemeIcon();
    }

    private void ApplyTheme()
    {
        var themePath = _isDarkTheme
            ? "/FathomOS.Modules.NetworkTimeSync;component/Themes/DarkTheme.xaml"
            : "/FathomOS.Modules.NetworkTimeSync;component/Themes/LightTheme.xaml";

        try
        {
            var newTheme = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            };

            // Find and replace the theme dictionary
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(newTheme);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply theme: {ex.Message}");
        }
    }

    private void UpdateThemeIcon()
    {
        if (ThemeToggleButton != null)
        {
            ThemeToggleButton.Content = _isDarkTheme ? "☀" : "🌙";
            ThemeToggleButton.ToolTip = _isDarkTheme ? "Switch to Light Theme" : "Switch to Dark Theme";
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════════════════

    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.UpdateSelectedCount();
    }

    private void SettingsOverlay_Click(object sender, MouseButtonEventArgs e)
    {
        _viewModel.IsSettingsOpen = false;
    }

    private void AddComputerOverlay_Click(object sender, MouseButtonEventArgs e)
    {
        _viewModel.IsAddComputerOpen = false;
    }

    private void HistoryOverlay_Click(object sender, MouseButtonEventArgs e)
    {
        _viewModel.IsHistoryOpen = false;
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.ContextMenu != null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PUBLIC METHODS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Load a configuration file into the dashboard.
    /// </summary>
    public void LoadConfiguration(string filePath)
    {
        _viewModel.LoadConfiguration(filePath);
    }
}
