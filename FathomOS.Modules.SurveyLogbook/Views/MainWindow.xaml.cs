// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Views/MainWindow.xaml.cs
// Purpose: Main window code-behind with theme loading and initialization
// ============================================================================

using System.Windows;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using FathomOS.Modules.SurveyLogbook.ViewModels;

namespace FathomOS.Modules.SurveyLogbook.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    private readonly DispatcherTimer _clockTimer;
    private MainViewModel? _viewModel;

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    public MainWindow()
    {
        // Load theme BEFORE InitializeComponent (as per integration guide)
        LoadTheme();
        
        InitializeComponent();
        
        // Initialize clock timer for status bar
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += ClockTimer_Tick;
        _clockTimer.Start();
        
        // Set up window events
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    /// <summary>
    /// Loads the theme resource dictionary.
    /// </summary>
    private void LoadTheme()
    {
        try
        {
            // Default to dark theme
            var themeUri = new Uri("/FathomOS.Modules.SurveyLogbook;component/Themes/DarkTheme.xaml", UriKind.Relative);
            var themeDictionary = new ResourceDictionary { Source = themeUri };
            Resources.MergedDictionaries.Add(themeDictionary);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load theme: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles window loaded event.
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Create and set the ViewModel
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            // Allow UI to fully initialize before any async operations
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to initialize Survey Logbook:\n\n{ex.Message}",
                "Initialization Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles window closing event.
    /// </summary>
    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            // Stop the clock timer
            _clockTimer.Stop();
            
            // Check for unsaved changes
            if (_viewModel != null && _viewModel.SurveyLogViewModel.TotalEntries > 0)
            {
                var result = System.Windows.MessageBox.Show(
                    "Do you want to save the log before closing?",
                    "Save Log",
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);

                switch (result)
                {
                    case System.Windows.MessageBoxResult.Yes:
                        await _viewModel.SaveAsync();
                        break;
                    case System.Windows.MessageBoxResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            }

            // Stop monitoring services
            _viewModel?.Stop();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during window closing: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the clock display in the status bar.
    /// </summary>
    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        // The time is bound via XAML, but we can force update here if needed
        // CurrentTimeText.Text = DateTime.Now.ToString("HH:mm:ss");
    }

    /// <summary>
    /// Switches the application theme.
    /// </summary>
    /// <param name="themeName">Name of the theme (Dark, Light, Modern, Gradient)</param>
    public void SwitchTheme(string themeName)
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
            var themeUri = new Uri($"/FathomOS.Modules.SurveyLogbook;component/Themes/{themeName}Theme.xaml", UriKind.Relative);
            var themeDictionary = new ResourceDictionary { Source = themeUri };
            Resources.MergedDictionaries.Add(themeDictionary);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to switch theme to {themeName}: {ex.Message}");
        }
    }
}
