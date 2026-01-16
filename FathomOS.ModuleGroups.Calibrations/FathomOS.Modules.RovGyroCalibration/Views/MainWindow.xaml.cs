using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.RovGyroCalibration.ViewModels;

namespace FathomOS.Modules.RovGyroCalibration.Views;

/// <summary>
/// Main window for the ROV Gyro Calibration wizard.
/// Hosts the 7-step "Fathom 7 Process" workflow.
/// </summary>
public partial class MainWindow : MetroWindow
{
    public MainWindow()
    {
        try
        {
            // Load theme resources BEFORE InitializeComponent
            LoadTheme();

            InitializeComponent();

            // Set up the ViewModel
            DataContext = new MainViewModel();

            // Handle window closing to clean up resources
            Closing += MainWindow_Closing;
        }
        catch (Exception ex)
        {
            var msg = $"MainWindow initialization error:\n{ex.Message}";
            if (ex.InnerException != null)
                msg += $"\n\nInner: {ex.InnerException.Message}";
            System.Windows.MessageBox.Show(msg, "Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    /// <summary>
    /// Load the appropriate theme resources
    /// </summary>
    private void LoadTheme()
    {
        try
        {
            var themeUri = new Uri("/FathomOS.Modules.RovGyroCalibration;component/Themes/DarkTheme.xaml", UriKind.Relative);
            var themeDictionary = new ResourceDictionary { Source = themeUri };
            Resources.MergedDictionaries.Add(themeDictionary);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load theme: {ex.Message}");
            // Continue without theme - will use default styles
        }
    }

    /// <summary>
    /// Handle window closing
    /// </summary>
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Check if there's unsaved work
        if (DataContext is MainViewModel vm && vm.DataPoints.Count > 0)
        {
            // Only prompt if processing has started but export not completed
            // For now, we'll just close without prompt
            // TODO: Add dirty flag tracking for unsaved changes
        }
    }

    /// <summary>
    /// Get the ViewModel (for external access if needed)
    /// </summary>
    public MainViewModel? ViewModel => DataContext as MainViewModel;
}
