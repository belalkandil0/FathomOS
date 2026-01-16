using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;
using FathomOS.Modules.GnssCalibration.ViewModels;

namespace FathomOS.Modules.GnssCalibration.Views;

/// <summary>
/// Main window for GNSS Calibration module - hosts the 7-step wizard.
/// </summary>
public partial class MainWindow : MetroWindow
{
    public MainWindow()
    {
        // Load theme BEFORE InitializeComponent
        LoadTheme(true); // Start with dark theme
        
        InitializeComponent();
        
        // Subscribe to theme change events from ViewModel
        if (DataContext is MainViewModel vm)
        {
            vm.OnThemeChanged += OnThemeChanged;
            vm.OnShowHelpWindow += OnShowHelpWindow;
            
            // Apply saved window state
            ApplyWindowState(vm);
        }
        
        Loaded += MainWindow_Loaded;
        
        // Subscribe to keyboard events
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Re-subscribe in case DataContext was set after construction
        if (DataContext is MainViewModel vm)
        {
            vm.OnThemeChanged -= OnThemeChanged; // Prevent double subscription
            vm.OnThemeChanged += OnThemeChanged;
            vm.OnShowHelpWindow -= OnShowHelpWindow;
            vm.OnShowHelpWindow += OnShowHelpWindow;
        }
    }
    
    /// <summary>
    /// Handles keyboard shortcuts.
    /// </summary>
    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            if (vm.HandleKeyDown(e.Key, Keyboard.Modifiers))
            {
                e.Handled = true;
            }
        }
    }
    
    /// <summary>
    /// Applies saved window state from settings.
    /// </summary>
    private void ApplyWindowState(MainViewModel vm)
    {
        try
        {
            var settings = vm.Settings;
            
            if (settings.WindowWidth > 0)
                Width = settings.WindowWidth;
            if (settings.WindowHeight > 0)
                Height = settings.WindowHeight;
            if (settings.IsMaximized)
                WindowState = WindowState.Maximized;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply window state: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Saves window state to settings.
    /// </summary>
    private void SaveWindowState(MainViewModel vm)
    {
        try
        {
            var settings = vm.Settings;
            
            settings.IsMaximized = WindowState == WindowState.Maximized;
            
            // Only save size if not maximized
            if (WindowState != WindowState.Maximized)
            {
                settings.WindowWidth = Width;
                settings.WindowHeight = Height;
            }
            
            // Save all settings
            vm.SaveSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save window state: {ex.Message}");
        }
    }

    private void OnThemeChanged(object? sender, bool isDarkTheme)
    {
        SwitchTheme(isDarkTheme);
    }
    
    private void OnShowHelpWindow(object? sender, EventArgs e)
    {
        var helpWindow = new HelpWindow
        {
            Owner = this
        };
        helpWindow.ShowDialog();
    }

    private void LoadTheme(bool useDarkTheme)
    {
        try
        {
            var themeName = useDarkTheme ? "DarkTheme" : "LightTheme";
            var themeUri = new Uri($"/FathomOS.Modules.GnssCalibration;component/Themes/{themeName}.xaml", UriKind.Relative);
            var themeDictionary = new ResourceDictionary { Source = themeUri };
            Resources.MergedDictionaries.Add(themeDictionary);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load theme: {ex.Message}");
        }
    }

    /// <summary>
    /// Switches between dark and light themes.
    /// </summary>
    public void SwitchTheme(bool useDarkTheme)
    {
        try
        {
            // Remove existing theme
            var existingTheme = Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.ToString().Contains("Theme.xaml") == true);
            
            if (existingTheme != null)
            {
                Resources.MergedDictionaries.Remove(existingTheme);
            }

            // Load new theme
            var themeName = useDarkTheme ? "DarkTheme" : "LightTheme";
            var themeUri = new Uri($"/FathomOS.Modules.GnssCalibration;component/Themes/{themeName}.xaml", UriKind.Relative);
            var themeDictionary = new ResourceDictionary { Source = themeUri };
            Resources.MergedDictionaries.Add(themeDictionary);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to switch theme: {ex.Message}");
        }
    }
    
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Check for unsaved changes
        if (DataContext is MainViewModel vm && vm.HasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save before closing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            
            if (result == MessageBoxResult.Yes)
            {
                vm.SaveProjectCommand.Execute(null);
            }
        }
        
        base.OnClosing(e);
    }
    
    protected override void OnClosed(EventArgs e)
    {
        // Save window state and settings
        if (DataContext is MainViewModel vm)
        {
            SaveWindowState(vm);
            vm.OnThemeChanged -= OnThemeChanged;
            vm.OnShowHelpWindow -= OnShowHelpWindow;
        }
        
        base.OnClosed(e);
    }
}
