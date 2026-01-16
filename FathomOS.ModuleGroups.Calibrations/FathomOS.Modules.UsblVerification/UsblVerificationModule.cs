using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.UsblVerification.Views;

namespace FathomOS.Modules.UsblVerification;

/// <summary>
/// USBL Verification Module - IModule implementation for Fathom OS integration.
/// Provides spin and transit verification testing for USBL positioning systems.
/// </summary>
public class UsblVerificationModule : IModule
{
    private MainWindow? _mainWindow;

    #region IModule Properties

    public string ModuleId => "UsblVerification";
    
    public string DisplayName => "USBL Verification";
    
    public string Description => "Verify USBL positioning accuracy through spin and transit tests with automated reporting.";
    
    public Version Version => new Version(1, 7, 0);
    
    public string IconResource => "/FathomOS.Modules.UsblVerification;component/Assets/icon.png";
    
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
                {
                    _mainWindow.Owner = owner;
                }
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
            System.Windows.MessageBox.Show(
                $"Failed to launch {DisplayName}: {ex.Message}",
                "Module Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            System.Diagnostics.Debug.WriteLine($"Launch error: {ex}");
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
        return ext is ".npd" or ".csv" or ".txt" or ".usbl";
    }

    public void OpenFile(string filePath)
    {
        Launch();
        _mainWindow?.LoadFile(filePath);
    }

    #endregion
}
