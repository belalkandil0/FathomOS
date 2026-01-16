namespace FathomOS.Modules.TreeInclination.Views;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using MahApps.Metro.Controls;
using FathomOS.Modules.TreeInclination.Models;
using FathomOS.Modules.TreeInclination.ViewModels;

public partial class MainWindow : MetroWindow
{
    private bool _currentThemeIsDark = true;

    public MainWindow()
    {
        // Load theme before InitializeComponent
        LoadTheme(true);
        
        // CRITICAL: Set DataContext BEFORE InitializeComponent
        // so that bindings can resolve immediately when XAML is processed
        DataContext = new MainViewModel();
        
        InitializeComponent();
        
        // TODO: When FathomOS.Core.Branding is available, update window title:
        // this.Title = $"Tree Inclination Analysis — {BrandingService.EditionName}";
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            // Subscribe to theme changes
            vm.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(vm.IsDarkTheme))
                {
                    ApplyTheme(vm.IsDarkTheme);
                }
            };
            
            // Apply initial theme
            ApplyTheme(vm.IsDarkTheme);
        }
    }

    private void LoadTheme(bool isDark)
    {
        var themeName = isDark ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.TreeInclination;component/Themes/{themeName}.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Clear();
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        _currentThemeIsDark = isDark;
    }

    private void ApplyTheme(bool isDark)
    {
        if (_currentThemeIsDark == isDark) return;
        LoadTheme(isDark);
    }

    private void RecentFilesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem is RecentFileItem item && DataContext is MainViewModel vm)
        {
            vm.OpenRecentCommand.Execute(item.FilePath);
            combo.SelectedIndex = -1; // Reset selection
        }
    }

    // Camera control event handlers for 3D view
    private void ResetCamera_Click(object sender, RoutedEventArgs e)
    {
        if (MainViewport3D.Camera is PerspectiveCamera camera)
        {
            camera.Position = new Point3D(2, 2, 2);
            camera.LookDirection = new Vector3D(-1, -1, -1);
            camera.UpDirection = new Vector3D(0, 0, 1);
        }
    }

    private void TopView_Click(object sender, RoutedEventArgs e)
    {
        if (MainViewport3D.Camera is PerspectiveCamera camera)
        {
            camera.Position = new Point3D(0, 0, 3);
            camera.LookDirection = new Vector3D(0, 0, -1);
            camera.UpDirection = new Vector3D(0, 1, 0);
        }
    }

    private void SideView_Click(object sender, RoutedEventArgs e)
    {
        if (MainViewport3D.Camera is PerspectiveCamera camera)
        {
            camera.Position = new Point3D(3, 0, 0);
            camera.LookDirection = new Vector3D(-1, 0, 0);
            camera.UpDirection = new Vector3D(0, 0, 1);
        }
    }

    private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        // Refresh charts after user edits coordinate values
        if (DataContext is MainViewModel vm)
        {
            // Use dispatcher to ensure the binding has completed
            Dispatcher.BeginInvoke(new Action(() =>
            {
                vm.RefreshChartsCommand?.Execute(null);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}
