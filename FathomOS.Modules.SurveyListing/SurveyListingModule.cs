using System.IO;
using System.Windows;
using FathomOS.Core;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.SurveyListing.Views;
using MessageBox = System.Windows.MessageBox;

namespace FathomOS.Modules.SurveyListing;

/// <summary>
/// Survey Listing Generator module for Fathom OS.
/// Generates survey listings from NPD files with route alignment, tide corrections,
/// and multiple export formats.
/// </summary>
public class SurveyListingModule : IModule
{
    private MainWindow? _mainWindow;
    
    #region IModule Properties
    
    public string ModuleId => "SurveyListing";
    
    public string DisplayName => "Survey Listing Generator";
    
    public string Description => AppInfo.Description;
    
    public Version Version => AppInfo.Version;
    
    public string IconResource => "/FathomOS.Modules.SurveyListing;component/Assets/icon.png";
    
    public string Category => "Data Processing";
    
    public int DisplayOrder => 10;
    
    #endregion
    
    #region IModule Methods
    
    public void Initialize()
    {
        // Module initialization
        // Load settings, register services, etc.
        System.Diagnostics.Debug.WriteLine($"Survey Listing Module {AppInfo.VersionDisplay} initialized");
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
            MessageBox.Show($"Failed to launch Survey Listing module: {ex.Message}",
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
            System.Diagnostics.Debug.WriteLine($"Error during Survey Listing shutdown: {ex.Message}");
        }
    }
    
    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        return extension switch
        {
            ".npd" => true,   // Survey data files
            ".rlx" => true,   // Route alignment files
            ".s7p" => true,   // Fathom OS project files
            ".tide" => true,  // Tide correction files
            _ => false
        };
    }
    
    public void OpenFile(string filePath)
    {
        Launch();
        
        if (_mainWindow != null && File.Exists(filePath))
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // Load file based on type
            switch (extension)
            {
                case ".s7p":
                    _mainWindow.LoadProject(filePath);
                    break;
                case ".npd":
                    _mainWindow.LoadSurveyFile(filePath);
                    break;
                case ".rlx":
                    _mainWindow.LoadRouteFile(filePath);
                    break;
                case ".tide":
                    _mainWindow.LoadTideFile(filePath);
                    break;
            }
        }
    }
    
    #endregion
}
