using System.Linq;
using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.MruCalibration.ViewModels;

namespace FathomOS.Modules.MruCalibration.Views;

/// <summary>
/// Main window for MRU Calibration Module
/// Implements the 7-step wizard workflow
/// </summary>
public partial class MainWindow : MetroWindow
{
    private bool _isDarkTheme = true;
    
    public MainWindow()
    {
        // Load Subsea7 theme BEFORE InitializeComponent
        LoadTheme(true);
        
        InitializeComponent();
        
        // Set DataContext to ViewModel
        var viewModel = new MainViewModel();
        viewModel.ThemeChanged += OnThemeChanged;
        DataContext = viewModel;
    }
    
    private void LoadTheme(bool isDark)
    {
        _isDarkTheme = isDark;
        
        // Clear existing theme
        var existingTheme = Resources.MergedDictionaries.FirstOrDefault(d => 
            d.Source?.ToString().Contains("Theme.xaml") == true);
        if (existingTheme != null)
        {
            Resources.MergedDictionaries.Remove(existingTheme);
        }
        
        // Load appropriate theme
        var themeName = isDark ? "Subsea7DarkTheme" : "Subsea7LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.MruCalibration;component/Themes/{themeName}.xaml", UriKind.Relative);
        
        try
        {
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        }
        catch
        {
            // Fallback to dark theme if light theme doesn't exist
            if (!isDark) return;
            themeUri = new Uri("/FathomOS.Modules.MruCalibration;component/Themes/Subsea7DarkTheme.xaml", UriKind.Relative);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        }
    }
    
    private void OnThemeChanged(object? sender, bool isDark)
    {
        LoadTheme(isDark);
    }
    
    /// <summary>
    /// Access the ViewModel
    /// </summary>
    public MainViewModel ViewModel => (MainViewModel)DataContext;
    
    /// <summary>
    /// Handle window closing - prompt to save if there are unsaved changes
    /// </summary>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (ViewModel.Session.HasAnyData && ViewModel.Session.ModifiedAt.HasValue)
        {
            var result = System.Windows.MessageBox.Show(
                "You have unsaved changes. Do you want to save before closing?",
                "Save Changes?",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
            
            switch (result)
            {
                case MessageBoxResult.Yes:
                    ViewModel.SaveSessionCommand.Execute(null);
                    break;
                case MessageBoxResult.Cancel:
                    e.Cancel = true;
                    return;
            }
        }
        
        // Unsubscribe from events
        if (DataContext is MainViewModel vm)
        {
            vm.ThemeChanged -= OnThemeChanged;
        }
        
        base.OnClosing(e);
    }
}
