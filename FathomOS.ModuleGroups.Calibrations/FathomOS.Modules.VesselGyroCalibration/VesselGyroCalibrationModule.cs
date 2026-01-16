using System;
using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.VesselGyroCalibration.Views;

namespace FathomOS.Modules.VesselGyroCalibration;

public class VesselGyroCalibrationModule : IModule
{
    private MainWindow? _mainWindow;

    public string ModuleId => "VesselGyroCalibration";
    public string DisplayName => "Vessel Gyro Calibration";
    public string Description => "Calibrate vessel gyro systems using C-O methodology with 3-sigma outlier detection";
    public Version Version => new(1, 0, 0);
    public string IconResource => "/FathomOS.Modules.VesselGyroCalibration;component/Assets/icon.png";
    public string Category => "Calibrations";
    public int DisplayOrder => 10;

    public void Initialize()
    {
        System.Diagnostics.Debug.WriteLine($"{DisplayName} v{Version} initialized");
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
                _mainWindow.WindowState = WindowState.Normal;
        }
        catch (Exception ex)
        {
            var fullError = $"Failed to launch {DisplayName}:\n\n{ex.Message}";
            if (ex.InnerException != null)
                fullError += $"\n\nInner: {ex.InnerException.Message}";
            fullError += $"\n\nStack: {ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0))}";
            
            System.Windows.MessageBox.Show(fullError,
                "Module Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Shutdown()
    {
        try { _mainWindow?.Close(); _mainWindow = null; }
        catch { }
    }

    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".npd" or ".csv";
    }

    public void OpenFile(string filePath)
    {
        Launch();
    }
}
