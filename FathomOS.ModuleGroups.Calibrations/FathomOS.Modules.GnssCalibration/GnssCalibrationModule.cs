using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.GnssCalibration.Views;

namespace FathomOS.Modules.GnssCalibration;

/// <summary>
/// GNSS Calibration and Verification Module
/// Compares GNSS positioning systems with statistical analysis and 2DRMS calculations.
/// </summary>
public class GnssCalibrationModule : IModule
{
    private MainWindow? _mainWindow;

    #region IModule Properties

    public string ModuleId => "GnssCalibration";
    
    public string DisplayName => "GNSS Calibration";
    
    public string Description => "Compare and validate GNSS positioning systems with statistical analysis, outlier detection, and 2DRMS calculations.";
    
    public Version Version => new Version(4, 5, 4);
    
    public string IconResource => "/FathomOS.Modules.GnssCalibration;component/Assets/icon.png";
    
    public string Category => "Calibration & Verification";
    
    public int DisplayOrder => 10;

    #endregion

    #region IModule Methods

    public void Initialize()
    {
        System.Diagnostics.Debug.WriteLine($"[{DisplayName}] v{Version} initialized");
    }

    public void Launch(Window? owner = null)
    {
        try
        {
            if (_mainWindow == null || !_mainWindow.IsLoaded)
            {
                _mainWindow = new MainWindow();
            }
            
            _mainWindow.Show();
            _mainWindow.Activate();
            
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to launch {DisplayName}:\n\n{ex.Message}",
                "Module Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            System.Diagnostics.Debug.WriteLine($"[{DisplayName}] Launch error: {ex}");
        }
    }

    public void Shutdown()
    {
        try
        {
            _mainWindow?.Close();
            _mainWindow = null;
            System.Diagnostics.Debug.WriteLine($"[{DisplayName}] Shutdown complete");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{DisplayName}] Shutdown error: {ex.Message}");
        }
    }

    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".npd" or ".csv" or ".pos";
    }

    public void OpenFile(string filePath)
    {
        Launch();
        
        // TODO: Load file directly into the module
        if (_mainWindow?.DataContext is ViewModels.MainViewModel vm)
        {
            vm.LoadFileCommand?.Execute(filePath);
        }
    }

    #endregion
}
