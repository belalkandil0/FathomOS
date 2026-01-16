namespace FathomOS.Modules.NetworkTimeSync;

using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.NetworkTimeSync.Views;

/// <summary>
/// Module implementation for Fathom OS Shell integration.
/// Implements IModule interface to enable discovery and launching.
/// </summary>
public class NetworkTimeSyncModule : IModule
{
    private DashboardWindow? _mainWindow;
    
    // ═══════════════════════════════════════════════════════════
    // MODULE METADATA
    // ═══════════════════════════════════════════════════════════
    
    public string ModuleId => "NetworkTimeSync";
    
    public string DisplayName => "Network Time Sync";
    
    public string Description => "Discover, monitor, and synchronize time across network computers";
    
    public Version Version => new Version(2, 0, 0);
    
    public string IconResource => "/FathomOS.Modules.NetworkTimeSync;component/Assets/icon.png";
    
    public string Category => "Utilities";
    
    public int DisplayOrder => 30;
    
    // ═══════════════════════════════════════════════════════════
    // LIFECYCLE METHODS
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Called when module is first loaded by the Shell.
    /// Initialize resources, register services, load settings.
    /// </summary>
    public void Initialize()
    {
        // Load any saved settings
        // Register any services with DI container if using one
        // Pre-load any resources needed
    }
    
    /// <summary>
    /// Launch the module's main window
    /// </summary>
    /// <param name="owner">Parent window for modal dialogs</param>
    public void Launch(Window? owner = null)
    {
        // If window exists and is loaded, just activate it
        if (_mainWindow != null && _mainWindow.IsLoaded)
        {
            _mainWindow.Activate();
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
            return;
        }
        
        // Create new window
        _mainWindow = new DashboardWindow();
        
        if (owner != null)
        {
            _mainWindow.Owner = owner;
        }
        
        _mainWindow.Show();
    }
    
    /// <summary>
    /// Called when application is closing - cleanup resources
    /// </summary>
    public void Shutdown()
    {
        // Save any unsaved data
        // Close window if open
        _mainWindow?.Close();
        _mainWindow = null;
    }
    
    // ═══════════════════════════════════════════════════════════
    // FILE HANDLING
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Check if module can handle a specific file type
    /// </summary>
    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;
        
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        
        // Module can handle NTS files (time sync configuration)
        return ext == ".nts" || ext == ".timesync";
    }
    
    /// <summary>
    /// Open a file directly in this module (for drag-drop or file association)
    /// </summary>
    public void OpenFile(string filePath)
    {
        // Launch the module
        Launch();
        
        // Load the configuration file
        _mainWindow?.LoadConfiguration(filePath);
    }
}
