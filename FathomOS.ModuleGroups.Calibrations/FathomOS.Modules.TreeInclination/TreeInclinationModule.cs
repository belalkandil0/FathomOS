namespace FathomOS.Modules.TreeInclination;

using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.TreeInclination.Views;

/// <summary>
/// Tree Inclination Calculator Module for Fathom OS
/// Calculates structure inclination from DigiQuartz sensor depth measurements
/// </summary>
public class TreeInclinationModule : IModule
{
    private MainWindow? _mainWindow;

    #region IModule Properties

    public string ModuleId => "TreeInclination";
    public string DisplayName => "Tree Inclination";
    public string Description => "Calculate inclination of subsea structures using DigiQuartz depth sensors";
    public Version Version => new Version(1, 0, 0);
    public string IconResource => "/FathomOS.Modules.TreeInclination;component/Assets/icon.png";
    public string Category => "Quality Control";
    public int DisplayOrder => 25;

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
                if (owner != null)
                    _mainWindow.Owner = owner;
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
        return ext is ".fosi" or ".npd";
    }

    public void OpenFile(string filePath)
    {
        Launch();
        
        if (_mainWindow?.DataContext is ViewModels.MainViewModel vm)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            
            if (ext == ".fosi")
            {
                vm.LoadProjectCommand.Execute(filePath);
            }
            else if (ext == ".npd")
            {
                vm.LoadFilesCommand.Execute(new[] { filePath });
            }
        }
    }

    #endregion
}
