using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;
using FathomOS.Modules.UsblVerification.Services;
using FathomOS.Modules.UsblVerification.ViewModels;

namespace FathomOS.Modules.UsblVerification.Views;

public partial class MainWindow : MetroWindow
{
    private bool _isDarkTheme = true;
    private readonly MainViewModel _viewModel;
    
    /// <summary>
    /// Gets the ViewModel for external access (e.g., file loading)
    /// </summary>
    public MainViewModel ViewModel => _viewModel;

    public MainWindow()
    {
        // Load default dark theme
        LoadTheme(true);
        
        InitializeComponent();
        
        _viewModel = new MainViewModel();
        _viewModel.RequestColumnMapping += OnRequestColumnMapping;
        _viewModel.RequestBatchColumnMapping += OnRequestBatchColumnMapping;
        _viewModel.Scene3DUpdated += OnScene3DUpdated;
        DataContext = _viewModel;
        
        UpdateThemeButton();
    }
    
    /// <summary>
    /// Load a file directly into the module (called from IModule.OpenFile)
    /// </summary>
    public void LoadFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            return;
            
        var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        
        if (ext == ".usblproj")
        {
            // Load project file
            _viewModel.LoadProjectCommand.Execute(filePath);
        }
        else if (ext is ".npd" or ".csv" or ".txt")
        {
            // Load as spin data file - prompt for heading assignment
            _viewModel.LoadSpinFilesCommand.Execute(filePath);
        }
    }

    private void LoadTheme(bool isDark)
    {
        _isDarkTheme = isDark;
        var themeName = isDark ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.UsblVerification;component/Themes/{themeName}.xaml", UriKind.Relative);
        
        // Clear existing theme dictionaries and add new one
        Resources.MergedDictionaries.Clear();
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
    }

    private void UpdateThemeButton()
    {
        if (ThemeIcon != null && ThemeText != null)
        {
            if (_isDarkTheme)
            {
                ThemeIcon.Kind = PackIconMaterialKind.WeatherNight;
                ThemeText.Text = "Dark";
            }
            else
            {
                ThemeIcon.Kind = PackIconMaterialKind.WeatherSunny;
                ThemeText.Text = "Light";
            }
        }
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        LoadTheme(!_isDarkTheme);
        UpdateThemeButton();
    }

    private void OnRequestColumnMapping(object? sender, ColumnMappingEventArgs e)
    {
        var dialog = new ColumnMappingDialog(e.FilePath);
        dialog.Owner = this;
        
        // Apply current theme to dialog
        dialog.Resources.MergedDictionaries.Clear();
        var themeName = _isDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.UsblVerification;component/Themes/{themeName}.xaml", UriKind.Relative);
        dialog.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        if (dialog.ShowDialog() == true && dialog.DialogConfirmed)
        {
            e.ResultMapping = dialog.ResultMapping;
            e.Confirmed = true;
        }
        else
        {
            e.Confirmed = false;
        }
    }

    private void OnRequestBatchColumnMapping(object? sender, BatchColumnMappingEventArgs e)
    {
        var dialog = new BatchColumnMappingDialog(e.FilePaths);
        dialog.Owner = this;
        
        // Apply current theme to dialog
        dialog.Resources.MergedDictionaries.Clear();
        var themeName = _isDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.UsblVerification;component/Themes/{themeName}.xaml", UriKind.Relative);
        dialog.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        if (dialog.ShowDialog() == true && dialog.DialogConfirmed)
        {
            e.ResultMappings = dialog.ResultMappings;
            e.Confirmed = true;
        }
        else
        {
            e.Confirmed = false;
        }
    }

    #region Drag and Drop Support
    
    private void MainWindow_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var validExtensions = new[] { ".npd", ".csv", ".txt", ".usbl", ".usp" };
            
            bool hasValidFiles = files.Any(f => 
                validExtensions.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()));
            
            e.Effects = hasValidFiles ? DragDropEffects.Copy : DragDropEffects.None;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }
    
    private void MainWindow_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            _viewModel.HandleDroppedFiles(files);
        }
        e.Handled = true;
    }
    
    #endregion
    
    #region Recent Projects
    
    private void RecentProject_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is RecentProject project)
        {
            _viewModel.OpenRecentProject(project);
        }
    }
    
    #endregion
    
    #region 3D Viewport Interaction
    
    private Point _lastMousePos;
    private bool _isRotating = false;
    private double _cameraDistance = 20;
    private double _cameraTheta = 45; // Horizontal angle
    private double _cameraPhi = 30;   // Vertical angle
    
    private void Viewport3D_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _isRotating = true;
            _lastMousePos = e.GetPosition(MainViewport3D);
            MainViewport3D.CaptureMouse();
        }
    }
    
    private void Viewport3D_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isRotating)
        {
            var currentPos = e.GetPosition(MainViewport3D);
            var dx = currentPos.X - _lastMousePos.X;
            var dy = currentPos.Y - _lastMousePos.Y;
            
            _cameraTheta += dx * 0.5;
            _cameraPhi = Math.Max(-89, Math.Min(89, _cameraPhi - dy * 0.5));
            
            UpdateCameraPosition();
            _lastMousePos = currentPos;
        }
    }
    
    private void Viewport3D_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isRotating = false;
        MainViewport3D.ReleaseMouseCapture();
    }
    
    private void Viewport3D_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _cameraDistance = Math.Max(5, Math.Min(100, _cameraDistance - e.Delta * 0.02));
        UpdateCameraPosition();
    }
    
    private void UpdateCameraPosition()
    {
        if (MainCamera == null) return;
        
        double thetaRad = _cameraTheta * Math.PI / 180;
        double phiRad = _cameraPhi * Math.PI / 180;
        
        double x = _cameraDistance * Math.Cos(phiRad) * Math.Cos(thetaRad);
        double y = _cameraDistance * Math.Cos(phiRad) * Math.Sin(thetaRad);
        double z = _cameraDistance * Math.Sin(phiRad);
        
        MainCamera.Position = new System.Windows.Media.Media3D.Point3D(x, y, z);
        MainCamera.LookDirection = new System.Windows.Media.Media3D.Vector3D(-x, -y, -z);
    }
    
    #endregion
    
    #region 3D Scene Update
    
    private void OnScene3DUpdated(object? sender, System.Windows.Media.Media3D.Model3DGroup? scene)
    {
        if (scene != null && MainModelVisual != null)
        {
            // Set the scene as the content of the ModelVisual3D
            MainModelVisual.Content = scene;
        }
    }
    
    #endregion

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.RequestColumnMapping -= OnRequestColumnMapping;
        _viewModel.RequestBatchColumnMapping -= OnRequestBatchColumnMapping;
        _viewModel.Scene3DUpdated -= OnScene3DUpdated;
        base.OnClosed(e);
    }
}
