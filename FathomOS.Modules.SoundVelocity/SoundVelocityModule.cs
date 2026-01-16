using System;
using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.SoundVelocity.Views;

namespace FathomOS.Modules.SoundVelocity;

public class SoundVelocityModule : IModule
{
    private MainWindow? _mainWindow;

    #region IModule Properties

    public string ModuleId => "SoundVelocity";
    public string DisplayName => "Sound Velocity Profile";
    public string Description => "Process CTD cast data, calculate sound velocity using Chen-Millero or Del Grosso formulas, and export to industry formats (USR, VEL, PRO, Excel)";
    public Version Version => new Version(1, 2, 0);
    public string IconResource => "/FathomOS.Modules.SoundVelocity;component/Assets/icon.png";
    public string Category => "Data Processing";
    public int DisplayOrder => 15;

    #endregion

    #region IModule Methods

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
            MessageBox.Show($"Failed to launch {DisplayName}: {ex.Message}",
                "Module Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Shutdown()
    {
        try
        {
            _mainWindow?.Close();
            _mainWindow = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Shutdown error: {ex.Message}");
        }
    }

    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".000" or ".001" or ".002" or ".003" or ".svp" or ".ctd" or ".bp3" or ".txt" or ".csv";
    }

    public void OpenFile(string filePath)
    {
        Launch();
        _mainWindow?.LoadFile(filePath);
    }

    #endregion
}
